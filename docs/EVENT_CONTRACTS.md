# Shiori — Event Contracts

**Status:** Draft — STEP 5 final validation pending  
**Scope:** Asynchronous contracts used between Shiori bounded contexts through RabbitMQ.

---

## Why this document exists

Shiori's services are deployed independently, so a RabbitMQ message cannot be treated as “just some JSON” passed between two pieces of the same application.

Once a message crosses a bounded-context boundary, it becomes a compatibility contract.

This document defines that contract language:

- what counts as a Domain Event, Integration Event, or Integration Command
- which metadata every message carries
- how contracts are named and versioned
- how Catalog lifecycle facts keep Tracking's local projection synchronized
- how import-related asynchronous work crosses the Tracking/Catalog boundary
- how JSON Schemas and contract tests protect compatibility
- how a breaking contract version is rolled out without requiring synchronized deployments

The main rule is:

> **Integration contracts describe Shiori business meaning, not another service's database or implementation.**

---

# 1. Message semantics

Shiori uses three different concepts that should not be collapsed just because all of them can be represented as objects in code.

---

## 1.1 Domain Event

A Domain Event is internal to one bounded context.

Example:

```text
Tracking
    -> progress changes
    -> ProgressChangedDomainEvent
```

It may help coordinate logic inside Tracking, but it does not automatically become a RabbitMQ message.

A domain event only becomes an external contract when another bounded context genuinely needs that fact.

So:

```text
Domain Event != Integration Event
```

This keeps internal refactors from becoming distributed compatibility obligations.

---

## 1.2 Integration Event

An Integration Event says:

> **This business fact already happened.**

Examples:

- `CatalogItemCreated`
- `CatalogItemUpdated`
- `CatalogItemRetired`
- `PublicationUnitCreated`
- `PublicationUnitUpdated`
- `PublicationUnitRetired`
- `UserLibraryImportCompleted`

The producer owns the fact.

Consumers are free to react according to their own responsibilities.

For example:

```text
Catalog
   -> PublicationUnitCreated
   -> RabbitMQ
      -> Tracking
      -> future consumer
```

Catalog does not publish `UpdateTrackingProjection` because that would expose one consumer's implementation.

The event should describe the fact, not the reaction.

---

## 1.3 Integration Command

An Integration Command says:

> **Please perform a capability that your bounded context owns.**

The current example is import hydration:

```text
Tracking
    -> HydrateCatalogItems
    -> RabbitMQ
    -> Catalog
```

Tracking owns the import workflow.

Catalog owns metadata hydration and provider access.

The command therefore asks Catalog to perform Catalog-owned work without telling it:

- which HTTP provider endpoint to call
- which Worker to use
- which MongoDB collection to write
- how to structure its internal use case

The command expresses the capability request, not the implementation.

---

## 1.4 Event vs command

A practical naming test is:

```text
Event:
"This happened."

Command:
"Please do this."
```

Examples:

```text
PublicationUnitCreated
    -> event

HydrateCatalogItems
    -> command
```

Shiori should not rename a command into fake past tense just to make everything look event-driven.

Likewise, a real fact should not be turned into a consumer-specific command merely because only one consumer exists today.

---

## 1.5 Messages do not transfer ownership

RabbitMQ changes how information travels, not who owns it.

A Catalog event consumed by Tracking does not make Tracking the owner of Catalog data.

A hydration command from Tracking to Catalog does not make Catalog the owner of the import workflow.

This distinction remains true no matter how many consumers are added later.

---

## 1.6 Contracts are not persistence models

The intended path is:

```text
Producer Domain/Persistence
    -> explicit mapping
    -> Integration Contract
    -> RabbitMQ
    -> consumer mapping
    -> Consumer Local Model
```

Not:

```text
Catalog MongoDB document
    -> serialize directly
    -> RabbitMQ
    -> Tracking stores it
```

The integration contract is a third model with its own purpose and compatibility lifecycle.

---

## 1.7 This is not Event Sourcing

RabbitMQ is not Shiori's historical source of truth.

Integration Events do not replace:

- Catalog canonical MongoDB state
- Tracking current state
- Tracking immutable history
- Identity account state

Shiori uses events for integration, not as the primary persistence model for aggregates.

---

# 2. Integration Event envelope

Every Integration Event contains:

```text
envelope metadata
+
event-specific payload
```

The envelope answers what happened, which resource produced it, which contract version applies, and how the message relates to the wider distributed flow.

The baseline conceptual shape is:

```json
{
  "eventId": "01JEVT...",
  "eventType": "CatalogItemUpdated",
  "eventVersion": 1,
  "aggregateId": "01JCAT...",
  "aggregateVersion": 42,
  "occurredAt": "2026-08-09T19:32:15Z",
  "correlationId": "7234d279-d290-4ab4-96dc-b01900bc11c8",
  "causationId": "01JCAUSE...",
  "payload": {}
}
```

The exact JSON Schema is created during implementation, but these field meanings are part of the architecture.

---

## 2.1 `eventId`

`eventId` identifies one logical event occurrence.

It is primarily used for duplicate detection.

RabbitMQ delivery is at-least-once, so the same logical event may be delivered more than once.

A redelivery keeps the same `eventId`.

```text
EVENT-A
delivery 1

EVENT-A
delivery 2

EVENT-A
delivery 3
```

Tracking's Inbox can then recognize that these are the same event.

Generating a new event ID on every retry would defeat that protection.

---

## 2.2 `eventType`

`eventType` identifies the semantic contract.

Examples:

```text
CatalogItemUpdated
PublicationUnitCreated
UserLibraryImportCompleted
```

It does not identify:

- a queue
- a consumer
- a database collection
- a CLR type
- a deployment version

The contract name should describe the business fact.

---

## 2.3 `aggregateId`

`aggregateId` identifies the Shiori-owned resource whose state produced the event.

For example:

```text
CatalogItemCreated
aggregateId = CatalogItemId

CatalogItemUpdated
aggregateId = same CatalogItemId
```

Each event occurrence receives a new `eventId`, while the `aggregateId` stays stable for that resource.

Provider IDs are not used as canonical aggregate identities across Shiori.

---

## 2.4 `aggregateVersion`

`aggregateVersion` represents the producer-owned state version associated with the event.

It protects local projections from moving backward.

Example:

```text
Tracking projection: v42
incoming event:      v41
```

That incoming event is not necessarily a duplicate, but it is stale for this projection and must not replace v42.

This solves a different problem from `eventId`.

---

## 2.5 `eventVersion`

`eventVersion` is the version of the integration contract itself.

For example:

```text
CatalogItemUpdated v1
```

may eventually coexist with:

```text
CatalogItemUpdated v2
```

The two version concepts are deliberately separate:

```text
eventVersion
    -> contract evolution

aggregateVersion
    -> resource-state evolution
```

A Catalog item may be at aggregate version 183 while still publishing `CatalogItemUpdated` contract version 1.

---

## 2.6 `occurredAt`

`occurredAt` is when the producer durably accepted the business fact.

It is not:

- publish time
- RabbitMQ delivery time
- retry time
- consumer processing time

If a Catalog mutation is committed at 19:05 and RabbitMQ only becomes available at 19:08, the event still occurred at 19:05.

Transport delay does not rewrite business history.

---

## 2.7 `correlationId`

`correlationId` links work belonging to the same wider distributed flow.

A request may move through:

```text
Gateway
-> Tracking
-> Outbox
-> RabbitMQ
-> Catalog
-> RabbitMQ
-> Tracking consumer
```

while retaining the same correlation ID.

This is observability metadata, not identity or authorization proof.

---

## 2.8 `causationId`

`causationId` identifies the immediate predecessor that caused the current message, when one exists.

For example:

```text
HydrateCatalogItems
commandId = CMD-1

CatalogItemCreated
causationId = CMD-1
```

The correlation ID answers:

> Which broader flow am I part of?

The causation ID answers:

> What directly caused me?

The exact rule for an HTTP request directly causing the first asynchronous message is intentionally not fixed by this source document.

---

## 2.9 `payload`

`payload` contains only the business data specific to:

```text
eventType + eventVersion
```

It should be large enough for the event's declared purpose and no larger by default.

It must not simply expose:

- the full aggregate
- the EF entity
- the MongoDB document
- a provider DTO

---

## 2.10 Event metadata summary

| Field | Meaning |
|---|---|
| `eventId` | Identity of this event occurrence |
| `eventType` | Semantic event contract |
| `eventVersion` | Version of that contract |
| `aggregateId` | Shiori resource that produced the fact |
| `aggregateVersion` | Producer-side state version |
| `occurredAt` | Time the fact was durably accepted |
| `correlationId` | Wider distributed flow |
| `causationId` | Immediate causal predecessor |
| `payload` | Event-specific business data |

The most important distinctions are:

```text
eventId != aggregateId
eventVersion != aggregateVersion
occurredAt != delivery time
correlationId != causationId
```

---

# 3. Integration Command envelope

Commands use a similar envelope but keep command-specific names so the serialized message itself makes the semantic category clear.

The baseline shape is:

```json
{
  "commandId": "01JCMD...",
  "commandType": "HydrateCatalogItems",
  "commandVersion": 1,
  "correlationId": "7234d279-d290-4ab4-96dc-b01900bc11c8",
  "causationId": "01JCAUSE...",
  "payload": {}
}
```

---

## 3.1 `commandId`

Identifies one logical capability request.

A RabbitMQ redelivery preserves the same `commandId`.

A genuinely new request receives a new one.

This lets the receiving bounded context protect itself against duplicate execution.

---

## 3.2 `commandType`

Names the requested capability.

Good:

```text
HydrateCatalogItems
```

Poor:

```text
CallAniListBatch
RunCatalogWorker
InsertCatalogDocuments
```

The command should survive implementation changes inside Catalog.

---

## 3.3 `commandVersion`

Represents the compatibility version of that command contract.

It does not follow:

- application version
- container version
- database migration
- queue retry count
- import-job revision

Version numbers change when contract compatibility changes.

---

## 3.4 Why commands do not have universal aggregate metadata

Commands do not universally carry `aggregateId` or `aggregateVersion`.

A command is a request for work, not a statement that one producer-owned aggregate is now at a particular version.

For example, one hydration command may contain several source references.

If a particular command needs a resource ID, expected revision, or concurrency precondition, that belongs explicitly in that command's payload.

---

# 4. Serialization and naming

All RabbitMQ integration bodies use:

```text
JSON
UTF-8
```

Contracts must be understandable from their documented serialized form without loading the producer's .NET assembly.

---

## 4.1 JSON property names

Envelope and payload properties use `camelCase`.

Example:

```json
{
  "eventId": "01JEVT...",
  "eventType": "CatalogItemUpdated",
  "eventVersion": 1,
  "aggregateId": "01JCAT...",
  "aggregateVersion": 42
}
```

---

## 4.2 Event names

Event contract names use PascalCase and describe completed facts:

```text
CatalogItemCreated
CatalogItemUpdated
CatalogItemRetired
PublicationUnitCreated
PublicationUnitUpdated
PublicationUnitRetired
UserLibraryImportCompleted
```

They should not include:

- consumer names
- queue names
- database terminology
- service versions
- provider implementation details

---

## 4.3 Command names

Commands also use PascalCase but are capability-oriented:

```text
HydrateCatalogItems
```

The name should sound like requested work, not like an already completed fact.

---

## 4.4 Type and version stay separate

Correct:

```json
{
  "eventType": "CatalogItemUpdated",
  "eventVersion": 1
}
```

Not:

```json
{
  "eventType": "CatalogItemUpdatedV1"
}
```

The same applies to commands.

---

## 4.5 C# names are not the wire contract

The local C# class may use PascalCase properties.

The JSON contract remains camelCase.

Refactoring an internal C# name or persistence property does not give the producer permission to silently rename the serialized field.

The wire contract is the compatibility surface.

---

# 5. Versioning and compatibility

Shiori services should be able to deploy independently.

That requires contract versions to change because compatibility changed, not because a service was redeployed.

Every published contract change is classified as either:

```text
BACKWARD COMPATIBLE
```

or:

```text
BREAKING
```

---

## 5.1 Compatible changes

The main compatible evolution is adding information that existing consumers can safely ignore.

Examples:

- new optional property
- new optional nested object
- entirely new event/command contract

Example:

Before:

```json
{
  "catalogItemId": "01JCAT..."
}
```

Compatible v1 evolution:

```json
{
  "catalogItemId": "01JCAT...",
  "trackingCapability": "reading"
}
```

only if old v1 consumers remain correct when they ignore `trackingCapability`.

---

## 5.2 Breaking changes

The following are breaking when applied to an already published version:

- rename an existing property
- remove contracted information
- change a property's JSON type
- turn optional data into required data
- change the semantic meaning of a field
- change units while preserving the same JSON type
- replace a Shiori ID with a provider ID
- rename a published event or command type

For example, changing:

```text
elapsed = seconds
```

to:

```text
elapsed = milliseconds
```

is breaking even though both are numbers.

Semantics are part of the contract.

---

## 5.3 Published versions are historical boundaries

Once `CatalogItemUpdated v1` has been published, v1 keeps that meaning.

If an incompatible future shape is required:

```text
CatalogItemUpdated v2
```

is introduced.

v1 is not silently rewritten.

Versioning is per contract; there is no global Shiori message version.

---

## 5.4 Consumer tolerance

A consumer that supports a compatible version should ignore unknown additive optional properties rather than fail just because the producer learned something new.

That tolerance does not mean consumers ignore:

- missing required fields
- invalid JSON types
- unsupported contract versions
- semantically invalid values

---

# 6. Catalog projection events

Catalog publishes lifecycle facts so Tracking can maintain its local Catalog projection without calling Catalog after every message.

The active projection families are:

```text
CatalogItemCreated
CatalogItemUpdated
CatalogItemRetired

PublicationUnitCreated
PublicationUnitUpdated
PublicationUnitRetired
```

These events contain only state required for the declared projection.

---

## 6.1 Snapshot rather than patch

`Created` and `Updated` events carry the current projection-relevant state.

`Updated` is not a JSON Patch or a list of changed fields.

That means Tracking can receive a newer snapshot and converge without reconstructing every intermediate mutation.

Conceptually:

```text
Catalog v40
Catalog v41
Catalog v42
```

If Tracking safely receives the v42 projection snapshot after v40, it should not need a field-by-field v41 patch to know the v42 state relevant to its projection.

---

## 6.2 Catalog item snapshot

The conceptual v1 payload includes fields such as:

```json
{
  "mediaType": "manga",
  "trackingCapability": "reading",
  "releaseTracks": [
    {
      "trackId": "officialEnglish",
      "supportStatus": "supported",
      "stalenessStatus": "current",
      "unitType": "chapter",
      "latestKnownUnit": {
        "publicationUnitId": "01JUNIT...",
        "label": "74"
      },
      "source": "provider-or-catalog-source",
      "lastVerifiedAt": "2026-08-09T19:32:15Z"
    }
  ],
  "isRetired": false
}
```

The Catalog item ID is already carried in:

```text
envelope.aggregateId
```

and is not duplicated in the payload.

The snapshot is intentionally smaller than the full Catalog model.

---

## 6.3 `CatalogItemCreated.v1`

Means:

> A canonical Shiori Catalog item now exists, and this is the current state Tracking needs for its local projection.

Tracking can create or reconcile its `catalog_item_registry` row without calling Catalog back.

---

## 6.4 `CatalogItemUpdated.v1`

Means:

> The Tracking-relevant projection state of an existing Catalog item changed.

It should be emitted when a committed change affects something Tracking actually projects, such as:

- release-track latest known unit
- support/staleness state
- projected provenance/verification state
- media/progress classification
- Tracking capability

It should **not** be emitted merely because any field in the MongoDB document changed.

A synopsis typo, banner change, or trailer presentation change does not need to generate a Tracking projection event if Tracking does not project it.

RabbitMQ is not a generic MongoDB change feed.

---

## 6.5 `CatalogItemRetired.v1`

Means:

> The canonical Catalog item is retired and should no longer be treated as active by consumers.

This does not instruct Tracking to delete the user's progress or history.

A retirement payload can retain the projection-level state as a tombstone with:

```json
{
  "isRetired": true
}
```

The event does not currently define merge, replacement, redirect, or regrouping semantics.

---

## 6.6 Publication Unit snapshot

Publication Unit events maintain the local unit registry needed for granular progress validation.

Conceptually:

```json
{
  "catalogItemId": "01JCAT...",
  "unitType": "chapter",
  "label": "10.5",
  "volumeUnitId": "01JVOLUME...",
  "isRetired": false
}
```

The unit's own ID is:

```text
envelope.aggregateId = PublicationUnitId
```

Labels remain strings because valid reading labels may include:

```text
0
10.5
Extra
Special
One-shot
named interludes
```

---

## 6.7 Publication Unit lifecycle

`PublicationUnitCreated.v1`

> A canonical publication unit now exists.

`PublicationUnitUpdated.v1`

> Tracking-relevant state of an existing publication unit changed.

`PublicationUnitRetired.v1`

> The unit is retired and should not remain active in Tracking's projection.

Retirement does not authorize deletion of Tracking-owned history.

A publication-unit lifecycle event also does not automatically mean:

> Notify this user that a new official release is available.

That stronger future semantic needs its own explicit contract if required.

---

## 6.8 Event emission table

| Catalog change | Contract |
|---|---|
| New canonical Catalog item | `CatalogItemCreated.v1` |
| Tracking-relevant item state changed | `CatalogItemUpdated.v1` |
| Catalog item retired | `CatalogItemRetired.v1` |
| Synopsis/presentation-only change | No Tracking projection event |
| New canonical publication unit | `PublicationUnitCreated.v1` |
| Tracking-relevant unit state changed | `PublicationUnitUpdated.v1` |
| Publication unit retired | `PublicationUnitRetired.v1` |
| Release-track snapshot changed | `CatalogItemUpdated.v1` |
| Internal/provider change outside projection semantics | No projection event |

---

# 7. Import contracts

Tracking owns Smart Staging Import.

Catalog owns metadata hydration.

The asynchronous relationship is:

```text
Tracking
    -> HydrateCatalogItems command
    -> Catalog

Catalog
    -> normal lifecycle events
    -> Tracking projection
```

Import does not create a private provider path.

---

## 7.1 `HydrateCatalogItems.v1`

**Category:** Integration Command  
**Producer:** Tracking  
**Consumer:** Catalog

Meaning:

> Resolve and hydrate this bounded batch of source references through Catalog's normal metadata capability.

Conceptual payload:

```json
{
  "importJobId": "01JIMP...",
  "sourceType": "myAnimeList",
  "items": [
    {
      "sourceItemId": "5114",
      "mediaType": "anime"
    },
    {
      "sourceItemId": "9253",
      "mediaType": "manga"
    }
  ]
}
```

`importJobId` stays a Tracking-owned workflow ID.

`sourceItemId` is a source/import identifier, not a canonical Shiori `CatalogItemId`.

Catalog still performs provider normalization and canonical persistence.

---

## 7.2 Hydration result vs lifecycle events

A hydration-result contract, when finalized, answers:

> How did this requested batch finish?

Catalog lifecycle events answer:

> What canonical Catalog state now exists?

Those are different contracts.

A hydration result must not replace normal Catalog -> Tracking projection events.

The source document notes that the Roadmap still requires a correlated hydration-result event with duplicate protection and partial-failure reporting before Milestone 4 is complete, but this specific result contract is not defined here yet.

---

## 7.3 `UserLibraryImportCompleted.v1`

**Category:** Integration Event  
**Producer:** Tracking

Meaning:

> The Tracking-owned import job was durably finalized as Completed after all expected approved commit batches finished.

It is not emitted:

- after upload
- after parsing
- after preview
- when the user clicks Confirm
- after each batch
- once per imported row

For this event:

```text
aggregateId = ImportJobId
```

Conceptual payload:

```json
{
  "userId": "01JUSER...",
  "sourceType": "myAnimeList",
  "processedEntryCount": 4000,
  "committedEntryCount": 3984,
  "errorCount": 0
}
```

The durable import job remains Tracking's source of truth.

If RabbitMQ is down after finalization:

```text
job = Completed
Outbox = pending
publish = delayed
```

The import is still completed.

---

## 7.4 One completion fact per import

The intended cardinality is:

```text
one completed import job
    -> one UserLibraryImportCompleted event
```

not:

```text
4000 imported rows
    -> 4000 completion events
```

The completion event communicates the workflow result, not each row mutation.

---

# 8. Progress events

The source document makes an explicit MVP decision:

```text
ProgressUpdated.v1
STATUS: NOT PUBLISHED FOR MVP
```

Normal progress mutations do not create a RabbitMQ event merely because progress changed.

Tracking already owns:

- current progress
- revision/concurrency state
- immutable history
- Progress Vault foundation
- core personal statistics

There is no approved MVP consumer that needs every progress change externally.

Publishing a generic progress event now would create:

- extra Outbox volume
- RabbitMQ traffic
- compatibility obligations
- retention/operations pressure

without a concrete cross-service requirement.

The rule is:

```text
Tracking mutation
    |
    v
Does an approved external consumer need
a defined semantic fact?
    |
 +--+--+
 |     |
Yes    No
 |     |
 v     v
Outbox no integration event
```

Tracking still keeps the Outbox capability for future semantic events when a real consumer exists.

---

# 9. Contract storage

The cross-service contract is shared as a schema, not as a shared production C# assembly.

Shiori does not introduce:

```text
Shiori.Shared.Contracts.dll
```

or another common production package containing all event/command classes.

The reason is simple: sharing a DTO assembly across Catalog and Tracking would create a compile-time coupling between independently deployable services.

Small duplication of local contract classes is accepted.

---

## 9.1 Canonical location

Schemas live in a neutral repository directory:

```text
contracts/
└── integration/
    ├── events/
    │   ├── CatalogItemCreated/
    │   │   └── v1.schema.json
    │   ├── CatalogItemUpdated/
    │   │   └── v1.schema.json
    │   ├── CatalogItemRetired/
    │   │   └── v1.schema.json
    │   ├── PublicationUnitCreated/
    │   │   └── v1.schema.json
    │   ├── PublicationUnitUpdated/
    │   │   └── v1.schema.json
    │   ├── PublicationUnitRetired/
    │   │   └── v1.schema.json
    │   └── UserLibraryImportCompleted/
    │       └── v1.schema.json
    └── commands/
        └── HydrateCatalogItems/
            └── v1.schema.json
```

Each version receives its own artifact.

A future v2 is added beside v1 rather than overwriting it.

---

## 9.2 JSON Schema is canonical

The canonical machine-readable contract is:

```text
type
+
version
+
JSON Schema
```

Each service owns its own local C# representation.

Conceptually:

```text
Canonical JSON Schema
       /       \
      v         v
Catalog C#   Tracking C#
local type   local type
```

Neither C# class becomes the cross-service source of truth.

---

## 9.3 No runtime Schema Registry yet

The current architecture does not need a separate Schema Registry service.

Repository-level schemas plus CI validation are enough for the current single-repository setup.

If Shiori later splits repositories or needs centralized artifact distribution, the schema files can be published through an artifact mechanism without changing their role.

---

## 9.4 Retired schemas stay in history

When v1 stops being actively consumed, `v1.schema.json` remains available for:

- compatibility history
- old fixture tests
- DLQ investigation
- debugging
- migration understanding

Retirement does not mean pretending the version never existed.

---

# 10. Contract testing

Contract Tests answer:

> Does this implementation still understand the contract version it claims to support?

They do not replace RabbitMQ infrastructure tests.

---

## 10.1 Producer Contract Tests

A Producer test:

1. creates the local message representation
2. serializes it to UTF-8 JSON
3. validates that JSON against the canonical schema

This catches accidental drift such as:

```text
catalogItemId -> itemId
```

or:

```text
aggregateVersion: number -> string
```

while the producer still claims to emit v1.

---

## 10.2 Consumer Contract Tests

A Consumer test proves that the service can:

- deserialize every version it claims to support
- handle missing optional fields
- ignore compatible unknown additive fields
- handle unknown values according to schema rules
- map into its own local representation
- preserve `eventId` for duplicate detection
- preserve aggregate-version behavior where relevant

No producer assembly is loaded.

---

## 10.3 Unknown values

Unknown values are not automatically accepted or rejected globally.

Each schema decides whether a value set is open or closed.

If the schema permits extensibility, the consumer should handle an unknown value safely.

If the schema defines a closed set, an unknown value is contract-invalid.

The consumer should never silently map an unknown value to an unrelated known value just to keep processing.

---

## 10.4 Contract tests vs RabbitMQ integration tests

Contract Tests verify:

- JSON shape
- schema compatibility
- serialization/deserialization
- type/version selection
- optional/additive compatibility
- mapping semantics

RabbitMQ Integration Tests verify:

- real publish/consume
- broker connectivity
- message transport
- ACK/redelivery
- Inbox/Outbox behavior
- durable infrastructure interaction

Both are needed.

A valid schema does not prove RabbitMQ is configured correctly.

A working queue does not prove the payload is compatible.

---

# 11. Breaking-version deployment procedure

Breaking contract evolution follows a consumer-first expand/contract sequence.

Suppose production uses:

```text
CatalogItemUpdated v1
```

and a real incompatible requirement needs v2.

The rollout is:

```text
1. Define v2 schema and tests.
2. Keep producer emitting v1.
3. Deploy consumers that understand v1 + v2.
4. Verify required consumers are v2-ready.
5. Switch producer to emit v2 for new facts.
6. Keep consumers accepting v1 + v2 while old v1 messages drain.
7. Retire active v1 support only after v1 can no longer legitimately arrive.
```

At no point should the normal system enter this state:

```text
Producer emits v2
Consumer only understands v1
```

---

## 11.1 Consumer dispatch

During migration, consumers select the correct contract using:

```text
eventType + eventVersion
```

or:

```text
commandType + commandVersion
```

They do not guess the version from whichever payload properties happen to exist.

---

## 11.2 Dual publishing is not the default

Shiori does not automatically publish both v1 and v2 for the same business fact during migration.

That creates a second deduplication problem across versions.

The normal strategy is:

```text
consumer supports new version first
-> producer switches once
-> old consumer support drains later
```

If a specific future migration truly needs dual publishing, it should be designed explicitly.

---

## 11.3 Rollback safety

Once a producer emits v2, operations must not roll a consumer back to a release that only understands v1 while v2 messages may still exist.

The rule is:

> **Do not roll a consumer behind the oldest contract version that may legitimately arrive.**

The same migration model applies to commands.

For example, Catalog must understand `HydrateCatalogItems v2` before Tracking starts emitting it.

---

# 12. Current contract set

The active/defined contract set in this source document is:

## Catalog -> Tracking events

```text
CatalogItemCreated.v1
CatalogItemUpdated.v1
CatalogItemRetired.v1

PublicationUnitCreated.v1
PublicationUnitUpdated.v1
PublicationUnitRetired.v1
```

## Tracking -> Catalog command

```text
HydrateCatalogItems.v1
```

## Tracking event

```text
UserLibraryImportCompleted.v1
```

## Explicitly not published for MVP

```text
ProgressUpdated.v1
```

## Still required later by the Roadmap but not defined here

A correlated Catalog hydration-result contract with duplicate protection and partial-failure reporting remains required before the import milestone is complete.

This source document does not invent that contract prematurely.

---

# 13. Core contract principles

The many rules in this document reduce to a smaller set of principles:

1. Domain Events are internal unless deliberately promoted to integration contracts.
2. Integration Events describe facts that already happened.
3. Integration Commands request capabilities owned by another bounded context.
4. Messaging never transfers business ownership.
5. Integration contracts do not expose persistence/provider models.
6. RabbitMQ delivery is treated as at-least-once.
7. `eventId`/`commandId` survive redelivery of the same logical message.
8. `aggregateVersion` protects projection state from stale/out-of-order facts.
9. Contract version and aggregate version are different concepts.
10. JSON properties use camelCase; contract type names use PascalCase.
11. Contract compatibility changes, not service deployments, drive version numbers.
12. Compatible evolution is additive where old consumers can safely ignore new data.
13. Published versions are never redefined incompatibly.
14. Catalog projection events carry current projection-relevant snapshots, not MongoDB diffs.
15. Catalog emits projection events only when projection semantics actually changed.
16. Tracking does not call Catalog back after every projection event.
17. Import hydration remains a Tracking -> Catalog command, while canonical Catalog state returns through normal Catalog lifecycle events.
18. `ProgressUpdated.v1` is not an MVP contract.
19. JSON Schema is the canonical machine-readable contract.
20. Each service owns its local C# representation.
21. Contract Tests and RabbitMQ Integration Tests cover different failure classes.
22. Breaking migrations expand consumers before producers begin emitting the new version.

---

# 14. Current document state

This source file still identifies itself as:

```text
Draft — STEP 5 final validation pending
```

Its completion gate requires a final consistency check against:

- `ADR.md`
- `SYSTEM_DESIGN.md`
- `ROADMAP.md`

and specifically calls out the earlier `ProgressUpdated` mention so the project does not accidentally treat it as an active MVP event.

This humanized version preserves that source-state rather than silently marking STEP 5 accepted.

The final synchronization/approval belongs to the Architecture Freeze/document-consistency pass, not to editorial humanization.
