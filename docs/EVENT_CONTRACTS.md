# Shiori — Event Contracts

**Status:** Draft — STEP 5 final validation pending
**Scope:** Asynchronous integration-contract semantics and universal Integration Event metadata for RabbitMQ communication between Shiori bounded contexts.

## Related Documents

- `ADR.md` — accepted service boundaries, ownership, messaging, Outbox/Inbox, and transaction rules.
- `SYSTEM_DESIGN.md` — runtime communication flows, local projections, eventual consistency, and failure behavior.
- `ROADMAP.md` — required integration events, versioned envelopes, contract testing, and milestone sequencing.
- `API_CONVENTIONS.md` — HTTP tracing and correlation rules that asynchronous flows must continue without redefining.
- `PRODUCT_HORIZON.md` — future-extension requirements that depend on semantic, additive integration contracts.

`EVENT_CONTRACTS.md` defines the asynchronous language used between Shiori bounded contexts.

It does not redefine:

- Business ownership.
- Public HTTP APIs.
- Database schemas.
- Domain entities.
- Persistence models.
- RabbitMQ deployment topology.
- Exchange names.
- Queue names.
- Routing keys.
- Retry counts.
- Dead-letter replay procedures.
- Operational retention periods.

Those concerns remain governed by their corresponding architecture or operational documents.

---

# 1. Purpose, Scope & Terminology

## 1.1 Purpose

Shiori uses RabbitMQ for durable asynchronous communication between bounded contexts.

The purpose of this document is to make that communication an **explicit, versioned contract** rather than an implicit agreement between producer and consumer implementations.

An integration message is not merely serialized data transported through RabbitMQ.

It is a compatibility boundary between independently deployable components.

A producer and a consumer may:

- Run different application versions.
- Be deployed at different times.
- Use different persistence technologies.
- Represent the same business concept differently internally.

Therefore, neither side may depend on the other's:

- Domain entities.
- Database models.
- EF Core entities.
- MongoDB documents.
- Infrastructure implementation types.
- Provider DTOs.

The governing principle is:

> **An Integration Contract describes a business fact or capability request that crosses a bounded-context boundary. It does not expose the producer's internal implementation.**

---

## 1.2 Messaging Scope

RabbitMQ is used when information or work does not need to complete synchronously inside the caller's current HTTP request.

The currently accepted asynchronous model includes:

```text
Catalog
   |
   | Integration Events
   v
RabbitMQ
   |
   v
Tracking
```

and workflows such as:

```text
Tracking
   |
   | Integration Command
   v
RabbitMQ
   |
   v
Catalog
```

for Catalog-owned hydration during imports.

The communication remains asynchronous.

RabbitMQ is not used as ordinary synchronous RPC disguised behind a broker.

Messaging does not transfer business ownership.

For example:

```text
Catalog
owns publication facts
        |
        | PublicationUnitCreated
        v
     Tracking
```

Tracking may react to the event and update a local projection, but the publication unit remains Catalog-owned.

Likewise:

```text
Tracking
owns the import workflow
        |
        | hydration command
        v
      Catalog
```

Catalog owns metadata hydration, but it does not become the owner of the user's import workflow.

---

## 1.3 Domain Event

A **Domain Event** represents a meaningful fact inside a single bounded context.

It belongs to that bounded context's internal business model.

Conceptually:

```text
Tracking Domain

Progress changes
      |
      v
ProgressChangedDomainEvent
```

A Domain Event may be useful internally for:

- Domain behavior.
- Internal coordination.
- Application processing.
- Decoupling internal business behavior.

It is **not automatically a RabbitMQ message**.

Therefore:

```text
Domain Event
     !=
Integration Event
```

The existence of:

```text
ProgressChangedDomainEvent
```

does not automatically justify publishing:

```text
ProgressUpdated
```

to RabbitMQ.

A separate integration contract is created only when a business fact must cross a bounded-context boundary.

This prevents every internal implementation detail from becoming a permanent distributed compatibility obligation.

---

## 1.4 Integration Event

An **Integration Event** states:

> **A business fact already occurred.**

It describes something that the producing bounded context has already durably accepted as true.

Examples already present in Shiori's accepted architecture include:

```text
CatalogItemCreated
CatalogItemUpdated
CatalogItemRetired

PublicationUnitCreated
PublicationUnitUpdated
PublicationUnitRetired
```

The semantic direction is:

```text
Producer
   |
   | "This happened."
   v
RabbitMQ
```

not:

```text
Producer
   |
   | "Consumer, perform this exact implementation."
   v
RabbitMQ
```

An Integration Event does not dictate what a consumer must do internally.

For example:

```text
Catalog
   |
   | PublicationUnitCreated
   v
RabbitMQ
   |
   +------> Tracking
   |
   +------> Future capability
```

Tracking may update a projection.

A future Notification capability may evaluate whether a notification is needed.

Another future consumer may ignore the event entirely.

The producer does not branch on those consumers.

An Integration Event therefore describes a **fact**, not an instruction.

---

## 1.5 Integration Command

An **Integration Command** states:

> **Please perform a capability that your bounded context owns.**

Unlike an Integration Event, it is an explicit request for work.

Shiori's canonical existing example is Catalog hydration during Smart Staging Import.

Tracking may encounter identifiers that its local Catalog projection does not yet know.

Tracking must not query AniList or MangaDex directly because Catalog is Shiori's metadata Anti-Corruption Layer.

Therefore the direction is:

```text
Tracking
   |
   | Catalog hydration request
   v
RabbitMQ
   |
   v
Catalog
```

The semantic meaning is:

```text
Tracking:
"I need this Catalog-owned capability performed."
```

not:

```text
Tracking:
"I now own metadata ingestion."
```

The command does not transfer ownership and does not dictate Catalog's internal implementation.

Catalog remains free to satisfy that capability using its own:

- Application use cases.
- MongoDB state.
- AniList adapter.
- MangaDex adapter.
- Cache.
- Outbox.
- Worker topology.

The caller depends on the **contracted capability**, not on the implementation.

---

## 1.6 Strict Semantic Difference

The distinction is normative:

```text
DOMAIN EVENT
────────────────────────────────
Scope:
Inside one bounded context.

Meaning:
A meaningful internal domain fact.

RabbitMQ contract:
Not automatically.

Compatibility surface:
Internal unless explicitly promoted.


INTEGRATION EVENT
────────────────────────────────
Scope:
Across bounded contexts.

Meaning:
"This occurred."

Direction:
Producer announces a fact.

Ownership:
Remains with producer.

Consumers:
Unknown to producer.


INTEGRATION COMMAND
────────────────────────────────
Scope:
Across bounded contexts.

Meaning:
"Perform this capability."

Direction:
Caller requests foreign-owned work.

Ownership:
Capability remains with receiver.

Workflow ownership:
Remains explicitly defined.
```

The choice between Event and Command must be based on **semantics**, not on which class name is more convenient in code.

---

## 1.7 Commands Must Not Be Disguised Events

A capability request must not be renamed into past tense merely to make every RabbitMQ message look like an event.

For example, if Tracking needs Catalog to hydrate missing metadata, the semantic message is:

```text
Request Catalog hydration
```

not a fake fact such as:

```text
CatalogHydrationNeededOccurred
```

if the actual purpose is to ask Catalog to perform work.

Likewise, an already completed business fact must not be represented as a command merely because one current consumer happens to react to it.

For example:

```text
PublicationUnitCreated
```

is a fact.

It should not become:

```text
UpdateTrackingProjection
```

because that would couple Catalog to one consumer's implementation.

---

## 1.8 Integration Contracts Are Semantic Boundaries

Integration contracts must describe Shiori concepts rather than transport or persistence mechanics.

Correct conceptual contract:

```text
CatalogItemUpdated
```

Incorrect architectural direction:

```text
MongoDocumentChanged
```

Correct:

```text
PublicationUnitCreated
```

Incorrect:

```text
CollectionRowInserted
```

Correct:

```text
Catalog hydration request
```

Incorrect:

```text
CallAniListNow
```

The contract represents the business boundary.

It does not expose how the producer currently stores or retrieves the data.

---

## 1.9 Integration Contract vs Persistence Model

The following flow is accepted:

```text
Catalog Domain / Persistence
          |
          | mapping
          v
Integration Contract
          |
          | RabbitMQ
          v
Tracking Consumer
          |
          | mapping
          v
Tracking Projection Model
```

The following flow is prohibited:

```text
Catalog MongoDB Document
          |
          | serialize directly
          v
RabbitMQ
          |
          v
Tracking stores / depends on it
```

Tracking owns its projection representation.

Catalog owns its canonical representation.

The message between them is a third, explicit compatibility contract.

---

## 1.10 Integration Contracts Are Not Event Sourcing

RabbitMQ is not Shiori's historical source of truth.

Shiori has not adopted Event Sourcing.

Integration Events exist to communicate business facts between bounded contexts.

They do not replace:

- Catalog canonical state.
- Tracking current state.
- Tracking immutable progress history.
- Identity account state.

Therefore:

```text
RabbitMQ
!=
Shiori database

RabbitMQ
!=
historical audit store

Integration Event
!=
Event-Sourced aggregate history
```

---

# 2. Universal Envelopes & Metadata Rules

## 2.1 Purpose of the Envelope

Every Integration Event needs two distinct categories of information:

```text
Envelope metadata
+
Event-specific payload
```

The envelope answers questions such as:

```text
Which event is this?

Which contract does it follow?

Which Shiori resource changed?

Which state version produced it?

When did the fact occur?

Which distributed workflow does it belong to?

What caused it?
```

The payload answers:

```text
What business information does this
specific event contract expose?
```

The baseline Integration Event envelope contains:

- Event identifier.
- Event type.
- Event version.
- Aggregate identifier.
- Aggregate version.
- Occurrence timestamp.
- Correlation metadata.
- Causation metadata when available.

---

## 2.2 Conceptual Integration Event Envelope

The baseline Integration Event structure is:

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
  "payload": {
  }
}
```

This example is **conceptual**.

The semantics of the fields below are normative for this document.

Exact JSON Schema constraints, schema storage, enum policies, compatibility rules, and serialization validation belong to later STEP 5 sections.

No additional envelope fields are introduced here without an existing architectural requirement.

---

## 2.3 `eventId`

`eventId` identifies **one specific Integration Event occurrence**.

It answers:

> **Have I already processed this exact event?**

It does not identify:

- The Catalog Item.
- The publication unit.
- The import job.
- The aggregate's current state.
- The HTTP request.

Its primary current purpose is message idempotency.

Shiori assumes **at-least-once delivery**.

Therefore the same Integration Event may be delivered more than once.

Conceptually:

```text
CatalogItemUpdated

eventId = EVENT-A
```

may arrive:

```text
Delivery #1
EVENT-A

Delivery #2
EVENT-A

Delivery #3
EVENT-A
```

The consumer's Inbox recognizes the repeated `eventId`.

```text
Receive EVENT-A
      |
      v
Inbox contains EVENT-A?
      |
   +--+--+
   |     |
  YES    NO
   |     |
   v     v
duplicate process
effect    normally
```

Therefore:

> **A retry or redelivery of the same logical Integration Event preserves the same `eventId`.**

Generating a new `eventId` for every delivery attempt would defeat Inbox deduplication because the consumer could no longer recognize that it is seeing the same event again.

---

## 2.4 `eventType`

`eventType` identifies the semantic Integration Event contract.

Examples currently approved include:

```text
CatalogItemCreated
CatalogItemUpdated
CatalogItemRetired
PublicationUnitCreated
PublicationUnitUpdated
PublicationUnitRetired
```

`eventType` answers:

> **What business fact does this message represent?**

It must identify a semantic contract rather than:

- A queue.
- A consumer.
- A database operation.
- A persistence collection.
- A transport implementation.

For example:

```json
{
  "eventType": "CatalogItemUpdated"
}
```

means that the message follows the `CatalogItemUpdated` contract.

Its contract version is identified separately through:

```text
eventVersion
```

The two concepts must not be collapsed.

---

## 2.5 `aggregateId`

`aggregateId` identifies the Shiori-owned resource whose state produced the event.

It answers:

> **Which logical resource does this fact belong to?**

For example:

```text
Catalog Item X
```

may produce over time:

```text
CatalogItemCreated
aggregateId = X

CatalogItemUpdated
aggregateId = X

CatalogItemUpdated
aggregateId = X

CatalogItemRetired
aggregateId = X
```

Those are different event occurrences, so their:

```text
eventId
```

values differ.

Their:

```text
aggregateId
```

remains the same because the events concern the same Catalog Item.

Conceptually:

```text
eventId
   =
identity of the event occurrence

aggregateId
   =
identity of the business resource
```

Stable identifiers crossing bounded contexts must remain Shiori-owned identifiers rather than external-provider identifiers.

---

## 2.6 `aggregateVersion`

`aggregateVersion` identifies the version of the producer-owned aggregate state associated with the Integration Event.

It answers:

> **Is this event newer or older than the state I have already projected for this aggregate?**

This is distinct from duplicate detection.

Consider:

```text
CatalogItem X

aggregateVersion = 10
aggregateVersion = 11
aggregateVersion = 12
```

A consumer may receive:

```text
10
12
11
```

Shiori must not assume that correctness depends on perfect global message ordering.

If Tracking has already applied:

```text
aggregateVersion = 12
```

and later receives:

```text
aggregateVersion = 11
```

the consumer must not regress its local projection from version 12 back to version 11.

Conceptually:

```text
Incoming aggregateVersion = 11
Current projectionVersion = 12

11 <= 12
     |
     v
Do not regress projection
```

`aggregateVersion` therefore provides **state-order protection**.

---

## 2.7 `eventId` and `aggregateVersion` Solve Different Problems

These mechanisms are intentionally independent.

### Duplicate delivery

```text
eventId = A
aggregateVersion = 12

arrives twice
```

Inbox detects:

```text
same eventId
```

This is message idempotency.

### Different stale event

```text
Event A
eventId = A
aggregateVersion = 12

Event B
eventId = B
aggregateVersion = 11
```

These are not duplicate messages.

Their event IDs differ.

However, B represents an older aggregate state.

Aggregate-version checking prevents:

```text
projection v12
     ↓
projection v11
```

Therefore:

```text
eventId
    -> duplicate-event identity

aggregateVersion
    -> aggregate-state ordering
```

Neither replaces the other.

---

## 2.8 `eventVersion`

`eventVersion` identifies the version of the **Integration Event contract**.

It answers:

> **Which schema and semantic version of this event does the payload follow?**

For example:

```text
CatalogItemUpdated
eventVersion = 1
```

could later coexist with:

```text
CatalogItemUpdated
eventVersion = 2
```

if a future change requires a new incompatible contract.

`eventVersion` does **not** identify:

- The current Catalog Item revision.
- The number of times the item was changed.
- A deployment version.
- A Docker image version.
- A database migration.
- A RabbitMQ delivery attempt.

Therefore this is perfectly valid conceptually:

```json
{
  "eventType": "CatalogItemUpdated",
  "eventVersion": 1,
  "aggregateId": "01JCAT...",
  "aggregateVersion": 183
}
```

It means:

```text
Contract:
CatalogItemUpdated v1

Aggregate state:
revision/version 183
```

The distinction is:

```text
eventVersion
      |
      v
Integration Contract evolution


aggregateVersion
      |
      v
Business resource state evolution
```

This separation is mandatory.

---

## 2.9 `occurredAt`

`occurredAt` represents the instant when the business fact **occurred in the producing bounded context**.

It does not represent:

- RabbitMQ delivery time.
- Consumer processing time.
- Queue insertion time.
- Retry time.
- Dead-letter time.

Consider:

```text
19:05:00
Catalog commits state
+
Outbox fact
        |
        |
RabbitMQ temporarily unavailable
        |
        v
19:08:32
Outbox finally publishes event
```

The event still conceptually represents:

```text
occurredAt = 19:05:00
```

because that is when the producing bounded context durably accepted the business fact.

The asynchronous transport delay does not rewrite domain history.

Conceptually:

```text
occurredAt
       |
       v
business fact time

published/delivered/processed time
       |
       v
transport/operational time
```

Those concepts must remain separate.

---

## 2.10 `correlationId`

`correlationId` groups operations that belong to the same distributed workflow or originating request context.

It answers:

> **Which wider operation does this event belong to?**

For example:

```text
HTTP request
correlationId = A
      |
      v
Tracking operation
correlationId = A
      |
      v
Integration Command
correlationId = A
      |
      v
Catalog processing
correlationId = A
      |
      v
Integration Event
correlationId = A
```

The correlation identifier lets operational tooling relate:

```text
Gateway logs
Tracking logs
Outbox activity
RabbitMQ message processing
Catalog logs
Tracking consumer logs
```

to the same broader flow.

Therefore:

> **`correlationId` is observability metadata, not business authority.**

---

## 2.11 `causationId`

`causationId` identifies the immediate causal predecessor of the current integration message when such a predecessor is available.

It answers:

> **What directly caused this message to exist?**

This differs from `correlationId`.

Consider the conceptual workflow:

```text
                    correlationId = A

HTTP operation
      |
      v
Hydration Command
id = B
correlation = A
      |
      v
CatalogItemCreated
id = C
correlation = A
causation = B
```

Both B and C belong to the same larger workflow:

```text
correlationId = A
```

but C exists because of B:

```text
causationId = B
```

Therefore:

```text
correlationId
      |
      v
"What whole flow am I part of?"


causationId
      |
      v
"What immediately caused me?"
```

This produces a causal chain rather than only a flat grouping of logs.

---

## 2.12 Correlation and Causation Together

Consider a longer flow:

```text
Request A
   |
   | correlation = A
   v
Message B
   |
   | correlation = A
   | causation   = A-or-predecessor*
   v
Message C
   |
   | correlation = A
   | causation   = B
   v
Message D
   |
   | correlation = A
   | causation   = C
   v
Consumer effect
```

The result is:

```text
Correlation:
A -> B -> C -> D belong together.

Causation:
B caused C.
C caused D.
```

This improves debugging of workflows that move from:

```text
HTTP
   ↓
Outbox
   ↓
RabbitMQ
   ↓
Worker
   ↓
Database
```

without requiring any service to know the entire workflow implementation.

`*` The exact rule for how `causationId` is populated when the immediate predecessor is an HTTP operation rather than another integration message has **not yet been explicitly fixed** in the accepted documents. This document therefore does not invent that rule.

---

## 2.13 `payload`

`payload` contains the business data specific to:

```text
eventType
+
eventVersion
```

For example:

```text
CatalogItemUpdated v1
```

has a different payload contract from:

```text
PublicationUnitCreated v1
```

The payload must contain enough stable information for the event's declared integration purpose.

It must **not** automatically contain:

```text
the complete producer aggregate
```

and must never simply serialize:

```text
EF entity
MongoDB document
provider DTO
```

Conceptually:

```text
Producer internal model
        |
        | explicit mapping
        v
Contract payload
        |
        | consumer mapping
        v
Consumer local model
```

Exact payload schemas are defined later per event.

---

## 2.14 Metadata Relationship Summary

The envelope fields represent independent dimensions:

| Field | Answers |
|---|---|
| `eventId` | Which exact event occurrence is this? |
| `eventType` | What business fact does this contract represent? |
| `eventVersion` | Which version of that integration contract is this? |
| `aggregateId` | Which Shiori resource produced the fact? |
| `aggregateVersion` | Which state version of that resource does this event represent? |
| `occurredAt` | When did the fact occur in the producing bounded context? |
| `correlationId` | Which broader distributed workflow does it belong to? |
| `causationId` | Which immediate predecessor caused it? |
| `payload` | What event-specific business data is being communicated? |

The critical distinctions are:

```text
eventId
!=
aggregateId

eventVersion
!=
aggregateVersion

occurredAt
!=
delivery time

correlationId
!=
causationId
```

---

## 2.15 Duplicate and Out-of-Order Example

Suppose Tracking currently holds:

```text
CatalogItem projection
aggregateVersion = 41
```

RabbitMQ delivers:

```json
{
  "eventId": "EVENT-42",
  "eventType": "CatalogItemUpdated",
  "eventVersion": 1,
  "aggregateId": "CATALOG-A",
  "aggregateVersion": 42,
  "occurredAt": "2026-08-09T19:32:15Z",
  "correlationId": "FLOW-A",
  "causationId": "CAUSE-A",
  "payload": {
  }
}
```

Tracking may apply the event:

```text
41 -> 42
```

and record:

```text
EVENT-42
```

in its Inbox.

RabbitMQ later redelivers:

```text
EVENT-42
```

The Inbox identifies an exact duplicate.

No second business effect is applied.

Later RabbitMQ delivers:

```json
{
  "eventId": "EVENT-41",
  "eventType": "CatalogItemUpdated",
  "eventVersion": 1,
  "aggregateId": "CATALOG-A",
  "aggregateVersion": 41,
  "occurredAt": "2026-08-09T19:31:20Z",
  "correlationId": "FLOW-A",
  "causationId": "CAUSE-PREVIOUS",
  "payload": {
  }
}
```

This event is **not a duplicate** because:

```text
EVENT-41 != EVENT-42
```

but it is stale relative to the projection:

```text
incoming aggregateVersion = 41
current aggregateVersion  = 42
```

Therefore it must not move the projection backward.

---

## 2.16 Envelope Invariants

For Integration Events, Part 1 establishes the following invariants:

1. Every Integration Event has its own stable `eventId`.
2. Redelivery of the same event preserves the same `eventId`.
3. `eventId` supports Inbox/idempotency and does not identify the aggregate.
4. `eventType` identifies the semantic event contract.
5. `eventVersion` identifies contract evolution.
6. `aggregateId` identifies the Shiori-owned resource associated with the event.
7. `aggregateVersion` represents producer-state evolution where ordering protection is required.
8. `eventVersion` and `aggregateVersion` are independent.
9. Consumers must not regress applicable local state because an older aggregate version arrives late.
10. `occurredAt` represents business-fact occurrence, not RabbitMQ transport timing.
11. `correlationId` groups the wider distributed flow.
12. `causationId` identifies the immediate causal predecessor when available.
13. Correlation metadata is observability metadata and never authorization or identity proof.
14. Event payloads are explicit contracts rather than serialized persistence models.
15. Consumers map integration contracts into their own local models.
16. Producers do not know or branch on consumers.
17. Integration Events remain versioned compatibility contracts between independently deployable bounded contexts.
18. At-least-once delivery is assumed; exactly-once broker delivery is not assumed.
19. Correctness does not depend on global message ordering.
20. RabbitMQ messages do not transfer business ownership.

---


---

# 3. Integration Command Envelope

## 3.1 Purpose

An **Integration Command** is a versioned asynchronous request asking another bounded context to perform a capability that it owns.

Its envelope is intentionally parallel to the Integration Event envelope, but uses command-specific terminology so that the contract does not blur the semantic difference between:

```text
"This happened."
```

and:

```text
"Perform this capability."
```

Shiori therefore does **not** use a generic `messageId`, `messageType`, or `messageVersion` for both categories.

Integration Commands use:

```text
commandId
commandType
commandVersion
correlationId
causationId
payload
```

This keeps the asynchronous contract explicit at the serialization boundary.

The governing rule is:

> **The envelope must communicate whether the message is a fact or a request without requiring the Consumer to infer that meaning from the payload.**

---

## 3.2 Standard Integration Command Envelope

The standard conceptual command envelope is:

```json
{
  "commandId": "01JCMD...",
  "commandType": "HydrateCatalogItems",
  "commandVersion": 1,
  "correlationId": "7234d279-d290-4ab4-96dc-b01900bc11c8",
  "causationId": "01JCAUSE...",
  "payload": {
  }
}
```

The exact payload schema is defined by each individual command contract.

The envelope itself carries command identity, contract identity, compatibility version, and distributed tracing metadata.

---

## 3.3 `commandId`

`commandId` identifies one logical Integration Command request.

It answers:

> **Have I already handled this exact command request?**

A retry or RabbitMQ redelivery of the same logical command preserves the same `commandId`.

Conceptually:

```text
HydrateCatalogItems
commandId = COMMAND-A
```

may be delivered more than once:

```text
Delivery 1 -> COMMAND-A
Delivery 2 -> COMMAND-A
Delivery 3 -> COMMAND-A
```

The Consumer must be able to recognize that these deliveries represent the same logical request rather than three independent commands.

The command identifier therefore supports command-side idempotency and duplicate protection in the same architectural spirit that `eventId` supports Inbox deduplication for Integration Events.

A new logical command requires a new `commandId`.

A transport retry of the same logical command does not.

---

## 3.4 `commandType`

`commandType` identifies the semantic capability request represented by the command.

Example:

```json
{
  "commandType": "HydrateCatalogItems"
}
```

It answers:

> **What capability is the Producer requesting?**

The name describes the requested business capability.

It must not describe:

- A queue.
- A Worker implementation.
- A method name.
- A provider-specific HTTP call.
- A database operation.

Preferred conceptual form:

```text
HydrateCatalogItems
```

Rejected semantic direction:

```text
CallAniListBatch
RunCatalogWorker
InsertCatalogDocuments
ProcessQueueMessage
```

The command contract expresses the bounded-context capability, not the technical implementation chosen by the Consumer.

---

## 3.5 `commandVersion`

`commandVersion` identifies the compatibility version of the Integration Command contract.

Example:

```text
HydrateCatalogItems
commandVersion = 1
```

The value changes only when the command contract experiences a breaking schema or semantic change that cannot remain compatible with the existing version.

It does not follow:

- Service version.
- Assembly version.
- Docker image version.
- Database migration number.
- RabbitMQ delivery attempt.
- Import job revision.

Conceptually:

```text
Catalog Service 2.8.4
may still consume
HydrateCatalogItems commandVersion = 1
```

Contract versioning is governed by compatibility, not deployment numbering.

---

## 3.6 Why Commands Do Not Carry `aggregateId`

Integration Commands do not use the universal event field:

```text
aggregateId
```

because a command is not defined as a statement that one specific aggregate has changed.

A command requests execution of a capability.

For example, a Catalog hydration command may request work for a bounded batch of unresolved items:

```text
HydrateCatalogItems
    |
    +-- Item A
    +-- Item B
    +-- Item C
```

There is no single producer-owned aggregate whose state the command envelope is declaring.

Any resource identifiers required by the requested capability belong in that command's explicit `payload`.

This preserves a clean semantic distinction:

```text
Integration Event
    aggregateId
    =
    identity of the producer-owned resource
    whose state produced the fact

Integration Command
    payload resource identifiers
    =
    inputs required to perform the requested capability
```

---

## 3.7 Why Commands Do Not Carry `aggregateVersion`

Integration Commands also do not use:

```text
aggregateVersion
```

because `aggregateVersion` exists to protect Consumers from stale or out-of-order **state facts**.

An Integration Event may state:

```text
CatalogItemUpdated
aggregateVersion = 42
```

and a Consumer can compare that version against a local projection to prevent regression.

A command does not claim:

> "Aggregate X is now at version 42."

It asks:

> "Perform this capability."

Therefore there is no universal aggregate-state ordering value that belongs in the command envelope.

If a specific command requires an expected resource revision or another concurrency precondition as part of its business semantics, that information must be modeled explicitly inside that command's payload.

It must not be overloaded into a universal `aggregateVersion` field.

The governing rule is:

> **Event aggregate versions describe producer state evolution. Command payloads describe execution preconditions when a command actually needs them.**

---

## 3.8 Command Correlation and Causation

Integration Commands use the same distributed observability concepts already established for Integration Events:

```text
correlationId
causationId
```

`correlationId` groups the command into the wider distributed workflow.

`causationId` identifies the immediate causal predecessor when one is available.

Example:

```text
Import workflow
correlationId = FLOW-A
        |
        v
HydrateCatalogItems
commandId = COMMAND-B
correlationId = FLOW-A
causationId = PREDECESSOR-A
```

If Catalog later produces Integration Events because of this command, those events can continue:

```text
correlationId = FLOW-A
causationId = COMMAND-B
```

This creates an explicit causal chain while preserving the semantic difference between the command and the events produced as a consequence.

---

## 3.9 Command Envelope Invariants

The following rules are normative:

1. Every Integration Command has a stable `commandId`.
2. Redelivery of the same logical command preserves the same `commandId`.
3. A new logical execution request receives a new `commandId`.
4. `commandType` identifies the semantic requested capability.
5. `commandVersion` identifies the compatibility version of that command contract.
6. `correlationId` continues the wider distributed workflow.
7. `causationId` identifies the immediate causal predecessor when available.
8. Command payloads contain the explicit data needed to perform the requested capability.
9. Commands do not contain universal `aggregateId`.
10. Commands do not contain universal `aggregateVersion`.
11. Resource identifiers needed by a command belong to its payload.
12. Resource-version or concurrency preconditions, when a specific command requires them, belong to that command's payload.
13. Command envelopes do not expose Consumer implementation details.
14. Commands do not transfer business ownership.
15. RabbitMQ redelivery does not convert one logical command into multiple business requests.

---

# 4. Serialization & Naming Convention

## 4.1 Serialization Format

All Shiori Integration Events and Integration Commands transported through RabbitMQ use:

```text
JSON
UTF-8
```

The serialized message body must be valid JSON encoded as UTF-8.

The contract must not depend on:

- CLR binary serialization.
- .NET assembly-qualified type names.
- Native EF Core serialization.
- MongoDB BSON documents as the integration contract.
- Language-specific object serialization that another implementation cannot interpret independently.

The governing rule is:

> **A Shiori integration contract must remain understandable from its documented JSON contract without loading the Producer's application assembly.**

This preserves independent deployment and prevents cross-service implementation coupling.

---

## 4.2 JSON Property Naming

All JSON envelope and payload property names use:

```text
camelCase
```

Examples:

```json
{
  "eventId": "01JEVT...",
  "eventType": "CatalogItemUpdated",
  "eventVersion": 1,
  "aggregateId": "01JCAT...",
  "aggregateVersion": 42,
  "occurredAt": "2026-08-09T19:32:15Z",
  "correlationId": "FLOW-A",
  "causationId": "CAUSE-A",
  "payload": {
    "catalogItemId": "01JCAT..."
  }
}
```

Command example:

```json
{
  "commandId": "01JCMD...",
  "commandType": "HydrateCatalogItems",
  "commandVersion": 1,
  "correlationId": "FLOW-A",
  "causationId": "CAUSE-A",
  "payload": {
    "importJobId": "01JIMP..."
  }
}
```

This is consistent with Shiori's already accepted JSON convention in the public API, where machine-readable JSON property names use camelCase.

The integration layer therefore does not introduce an unrelated JSON naming style such as:

```text
PascalCase
snake_case
SCREAMING_SNAKE_CASE
```

---

## 4.3 Event Type Naming

Integration Event contract names use:

```text
PascalCase
```

and describe completed business facts in past tense.

Approved examples already used throughout Shiori architecture are:

```text
CatalogItemCreated
CatalogItemUpdated
CatalogItemRetired
PublicationUnitCreated
PublicationUnitUpdated
PublicationUnitRetired
UserLibraryImportCompleted
```

Therefore the serialized value is:

```json
{
  "eventType": "CatalogItemUpdated"
}
```

not:

```json
{
  "eventType": "catalog-item-updated"
}
```

and not:

```json
{
  "eventType": "catalog_item_updated"
}
```

This convention keeps the contract names aligned with the semantic names already established in `ADR.md` and `ROADMAP.md`.

The name must describe the business fact.

It must not encode:

- Producer service version.
- Consumer name.
- Queue name.
- Database collection.
- Transport implementation.

Incorrect:

```text
CatalogV2TrackingItemUpdate
TrackingProjectionMessage
MongoCatalogItemChanged
RabbitCatalogUpdated
```

Correct:

```text
CatalogItemUpdated
```

---

## 4.4 Command Type Naming

Integration Command contract names also use:

```text
PascalCase
```

but use an imperative capability-oriented name rather than past tense.

Conceptual example:

```text
HydrateCatalogItems
```

Serialized:

```json
{
  "commandType": "HydrateCatalogItems"
}
```

An Integration Command name must communicate:

> **What capability should the Consumer perform?**

It must not pretend that the requested work has already happened.

Therefore:

```text
HydrateCatalogItems
```

is semantically a command.

Whereas:

```text
CatalogItemsHydrated
```

would describe a completed fact and would therefore be an event if such a fact were intentionally published.

---

## 4.5 Type Name and Version Are Separate

The contract version is never embedded into the type string.

Correct:

```json
{
  "eventType": "CatalogItemUpdated",
  "eventVersion": 1
}
```

Incorrect:

```json
{
  "eventType": "CatalogItemUpdatedV1"
}
```

Correct:

```json
{
  "commandType": "HydrateCatalogItems",
  "commandVersion": 1
}
```

Incorrect:

```json
{
  "commandType": "HydrateCatalogItemsV1"
}
```

Separating name and version prevents version parsing from becoming dependent on naming conventions and keeps contract identity explicit.

Conceptually:

```text
Contract identity:
CatalogItemUpdated

Compatibility version:
1
```

---

## 4.6 C# Names and Serialized JSON Are Separate Concerns

An implementation may represent a contract internally using C# PascalCase properties, for example:

```csharp
public sealed record IntegrationEventEnvelope(
    string EventId,
    string EventType,
    int EventVersion);
```

The serialized contract remains:

```json
{
  "eventId": "...",
  "eventType": "...",
  "eventVersion": 1
}
```

A C# refactor must not silently change the JSON contract.

The Consumer depends on the documented serialized contract, not on the Producer's CLR member names.

---

## 4.7 Contract Property Names Are Compatibility Surface

Every JSON property name is part of the versioned integration contract.

For example:

```json
{
  "catalogItemId": "01JCAT..."
}
```

must not later become:

```json
{
  "itemId": "01JCAT..."
}
```

inside the same contract version merely because an internal C# property or database column was renamed.

Persistence and implementation names may change independently.

Integration JSON names remain stable until the compatibility policy permits a versioned contract change.

---

## 4.8 Consumer Deserialization Expectations

Consumers must deserialize according to:

```text
message category
+
type
+
contract version
```

For Integration Events:

```text
eventType
+
eventVersion
```

For Integration Commands:

```text
commandType
+
commandVersion
```

Conceptually:

```text
CatalogItemUpdated
+
1
        |
        v
CatalogItemUpdated v1 contract
```

and:

```text
HydrateCatalogItems
+
1
        |
        v
HydrateCatalogItems v1 contract
```

A Consumer must not select a payload schema by:

- Queue name alone.
- C# runtime type metadata.
- Guessing based on payload fields.
- Database entity names.

This makes deserialization predictable and independently testable.

---

## 4.9 Naming and Serialization Invariants

The following rules are normative:

1. RabbitMQ integration message bodies use JSON encoded as UTF-8.
2. JSON properties use camelCase.
3. Integration Event type values use PascalCase.
4. Integration Event names describe completed business facts.
5. Integration Command type values use PascalCase.
6. Integration Command names describe requested capabilities.
7. Contract versions are numeric metadata fields, not suffixes embedded into type names.
8. C# property naming does not redefine serialized JSON naming.
9. JSON property names are part of the compatibility contract.
10. Persistence model names are not integration-contract names.
11. Provider DTO names are not integration-contract names.
12. Consumers select contracts explicitly by type plus contract version.
13. Consumers must not require Producer CLR assemblies to interpret messages.
14. Cross-service contracts never use arbitrary language-specific object serialization.

---

# 5. Versioning & Backward Compatibility Policy

## 5.1 Purpose

Shiori services are independently deployable.

Therefore a Producer and Consumer must not be required to deploy simultaneously merely because one contract evolves.

The purpose of integration-contract versioning is to preserve that independence.

The governing principle is:

> **A contract version changes because compatibility changes, not because the service was redeployed.**

Examples:

```text
Catalog service release: 2.4.8
CatalogItemUpdated eventVersion: 1
```

and:

```text
Tracking service release: 1.9.3
Consumer support: CatalogItemUpdated v1
```

are completely valid.

A new service release does not imply a new event or command version.

---

## 5.2 Compatibility Classification

Every change to a published Integration Event or Integration Command contract must be classified before it is accepted as either:

```text
BACKWARD COMPATIBLE
```

or:

```text
BREAKING
```

A backward-compatible change may remain within the existing contract version.

A breaking change must not silently redefine the existing version.

Example:

```text
CatalogItemUpdated v1
```

must continue meaning the same thing for Consumers already built against v1.

---

## 5.3 Rule of Gold for Existing Versions

Once a contract version has been published and may have Consumers, its existing required structure and semantics are considered stable.

The preferred evolution model is:

```text
existing contract
+
additive compatible information
```

rather than:

```text
rewrite existing contract in place
```

The key question is:

> **Can an existing conforming Consumer continue to understand and safely process the message after this change?**

If the answer is no, the change is breaking.

---

## 5.4 Compatible Change — Add Optional Property

Adding a new optional property is the primary compatible schema evolution allowed within the same version.

Before:

```json
{
  "catalogItemId": "01JCAT...",
  "mediaType": "manga"
}
```

Later, still compatible with `eventVersion: 1`:

```json
{
  "catalogItemId": "01JCAT...",
  "mediaType": "manga",
  "trackingCapability": "reading"
}
```

This is compatible only when:

- Existing Consumers may safely ignore the new property.
- The property is not required to correctly interpret existing fields.
- The absence of the new property preserves the previous contract semantics.
- The Producer does not reinterpret an old field because the new property now exists.

Consumers of compatible contract versions must tolerate unknown additive JSON properties.

---

## 5.5 Compatible Change — New Optional Nested Data

The same rule applies to optional nested structures.

For example, if an existing v1 payload is:

```json
{
  "catalogItemId": "01JCAT..."
}
```

an additive optional structure may remain compatible:

```json
{
  "catalogItemId": "01JCAT...",
  "releaseTrack": {
    "trackId": "officialEnglish"
  }
}
```

provided an older Consumer can ignore `releaseTrack` and continue processing the original meaning correctly.

A nested object is not automatically compatible merely because it is additive.

Compatibility depends on whether old Consumers can safely ignore it.

---

## 5.6 Compatible Change — Add New Contract

Adding an entirely new Integration Event or Integration Command type does not alter an existing contract.

For example:

```text
Existing:
CatalogItemCreated v1

Later:
PublicationUnitCreated v1
```

does not require:

```text
CatalogItemCreated v2
```

Each contract has its own independent lifecycle.

Likewise, adding a new command:

```text
HydrateCatalogItems v1
```

does not change the version of unrelated Integration Events.

---

## 5.7 Breaking Change — Rename Property

Renaming a property is breaking.

Before:

```json
{
  "catalogItemId": "01JCAT..."
}
```

Breaking in place:

```json
{
  "itemId": "01JCAT..."
}
```

Even when the underlying value is identical, an existing Consumer may no longer deserialize the expected property.

Therefore this cannot silently redefine the same published contract version.

---

## 5.8 Breaking Change — Remove Property

Removing an existing property that belongs to the published contract is breaking when existing Consumers may depend on it.

Before:

```json
{
  "catalogItemId": "01JCAT...",
  "mediaType": "manga"
}
```

Breaking:

```json
{
  "catalogItemId": "01JCAT..."
}
```

A Consumer using `mediaType` would no longer receive the information promised by the contract.

---

## 5.9 Breaking Change — Change Property Type

Changing a property's serialized type is breaking.

Before:

```json
{
  "aggregateVersion": 42
}
```

Breaking:

```json
{
  "aggregateVersion": "42"
}
```

Another example:

Before:

```json
{
  "chapter": "10.5"
}
```

Breaking:

```json
{
  "chapter": 10.5
}
```

Even when the values look equivalent, Consumers compiled against the previous schema may fail or reinterpret the data incorrectly.

---

## 5.10 Breaking Change — Optional to Required

A property introduced as optional must not later become required inside the same contract version if previously valid messages or Producer states may omit it.

Conceptually:

```text
v1 originally:
trackingCapability = optional
```

must not silently become:

```text
v1 later:
trackingCapability = required
```

because existing Consumers and historical Outbox messages may have been built around the previous contract.

If the new requirement changes compatibility, it requires a new contract version or an additive redesign.

---

## 5.11 Breaking Change — Change Semantic Meaning

A contract may remain structurally identical and still break compatibility.

Example:

```json
{
  "status": "completed"
}
```

If `status` originally means:

```text
Canonical Catalog lifecycle status
```

it cannot later silently mean:

```text
Tracking user's library status
```

while keeping the same property name and contract version.

Semantics are part of the contract just as much as JSON shape.

The same applies to units.

For example:

```json
{
  "elapsed": 120
}
```

cannot silently change from:

```text
seconds
```

to:

```text
milliseconds
```

inside the same version.

A semantic reinterpretation is breaking even if the JSON type does not change.

---

## 5.12 Breaking Change — Change Identifier Semantics

Stable Shiori identity semantics must not be replaced inside an active contract version.

For example, if:

```json
{
  "catalogItemId": "01JCAT..."
}
```

means a canonical Shiori `CatalogItemId`, the same property must not later contain:

```text
AniList ID
MangaDex ID
provider-specific ID
```

merely because those values can also be serialized as strings.

The serialized type may still be `string`, but the identity contract has changed.

That is a breaking semantic change.

---

## 5.13 Breaking Change — Change Type Identity

Changing:

```text
eventType = CatalogItemUpdated
```

to:

```text
eventType = CatalogMediaUpdated
```

is not a harmless rename.

The type name identifies the semantic contract.

If the new name represents a genuinely new contract, it must be introduced explicitly rather than silently replacing the previous type identity.

Likewise:

```text
commandType = HydrateCatalogItems
```

must not be silently renamed in place after publication.

---

## 5.14 Version Increment Rule

A new major contract version is required when a necessary change is breaking and cannot reasonably be introduced additively.

Example:

```text
CatalogItemUpdated
eventVersion = 1
```

may evolve to:

```text
CatalogItemUpdated
eventVersion = 2
```

when v2 intentionally introduces an incompatible schema or semantic boundary.

Likewise:

```text
HydrateCatalogItems
commandVersion = 1
```

may later require:

```text
HydrateCatalogItems
commandVersion = 2
```

The new version does not erase the existence of the previous one.

Migration and deployment behavior for coexistence, dual support, and eventual retirement is defined in the later compatibility/deployment section of STEP 5.

This section establishes only the compatibility boundary.

---

## 5.15 Versioning Is Per Contract

Versions are tracked independently for each Integration Event and Integration Command.

Valid state:

```text
CatalogItemCreated v1
CatalogItemUpdated v2
CatalogItemRetired v1

PublicationUnitCreated v1

HydrateCatalogItems v1
```

There is no global:

```text
Shiori Event Version = 2
```

that forces every contract to increment simultaneously.

Each contract evolves only when its own compatibility boundary changes.

---

## 5.16 Producer Responsibility

A Producer must not silently emit a breaking payload under an existing contract version.

Before publishing a contract change, the Producer side must classify the change.

Conceptually:

```text
Proposed contract change
        |
        v
Compatibility review
        |
   +----+----+
   |         |
Compatible  Breaking
   |         |
   v         v
same        new version
version     or redesign
```

The Producer owns the responsibility of not redefining published semantics without an explicit version transition.

---

## 5.17 Consumer Responsibility

Consumers of an existing compatible version must not be unnecessarily brittle.

For compatible additive evolution, Consumers must be able to tolerate unknown optional JSON properties.

A v1 Consumer should therefore not fail merely because a v1 Producer added an optional field that the Consumer does not use.

Conceptually:

```json
{
  "catalogItemId": "01JCAT...",
  "newOptionalField": "value"
}
```

An old Consumer that only requires:

```text
catalogItemId
```

should continue processing the contract.

This tolerance is a requirement for additive independent deployment.

It does not mean Consumers should ignore:

- Missing required properties.
- Invalid types.
- Unknown contract versions.
- Semantically invalid values.

Those are different compatibility conditions.

---

## 5.18 Do Not Reuse a Published Version for New Semantics

Once:

```text
CatalogItemUpdated v1
```

has been published, `v1` remains a historical compatibility boundary.

Shiori must never treat version numbers as reusable labels.

Incorrect:

```text
2026:
CatalogItemUpdated v1 = meaning A

2027:
rewrite v1 = meaning B
```

Correct:

```text
CatalogItemUpdated v1 = preserve meaning A

CatalogItemUpdated v2 = introduce incompatible meaning B
```

This remains true even if all currently known Consumers appear to have upgraded.

Contract history must remain explicit rather than silently rewritten.

---

## 5.19 Compatible vs Breaking Summary

| Change | Same version allowed? | Classification |
|---|---:|---|
| Add optional property old Consumers may ignore | Yes | Compatible |
| Add optional nested object old Consumers may ignore | Yes | Compatible |
| Add a completely new event/command contract | Independent contract | Compatible with existing contracts |
| Rename existing property | No | Breaking |
| Remove existing required/contracted property | No | Breaking |
| Change property type | No | Breaking |
| Change optional property to required | No | Breaking |
| Change field meaning | No | Breaking |
| Change units or interpretation | No | Breaking |
| Change canonical identifier semantics | No | Breaking |
| Rename published event/command type | No | Breaking |
| Change service implementation only | Yes | Not a contract change |
| Change database schema without changing contract | Yes | Not a contract change |
| Deploy a new Producer/Consumer version | Yes | Not automatically a contract change |

---

## 5.20 Versioning and Compatibility Invariants

The following rules are normative:

1. Event and command versions represent compatibility boundaries, not deployment versions.
2. Every published contract evolves independently.
3. Every contract change must be reviewed as `BACKWARD COMPATIBLE` or `BREAKING`.
4. Compatible additive changes may remain inside the same version.
5. Adding an optional field is compatible only when old Consumers may safely ignore it.
6. Consumers must tolerate unknown additive optional properties within a supported compatible version.
7. Renaming an existing property is breaking.
8. Removing an existing contracted property is breaking.
9. Changing a property's serialized type is breaking.
10. Changing optional input/data to required is breaking when previously valid messages may omit it.
11. Changing the semantic meaning of an existing property is breaking.
12. Changing units while preserving the same JSON type is still breaking.
13. Changing canonical identifier semantics is breaking.
14. Renaming a published event or command type is breaking.
15. A necessary breaking change requires a new contract version or an additive redesign.
16. Published contract versions are never silently redefined or reused for incompatible semantics.
17. A service deployment does not require a contract-version increment when the contract remains compatible.
18. Database or internal-model changes do not require a contract-version increment when serialized semantics remain unchanged.
19. There is no global Shiori message version; versioning is per contract.
20. Version compatibility exists to preserve independent deployment between Producers and Consumers.

---

# 6. Catalog Event Contracts — Items & Publication Units

## 6.1 Purpose

Catalog publishes Integration Events so Tracking can maintain the minimum Catalog projection required for Tracking-owned behavior without synchronously calling Catalog after a message arrives.

The current projection lifecycle is:

```text
CatalogItemCreated
CatalogItemUpdated
CatalogItemRetired

PublicationUnitCreated
PublicationUnitUpdated
PublicationUnitRetired
```

These contracts are **projection contracts**, not replicas of Catalog's MongoDB documents.

They carry only the Catalog-owned state that Tracking needs locally, including the relevant combination of:

- Stable Shiori identifiers.
- Media / progress information required by Tracking.
- Publication-unit identity.
- Release-track values required by Tracking.
- Retirement state.
- Aggregate version through the universal event envelope.

The governing rule is:

> **A Catalog Integration Event must contain enough current semantic state for its declared projection purpose without forcing Tracking to call Catalog back and without serializing Catalog persistence models wholesale.**

---

## 6.2 State-Carried Snapshot Rule

`Created` and `Updated` contracts carry a **snapshot of the current projection-relevant state**.

`Updated` is explicitly **not** a diff or patch.

Rejected:

```json
{
  "changedFields": [
    "releaseTracks"
  ]
}
```

Rejected:

```json
{
  "operations": [
    {
      "op": "replace",
      "path": "/releaseTracks/0/latestKnownUnit",
      "value": "..."
    }
  ]
}
```

Accepted conceptual direction:

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

The Consumer replaces or reconciles the projection subset represented by that event using the incoming aggregate version.

This reduces dependence on receiving every intermediate mutation in order.

For example:

```text
Catalog state v40
      |
      v
Catalog state v41
      |
      v
Catalog state v42
```

If Tracking receives a valid current-state snapshot for v42 after v40, it can converge to the relevant v42 projection state without needing to replay a field-by-field patch from v41 first.

This does **not** turn the event into a complete Catalog snapshot.

Only the state explicitly belonging to the integration contract is carried.

---

## 6.3 CatalogItem Projection Snapshot v1

The Catalog Item event family carries the current Tracking-relevant Catalog Item state.

The conceptual v1 snapshot is:

```json
{
  "mediaType": "manga",
  "trackingCapability": "reading",
  "releaseTracks": [
    {
      "trackId": "originalRelease",
      "supportStatus": "supported",
      "stalenessStatus": "current",
      "unitType": "chapter",
      "latestKnownUnit": {
        "publicationUnitId": "01JUNIT-RAW...",
        "label": "81"
      },
      "source": "provider-or-catalog-source",
      "lastVerifiedAt": "2026-08-09T19:30:00Z"
    },
    {
      "trackId": "officialEnglish",
      "supportStatus": "supported",
      "stalenessStatus": "current",
      "unitType": "chapter",
      "latestKnownUnit": {
        "publicationUnitId": "01JUNIT-EN...",
        "label": "74"
      },
      "source": "provider-or-catalog-source",
      "lastVerifiedAt": "2026-08-09T19:31:00Z"
    }
  ],
  "isRetired": false
}
```

The Catalog Item identifier is **not duplicated inside the payload**.

For this event family:

```text
envelope.aggregateId
=
CatalogItemId
```

This avoids two competing identity fields that could disagree.

`releaseTracks` represents the bounded release state Tracking needs locally. It does not expose the complete provider model.

The current architecture requires release-track information such as:

- Track identity.
- Support state.
- Staleness state.
- Unit type.
- Latest known relevant unit.
- Source / provenance information.
- Verification timing.

Exact JSON Schema constraints and enum openness are defined later in STEP 5.

---

## 6.4 `CatalogItemCreated.v1`

### Meaning

`CatalogItemCreated.v1` means:

> **A canonical Shiori Catalog Item now exists and the payload contains its current Tracking-relevant projection state.**

### Producer

```text
Catalog
```

### Current Consumer

```text
Tracking
```

### Aggregate

```text
aggregateId
=
CatalogItemId
```

### Conceptual Payload

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

Tracking can use this fact to create or reconcile its local `catalog_item_registry` row without issuing an HTTP request back to Catalog.

---

## 6.5 `CatalogItemUpdated.v1`

### Meaning

`CatalogItemUpdated.v1` means:

> **The current Tracking-relevant state of an existing canonical Catalog Item changed.**

It does **not** mean:

> Any field anywhere in the Catalog MongoDB document changed.

### Snapshot Semantics

The payload uses the same projection-snapshot shape as `CatalogItemCreated.v1`.

Example:

```json
{
  "mediaType": "manga",
  "trackingCapability": "reading",
  "releaseTracks": [
    {
      "trackId": "originalRelease",
      "supportStatus": "supported",
      "stalenessStatus": "current",
      "unitType": "chapter",
      "latestKnownUnit": {
        "publicationUnitId": "01JUNIT-RAW-NEW...",
        "label": "82"
      },
      "source": "provider-or-catalog-source",
      "lastVerifiedAt": "2026-08-10T01:05:00Z"
    },
    {
      "trackId": "officialEnglish",
      "supportStatus": "supported",
      "stalenessStatus": "current",
      "unitType": "chapter",
      "latestKnownUnit": {
        "publicationUnitId": "01JUNIT-EN...",
        "label": "74"
      },
      "source": "provider-or-catalog-source",
      "lastVerifiedAt": "2026-08-09T19:31:00Z"
    }
  ],
  "isRetired": false
}
```

The Consumer does not apply a patch such as:

```text
latest chapter += 1
```

It evaluates the incoming current-state snapshot against the local aggregate version and updates the projection to the state represented by the event.

---

## 6.6 When `CatalogItemUpdated.v1` Must Be Emitted

A Catalog Item update produces `CatalogItemUpdated.v1` when the committed Catalog mutation changes **state represented by the integration contract or otherwise required for Tracking projection correctness**.

Current examples include:

```text
Release-track latest-known unit changed.
Release-track support state changed.
Release-track staleness state changed.
Release-track source/provenance relevant to the projected track changed.
Release-track verification state/timing changed where it affects the projected release state.
Media/progress classification required by Tracking changed.
Tracking capability changed.
Other current projection state carried by CatalogItemUpdated.v1 changed.
```

The key test is:

> **Would failing to publish this committed change leave Tracking's approved local Catalog projection incorrect or stale?**

If yes, Catalog writes the corresponding Outbox event as part of the local durable decision.

---

## 6.7 When `CatalogItemUpdated.v1` Must Not Be Emitted

A Catalog mutation does **not** automatically produce `CatalogItemUpdated.v1` merely because the Catalog document changed.

For example, a correction such as:

```text
Synopsis:
"Thier journey..."
        ↓
"Their journey..."
```

does not justify a Tracking projection event when synopsis text is not part of the Tracking projection contract.

Likewise, purely presentation-oriented Catalog changes that are not represented in the Tracking projection do not require this event solely to announce that "something changed."

Examples may include changes to:

- Synopsis wording.
- Banner presentation data.
- Character-preview presentation data.
- Trailer presentation data.
- Other Catalog-only metadata that Tracking does not project.

The principle is:

```text
Catalog internal mutation
        |
        v
Does downstream projection semantics change?
        |
   +----+----+
   |         |
  YES        NO
   |         |
   v         v
Emit       No CatalogItemUpdated
event      projection event
```

This prevents RabbitMQ from becoming a generic MongoDB change feed.

---

## 6.8 `CatalogItemRetired.v1`

### Meaning

`CatalogItemRetired.v1` means:

> **The canonical Catalog Item is retired and Consumers must no longer treat it as an active Catalog Item.**

Retirement is a Catalog fact.

It is **not** a command telling Tracking to delete user history.

Tracking continues owning:

- User library state.
- Progress.
- Ratings.
- Consumption dates.
- Immutable progress history.

### Tombstone Snapshot

The v1 payload is a projection-level tombstone snapshot.

Conceptually:

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
  "isRetired": true
}
```

The envelope carries:

```text
aggregateId = CatalogItemId
aggregateVersion = retirement-producing Catalog Item version
```

The Consumer may retain the projected row as retired/tombstoned rather than physically deleting all knowledge of the identifier.

### Deliberately Not Defined in v1

This contract does not invent semantics for:

- Item merges.
- Replacement Catalog Items.
- Redirect targets.
- Franchise regrouping.
- Provider removal resolution.

Those behaviors remain separate architecture decisions.

`CatalogItemRetired.v1` communicates retirement only.

---

## 6.9 Publication Unit Projection Snapshot v1

Publication Unit events maintain Tracking's local unit registry used for granular progress validation.

The conceptual v1 projection snapshot is intentionally small:

```json
{
  "catalogItemId": "01JCAT...",
  "unitType": "chapter",
  "label": "10.5",
  "volumeUnitId": "01JVOLUME...",
  "isRetired": false
}
```

For a unit where no volume relationship applies, `volumeUnitId` is omitted.

The Publication Unit identifier itself is carried by:

```text
envelope.aggregateId
=
PublicationUnitId
```

The payload therefore does not duplicate `publicationUnitId`.

The contract preserves labels as strings because Shiori supports reading-unit labels such as:

```text
0
10.5
Extra
Special
One-shot
named interludes
```

The contract is not a complete publication-history representation.

It carries the state Tracking requires to recognize and validate the unit locally.

---

## 6.10 `PublicationUnitCreated.v1`

### Meaning

`PublicationUnitCreated.v1` means:

> **A canonical Publication Unit now exists in Catalog and the payload contains the current state required by Tracking's local unit projection.**

### Conceptual Payload

```json
{
  "catalogItemId": "01JCAT...",
  "unitType": "chapter",
  "label": "74",
  "volumeUnitId": "01JVOLUME...",
  "isRetired": false
}
```

Tracking can use the event to create or reconcile its local `catalog_unit_registry` representation.

The event does not require Tracking to fetch the unit from Catalog after consumption.

### Semantic Boundary

`PublicationUnitCreated` means that the canonical unit exists in Catalog.

It does **not by itself** claim a stronger future notification semantic such as:

> The user should now be notified that this unit became officially available in their market.

If a future feature requires a more specific verified-release business fact, that contract must be defined explicitly rather than silently changing the meaning of `PublicationUnitCreated.v1`.

---

## 6.11 `PublicationUnitUpdated.v1`

### Meaning

`PublicationUnitUpdated.v1` means:

> **The current Tracking-relevant projection state of an existing Publication Unit changed.**

Like `CatalogItemUpdated.v1`, it carries a snapshot, not a patch.

Conceptual payload:

```json
{
  "catalogItemId": "01JCAT...",
  "unitType": "chapter",
  "label": "10.5",
  "volumeUnitId": "01JVOLUME...",
  "isRetired": false
}
```

A previous unit state such as:

```json
{
  "label": "10"
}
```

is not required in the message.

Tracking compares the incoming aggregate version and applies the current v1 snapshot when eligible.

---

## 6.12 When `PublicationUnitUpdated.v1` Must Be Emitted

The event is emitted when a committed mutation changes Publication Unit state that Tracking projects or relies on for local progress validation.

Examples include:

```text
A chapter/unit label is corrected.
The unit type represented in the projection changes.
The projected volume association for a reading unit changes.
Other state carried by PublicationUnitUpdated.v1 changes.
```

A transition into retirement uses:

```text
PublicationUnitRetired
```

rather than representing retirement as an ordinary update.

The rule remains:

> **Emit when the local Tracking unit projection would otherwise become incorrect.**

---

## 6.13 When `PublicationUnitUpdated.v1` Must Not Be Emitted

Catalog does not publish this event for every provider or persistence mutation affecting a Publication Unit.

If a change does not alter the state represented by the Tracking projection contract, it does not require a `PublicationUnitUpdated.v1` solely to signal generic change.

Likewise, when a release-track summary changes at the Catalog Item level but the Publication Unit's own projected identity/state does not change, the appropriate downstream synchronization is:

```text
CatalogItemUpdated
```

not a synthetic `PublicationUnitUpdated` merely to duplicate the same release-track change.

---

## 6.14 `PublicationUnitRetired.v1`

### Meaning

`PublicationUnitRetired.v1` means:

> **The Publication Unit is retired in canonical Catalog state and must not remain active in Tracking's local unit projection.**

### Conceptual Tombstone Payload

```json
{
  "catalogItemId": "01JCAT...",
  "unitType": "chapter",
  "label": "74",
  "volumeUnitId": "01JVOLUME...",
  "isRetired": true
}
```

The envelope carries:

```text
aggregateId = PublicationUnitId
aggregateVersion = retirement-producing Publication Unit version
```

Tracking may preserve the local row as retired/tombstoned to maintain stable references and historical integrity.

The event does not authorize deletion of Tracking-owned progress history.

---

## 6.15 Item and Unit Event Emission Matrix

| Catalog mutation | Integration contract |
|---|---|
| New Catalog Item becomes canonical | `CatalogItemCreated.v1` |
| Tracking-relevant Catalog Item projection state changes | `CatalogItemUpdated.v1` |
| Catalog Item becomes retired | `CatalogItemRetired.v1` |
| Synopsis typo corrected only | No Tracking projection event |
| Banner/character/trailer presentation-only change | No Tracking projection event unless it later becomes part of the approved projection contract |
| New Publication Unit becomes canonical | `PublicationUnitCreated.v1` |
| Tracking-relevant Publication Unit projection state changes | `PublicationUnitUpdated.v1` |
| Publication Unit becomes retired | `PublicationUnitRetired.v1` |
| Catalog Item release-track snapshot changes | `CatalogItemUpdated.v1` |
| Internal/provider mutation with no change to the approved projection contract | No projection event |

---

## 6.16 Catalog Event Contract Invariants

1. Catalog remains the authoritative owner of Catalog Items and Publication Units.
2. Tracking owns only its local projection representation.
3. `Created` events carry current projection-relevant state.
4. `Updated` events carry current projection-relevant state.
5. `Updated` events are snapshots, not diffs or patches.
6. Consumers do not call Catalog back after every event to discover the state that changed.
7. Catalog persistence models are never serialized directly as integration payloads.
8. Catalog emits projection events only for committed changes relevant to the approved downstream projection semantics.
9. Purely Catalog-internal/presentation changes do not generate generic projection events.
10. `CatalogItemUpdated.v1` is mandatory when release-track or other Tracking-relevant item state changes.
11. Publication Unit labels remain string-capable because Shiori supports irregular reading labels.
12. Retirement is represented explicitly through `Retired` events.
13. Retirement does not command Tracking to delete user-owned history.
14. `aggregateVersion` protects projection monotonicity for all six lifecycle contracts.
15. Current event contracts remain intentionally smaller than the complete Catalog domain model.

---

# 7. Import Integration Contracts & Tracking Event Review

## 7.1 Import Contract Boundary

Tracking owns the Smart Staging Import workflow.

Catalog owns provider-backed metadata hydration.

The asynchronous boundary is:

```text
Tracking Import
      |
      | HydrateCatalogItems command
      v
RabbitMQ
      |
      v
Catalog
      |
      | normal Catalog lifecycle events
      v
RabbitMQ
      |
      v
Tracking projection
```

Hydrated Catalog state returns to Tracking through the same normal Catalog lifecycle events used outside imports.

The import workflow does not receive a private Catalog database path or direct AniList/MangaDex access.

---

## 7.2 `HydrateCatalogItems.v1`

### Category

```text
Integration Command
```

### Producer

```text
Tracking
```

### Consumer / Capability Owner

```text
Catalog
```

### Meaning

`HydrateCatalogItems.v1` means:

> **Resolve and hydrate this bounded batch of source references through Catalog's existing metadata capability so canonical Shiori Catalog state can be created or updated where possible.**

The command does not instruct Catalog:

- Which provider HTTP endpoint to call.
- Which database collection to write.
- Which Worker must process the command.
- Which Catalog lifecycle event must be fabricated.

Catalog remains responsible for its own Anti-Corruption Layer, provider policies, canonical identifiers, persistence, and Outbox publication.

---

## 7.3 `HydrateCatalogItems.v1` Conceptual Payload

The conceptual v1 payload is:

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

### `importJobId`

Identifies the Tracking-owned durable import workflow that requested the batch.

Catalog does not become the owner of the import job.

The identifier is carried so asynchronous result/correlation handling can remain tied to the correct Tracking workflow.

### `sourceType`

Identifies the source namespace represented by the item identifiers in this hydration batch.

The current import architecture supports MyAnimeList exports and AniList-compatible import flows.

The field describes the import/source identifier namespace.

It does not make that source the canonical Shiori identity.

### `items`

Contains the bounded set of unresolved source references that Tracking is asking Catalog to hydrate.

### `sourceItemId`

Represents the source-specific identifier known by the import parser.

It is not a canonical Shiori `CatalogItemId`.

Catalog remains responsible for resolving the source reference into canonical Shiori Catalog state when possible.

### `mediaType`

Carries the media classification known by the import workflow when available and required by the v1 command contract to contextualize the source reference.

It does not override Catalog's responsibility to normalize canonical metadata.

---

## 7.4 Hydration Command Identity and Correlation

One command envelope represents one logical hydration batch.

Therefore:

```text
commandId
=
identity of this hydration batch request
```

RabbitMQ redelivery preserves the same `commandId`.

A retry caused by transport failure must not accidentally become a second independent hydration request.

The wider import workflow continues through:

```text
correlationId
```

and any result message caused directly by the hydration command can identify the command through:

```text
causationId = HydrateCatalogItems.commandId
```

The exact hydration-result event contract is **not defined in this Part 3**.

However, the Roadmap already requires correlated batch hydration result events with duplicate protection and partial-failure reporting, so that separate result contract remains required before Milestone 4 can be considered complete.

---

## 7.5 Hydration Results Do Not Replace Catalog Lifecycle Events

A future hydration-result contract and the normal Catalog projection contracts have different purposes.

Conceptually:

```text
Hydration result
        |
        v
"How did this requested batch workflow finish?"


CatalogItemCreated / Updated
        |
        v
"What canonical Catalog facts now exist?"
```

The import workflow must not use a hydration result as a private replacement for normal Catalog → Tracking projection synchronization.

When hydration creates or changes canonical Catalog state, Catalog publishes the appropriate normal lifecycle Integration Events through its Outbox.

Tracking consumes them through its ordinary Inbox/projection flow.

This preserves one canonical synchronization path.

---

## 7.6 `UserLibraryImportCompleted.v1`

### Category

```text
Integration Event
```

### Producer

```text
Tracking
```

### Meaning

`UserLibraryImportCompleted.v1` means:

> **The Tracking-owned import job has been durably finalized as Completed after all expected approved commit batches required by that job have completed.**

This event is not produced:

- When Upload finishes.
- When parsing finishes.
- When Catalog matching finishes.
- When Preview becomes available.
- When the user presses Confirm.
- After an arbitrary individual batch.
- Once per imported row.

It is created only by successful durable import finalization.

---

## 7.7 Import Completion Aggregate

For `UserLibraryImportCompleted.v1`:

```text
envelope.aggregateId
=
ImportJobId
```

Because Section 2 makes `aggregateVersion` universal for Integration Events, the event uses the monotonic Tracking-owned import-job/workflow version associated with the durable finalization state.

Conceptually:

```text
aggregateVersion
=
finalized import-job state version
```

The payload does not duplicate `importJobId`.

---

## 7.8 `UserLibraryImportCompleted.v1` Conceptual Payload

The conceptual v1 summary payload is:

```json
{
  "userId": "01JUSER...",
  "sourceType": "myAnimeList",
  "processedEntryCount": 4000,
  "committedEntryCount": 3984,
  "errorCount": 0
}
```

### `userId`

Identifies the Shiori user whose Tracking library import was finalized.

This is the stable Shiori-owned user identifier.

### `sourceType`

Identifies the import source type recorded by the durable Tracking import job.

### `processedEntryCount`

Carries the terminal processing count recorded by the import job at finalization.

### `committedEntryCount`

Carries the number of import entries durably applied through the approved bounded commit process.

### `errorCount`

Carries the terminal error count recorded by the import workflow.

The event does not independently redefine what combination of counts qualifies the job for the `Completed` state.

That state transition remains owned by Tracking's import workflow.

The event reports the durable summary that existed when Tracking successfully finalized the job.

---

## 7.9 Exactly One Completion Fact

The event cardinality rule is:

```text
One durably Completed import job
        |
        v
One UserLibraryImportCompleted integration fact
```

Not:

```text
4000 imported rows
        |
        v
4000 completion events
```

The Outbox record is written by the same short finalization transaction that verifies the expected batches and marks the job `Completed`.

The actual RabbitMQ publish happens later through the Tracking Outbox publisher.

---

## 7.10 Completion Event Does Not Replace Durable Job State

`UserLibraryImportCompleted.v1` is an integration fact for other Consumers.

The authoritative import workflow state remains Tracking-owned durable job state.

If RabbitMQ publication is temporarily delayed:

```text
Tracking job = Completed
Outbox record = durable
RabbitMQ publish = pending
```

the import remains completed.

The event is eventually published from the Outbox.

RabbitMQ is not the source of truth for import completion.

---

## 7.11 Architectural Review — `ProgressUpdated.v1`

### Decision

```text
ProgressUpdated.v1
STATUS: NOT PUBLISHED FOR MVP
```

No `ProgressUpdated.v1` RabbitMQ contract is defined at this time.

Normal Tracking progress mutations therefore do **not** create an Integration Event merely because progress changed.

The accepted Tracking transaction remains capable of writing an Outbox fact **when a real external integration fact is required**, but ordinary progress updates do not receive a speculative message contract by default.

---

## 7.12 Why `ProgressUpdated.v1` Is Not Published

Tracking already preserves the information required by the current MVP inside its own boundary:

- Current progress.
- Revision/concurrency state.
- Required immutable progress history.
- Progress Vault foundation.
- Tracking-owned statistics and library behavior.

There is currently no approved MVP bounded context that needs every progress mutation through RabbitMQ.

Publishing a generic event now would therefore create:

```text
Producer contract
+
Outbox volume
+
RabbitMQ traffic
+
consumer compatibility obligations
+
retention/operations pressure
```

without a current cross-service requirement.

That would contradict the Product Horizon rule against pre-building speculative infrastructure solely because a future capability might use it.

The System Design also explicitly states that not every progress mutation necessarily requires an Integration Event.

---

## 7.13 Earlier ADR Mention Is Narrowed by This Review

Earlier architecture text listed:

```text
ProgressUpdated
```

among messages RabbitMQ could carry.

This Part 3 makes the current contract status explicit:

> **That earlier mention does not approve an MVP `ProgressUpdated.v1` publication contract.**

After this Part 3 is approved, cross-document consistency review should align the earlier ADR wording so `ProgressUpdated` is not mistaken for an active required MVP event.

This does not change the accepted rule that Tracking has a Transactional Outbox for Tracking-owned Integration Events when a real cross-service requirement exists.

---

## 7.14 Future Tracking Consumers

Future capabilities may eventually require Tracking-owned business facts.

Examples could include future:

- Recommendations.
- Notifications.
- Analytical projections.

Their existence in the Product Horizon is not sufficient reason to publish generic progress events today.

When an approved future Consumer requires a Tracking fact, Shiori will ask:

```text
What semantic business fact does that Consumer actually need?
```

and then define the smallest appropriate contract.

That may be a future progress-related event, a completion event, another semantic state transition, or a different approved contract.

It will not be created merely because:

```text
"something in Tracking changed"
```

The event architecture preserves the ability to add future Consumers without manufacturing speculative MVP traffic.

---

## 7.15 Tracking Progress Publication Rule

The normative rule is:

```text
Tracking mutation
      |
      v
Does an approved external Consumer / workflow
require a defined semantic Integration Event?
      |
   +--+--+
   |     |
  YES    NO
   |     |
   v     v
Write     No integration event
Outbox    for that mutation
fact
```

Therefore the MVP normal progress path is:

```text
Update progress
      |
      +--> current Tracking state
      +--> immutable progress history
      +--> revision/idempotency state when required
      |
      X--> no generic ProgressUpdated.v1
```

---

## 7.16 Import and Tracking Event Invariants

1. Tracking owns the Smart Staging Import workflow.
2. Catalog owns external metadata hydration.
3. `HydrateCatalogItems.v1` is an Integration Command from Tracking to Catalog.
4. `commandId` identifies one logical hydration batch request.
5. Hydration command redelivery preserves the same `commandId`.
6. The command payload carries the Tracking import job identifier, source namespace, and bounded unresolved source references.
7. Source identifiers in the command are not canonical Shiori Catalog identifiers.
8. Catalog remains responsible for normalization into canonical Shiori Catalog state.
9. Hydrated state returns to Tracking through normal Catalog lifecycle Integration Events.
10. A hydration-result contract does not replace Catalog lifecycle events.
11. `UserLibraryImportCompleted.v1` is produced only after durable finalization.
12. `UserLibraryImportCompleted.v1` uses `ImportJobId` as its event aggregate identity.
13. One completed import produces one summary completion event, not one event per imported row.
14. RabbitMQ does not become the authoritative import-state store.
15. `ProgressUpdated.v1` is not published for the MVP.
16. Ordinary progress mutations do not create speculative RabbitMQ traffic.
17. Tracking retains its Outbox capability for future approved semantic Integration Events.
18. Future Consumers must justify the specific Tracking business fact they require before a new contract is introduced.

---

# 8. Contract Storage & Distribution

## 8.1 Purpose

Integration Contracts are shared **semantics**, but they are not shared production implementation assemblies.

Catalog and Tracking need to agree on contracts such as:

```text
CatalogItemUpdated v1
PublicationUnitCreated v1
HydrateCatalogItems v1
```

without creating a compile-time dependency between their implementation projects.

The governing rule is:

> **The canonical cross-service contract is the versioned JSON Schema, not a shared C# type.**

This preserves independent evolution between bounded contexts.

---

## 8.2 No `Shiori.Shared.Contracts.dll`

Shiori does **not** introduce:

```text
Shiori.Shared.Contracts.dll
```

or equivalent production projects such as:

```text
Shiori.Shared
Shiori.Common
Shiori.Core
Shiori.Contracts
```

containing shared C# Integration Event or Integration Command classes referenced by multiple business services.

Rejected:

```text
Shiori.Shared.Contracts
        |
        +---- Catalog references it
        |
        +---- Tracking references it
```

The apparent advantage would be:

```text
"Define the DTO once."
```

but the architectural cost would be:

```text
Catalog deployment contract
        |
        v
shared assembly
        |
        v
Tracking compile-time dependency
```

The shared package would gradually become a coupling point between independently deployable bounded contexts.

Small duplication of local contract classes is accepted in exchange for stronger service independence.

---

## 8.3 Canonical Contract Location

Canonical Integration Contract schemas live in a neutral repository-level directory:

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
    │   │
    │   ├── PublicationUnitCreated/
    │   │   └── v1.schema.json
    │   ├── PublicationUnitUpdated/
    │   │   └── v1.schema.json
    │   ├── PublicationUnitRetired/
    │   │   └── v1.schema.json
    │   │
    │   └── UserLibraryImportCompleted/
    │       └── v1.schema.json
    │
    └── commands/
        └── HydrateCatalogItems/
            └── v1.schema.json
```

This directory is:

```text
contract source
```

not:

```text
business-service implementation source
```

It belongs to neither Catalog nor Tracking's Domain project.

---

## 8.4 One Schema per Contract Version

Every published contract version receives its own immutable schema artifact.

For example:

```text
CatalogItemUpdated/
├── v1.schema.json
└── v2.schema.json
```

means:

```text
CatalogItemUpdated v1
CatalogItemUpdated v2
```

are two explicit compatibility boundaries.

A v2 schema does not replace the existence of the v1 schema.

Rejected:

```text
CatalogItemUpdated/
└── schema.json
```

where `schema.json` is silently rewritten every time the contract changes.

The version must remain discoverable from both:

```text
eventVersion / commandVersion
```

and the schema artifact.

---

## 8.5 Schema Identity Mirrors Message Identity

Conceptually:

```text
eventType = CatalogItemUpdated
eventVersion = 1
```

resolves to:

```text
contracts/integration/events/
CatalogItemUpdated/
v1.schema.json
```

Likewise:

```text
commandType = HydrateCatalogItems
commandVersion = 1
```

resolves to:

```text
contracts/integration/commands/
HydrateCatalogItems/
v1.schema.json
```

The schema does not infer version information from C# namespaces, class names, assemblies, queue names, or RabbitMQ topology.

---

## 8.6 What the JSON Schema Defines

Each schema defines the serialized compatibility surface of its contract.

Conceptually, that includes:

```text
Envelope shape
Required metadata
Payload shape
Property names
JSON types
Required vs optional fields
Nested structures
Nullability where applicable
Contract-specific value constraints
```

For example, `CatalogItemUpdated/v1.schema.json` validates a message conceptually shaped as:

```json
{
  "eventId": "01JEVT...",
  "eventType": "CatalogItemUpdated",
  "eventVersion": 1,
  "aggregateId": "01JCAT...",
  "aggregateVersion": 42,
  "occurredAt": "2026-08-09T19:32:15Z",
  "correlationId": "FLOW-A",
  "causationId": "CAUSE-A",
  "payload": {
    "mediaType": "manga",
    "trackingCapability": "reading",
    "releaseTracks": [],
    "isRetired": false
  }
}
```

The exact JSON Schema draft and the .NET validation library are implementation/tooling selections and are not fixed by this architecture document.

The stable architectural decision is:

> **JSON Schema is the canonical machine-readable representation of the integration contract.**

---

## 8.7 Each Service Owns Its Local C# Representation

Catalog may locally define something conceptually like:

```text
CatalogItemUpdatedV1Contract
```

inside the Catalog boundary.

Tracking may independently define:

```text
CatalogItemUpdatedV1Message
```

inside the Tracking boundary.

Those classes may have very similar shapes.

That duplication is intentional.

Conceptually:

```text
Canonical JSON Schema
        /       \
       /         \
      v           v
Catalog local   Tracking local
C# contract      C# contract
```

not:

```text
Shared C# class
      |
  +---+---+
  |       |
Catalog Tracking
```

The contract is shared.

The implementation is not.

---

## 8.8 Local Classes Are Not Canonical

Neither Producer nor Consumer local C# classes are the authoritative cross-service specification.

The authoritative specification is:

```text
type
+
version
+
canonical JSON Schema
```

Therefore an internal C# refactor does not change the Integration Contract as long as the serialized JSON remains compatible with the existing schema and semantics.

---

## 8.9 Schema Changes Are Reviewed Like Public Contract Changes

A change under:

```text
contracts/integration/
```

is not treated as an ordinary refactor.

It requires a compatibility review using the rules established in Section 5.

For example:

```text
+ optional property
```

may remain v1.

But:

```text
rename property
change type
remove required property
change semantic meaning
```

cannot silently rewrite v1.

CI must treat canonical schema changes as compatibility-sensitive changes.

---

## 8.10 Schema Distribution

In the current repository model, schemas are distributed through the source repository and CI rather than through a shared production binary.

Producer and Consumer Contract Tests both resolve the same canonical schema artifact from:

```text
contracts/integration/
```

Conceptually:

```text
               Canonical Schema
                     |
          +----------+----------+
          |                     |
          v                     v
 Producer Contract Test   Consumer Contract Test
```

A production service does not need to reference another service assembly merely to understand the contract.

The schemas may be copied into test output or otherwise made available to the relevant test projects as test/build artifacts.

This does not introduce a runtime dependency between business services.

---

## 8.11 Runtime Schema Registry Is Not Required

STEP 5 does not introduce:

```text
Schema Registry service
```

or:

```text
Contract microservice
```

into the runtime architecture.

Shiori currently does not require another production deployment unit merely to distribute JSON Schemas.

The repository-level canonical contract plus CI validation is sufficient for the accepted architecture.

If Shiori later splits into separate repositories or develops a genuine need for centralized contract artifact distribution, the JSON Schemas can be published to an artifact mechanism without changing their role as the canonical neutral contract.

That future distribution mechanism is not pre-built now.

---

## 8.12 Schemas Survive Contract Retirement

Retiring:

```text
CatalogItemUpdated v1
```

does not mean deleting:

```text
v1.schema.json
```

from contract history.

Old schemas remain useful for:

- Compatibility history.
- Debugging.
- Dead-letter investigation.
- Controlled migration.
- Historical fixtures.
- Understanding previously published messages.

A version may become:

```text
no longer actively emitted
```

without becoming:

```text
never existed
```

---

## 8.13 Contract Storage Invariants

The following rules are normative:

1. JSON Schema is the canonical machine-readable Integration Contract representation.
2. Canonical schemas live under repository-level `contracts/integration/`.
3. Events and Commands are stored separately.
4. Each contract version has its own schema file.
5. Published schema versions are never silently rewritten to incompatible semantics.
6. `Shiori.Shared.Contracts.dll` does not exist.
7. No shared production C# integration-contract assembly is introduced.
8. Each service owns its own local C# representation.
9. Similar local C# classes across services are acceptable duplication.
10. Local C# classes are not the canonical cross-service contract.
11. Producer and Consumer tests validate against the same canonical schema.
12. Contract distribution does not require runtime service-to-service assembly dependencies.
13. A runtime Schema Registry is not introduced at this stage.
14. Retired schema versions remain preserved as contract history.
15. Changing a canonical schema requires compatibility review.

---

# 9. Contract Tests — Producer & Consumer

## 9.1 Purpose

Contract Tests answer:

> **Does this implementation still understand the Integration Contract we claim it supports?**

They do not answer:

> **Can RabbitMQ successfully route this message across the real infrastructure?**

Those are separate testing responsibilities.

---

## 9.2 Producer Contract Test

A Producer Contract Test verifies:

> **The JSON this Producer emits conforms to the canonical schema for the declared type and version.**

For example:

```text
Catalog local object
        |
        v
Serialize
        |
        v
UTF-8 JSON
        |
        v
CatalogItemUpdated/v1.schema.json
        |
        v
VALID
```

Compilation of a local C# class does not prove the externally observable JSON remains compatible.

---

## 9.3 Producer Test Example

Conceptually:

```text
Given:
Catalog state requiring CatalogItemUpdated

When:
Catalog builds CatalogItemUpdated v1
and serializes it

Then:
JSON validates against
contracts/integration/events/
CatalogItemUpdated/v1.schema.json
```

The resulting JSON must contain the expected metadata and a valid v1 payload.

---

## 9.4 What the Producer Contract Suite Must Detect

A Producer Contract Test must fail if implementation accidentally changes something such as:

```text
catalogItemId
        ↓
itemId
```

or:

```text
aggregateVersion: number
        ↓
aggregateVersion: string
```

or emits incompatible semantics while still declaring:

```text
eventVersion = 1
```

The objective is to make accidental contract drift visible in CI before deployment.

---

## 9.5 Producer Does Not Validate Its Database Model

The Producer Contract Test validates:

```text
serialized Integration Contract
```

not:

```text
MongoDB document schema
```

or:

```text
EF entity
```

Conceptually:

```text
Producer persistence/domain state
        |
        v
explicit mapping
        |
        v
Integration Contract
        |
        v
JSON Schema validation
```

This preserves the separation between persistence and integration contracts.

---

## 9.6 Consumer Contract Test

A Consumer Contract Test verifies:

> **The Consumer can understand every contract version it declares support for and convert that contract into its own local behavior/model safely.**

Conceptually:

```text
Canonical contract fixture
        |
        v
Consumer deserializer
        |
        v
Consumer local contract model
        |
        v
Consumer mapping / handling boundary
```

No Producer assembly is loaded.

---

## 9.7 Canonical Valid Message Test

For every supported event or command version, the Consumer must successfully deserialize a valid canonical message.

Example:

```json
{
  "eventId": "EVENT-42",
  "eventType": "CatalogItemUpdated",
  "eventVersion": 1,
  "aggregateId": "CATALOG-A",
  "aggregateVersion": 42,
  "occurredAt": "2026-08-09T19:32:15Z",
  "correlationId": "FLOW-A",
  "causationId": "CAUSE-A",
  "payload": {
    "mediaType": "manga",
    "trackingCapability": "reading",
    "releaseTracks": [],
    "isRetired": false
  }
}
```

Tracking must prove that it can interpret this as:

```text
CatalogItemUpdated v1
```

without relying on Catalog implementation types.

---

## 9.8 Optional Field Test

If a field is optional under a supported contract version, the Consumer must handle its absence correctly.

A Consumer must not accidentally make an optional integration property mandatory merely because its local C# constructor or mapper was written too strictly.

This is essential to the additive backward-compatibility policy established in Section 5.

---

## 9.9 Unknown Additive Property Test

Consumers supporting a compatible contract version must tolerate additive properties they do not yet use.

Suppose an older Consumer knows:

```json
{
  "catalogItemId": "..."
}
```

and a compatible v1 extension later emits:

```json
{
  "catalogItemId": "...",
  "newOptionalField": "new-information"
}
```

The old Consumer must not fail solely because:

```text
newOptionalField
```

is unknown to its local C# model.

This tolerance is one of the mechanisms that makes independent deployment possible.

---

## 9.10 Unknown Value Test

Consumers must have controlled behavior when they encounter a serialized value they do not currently recognize.

This does not mean every unknown value is automatically valid.

The canonical schema remains authoritative.

The rule is:

```text
If the contract permits the value space to evolve:
    Consumer must handle an unknown value safely
    rather than crash or reinterpret it as a known value.

If the canonical schema defines a closed set:
    A value outside that set is contract-invalid
    and must fail in a controlled manner.
```

The Consumer must never silently map an unknown value to an unrelated known meaning merely to keep processing.

This section does not globally classify every Shiori enum/string as open or closed; each concrete contract/schema defines that constraint.

---

## 9.11 `eventId` Idempotency Contract Test

The Consumer must prove that:

```text
eventId
```

is preserved as the identity used for duplicate-message handling.

Conceptually:

```text
Receive:

eventId = EVENT-A

process
```

then:

```text
Receive again:

eventId = EVENT-A
```

must be interpreted as:

```text
same logical Integration Event
```

not:

```text
new business fact
```

A contract-level test should verify that the Consumer's message handling boundary carries the exact `eventId` into the Inbox/idempotency mechanism rather than generating a new identity or using `aggregateId` as a substitute.

---

## 9.12 Durable Inbox Idempotency Is Also an Integration Test Concern

A Contract Test can prove:

```text
same eventId
        ↓
Consumer recognizes same logical message identity
```

but durable correctness such as:

```text
Inbox row committed
+
projection effect committed
+
crash
+
redelivery
+
no duplicate effect
```

depends on PostgreSQL transaction behavior and RabbitMQ redelivery.

That guarantee therefore also requires Integration Tests against real infrastructure.

---

## 9.13 Aggregate Version Consumer Test

For Catalog projection Consumers, the suite must also verify:

```text
current projection = 42

incoming event aggregateVersion = 41
```

must not result in:

```text
42 -> 41
```

This is distinct from `eventId` duplication.

A different event can have:

```text
new eventId
old aggregateVersion
```

and still be stale.

---

## 9.14 Contract Test vs RabbitMQ Integration Test

### Contract Test

Tests:

```text
JSON
schema
serialization
deserialization
type/version selection
optional-field compatibility
unknown additive fields
controlled unknown values
mapping into Consumer-owned representation
message identity semantics
```

It asks:

> **Do Producer and Consumer implementations obey the same contract?**

It does not require a RabbitMQ broker.

### RabbitMQ Integration Test

Tests actual infrastructure behavior:

```text
Publisher
    |
    v
real RabbitMQ
    |
    v
Consumer
```

It verifies concerns such as:

```text
actual publish
actual consume
UTF-8 body transport
real broker connectivity
ACK behavior
redelivery behavior
consumer integration
Outbox publisher integration where applicable
```

It asks:

> **Does the real messaging infrastructure deliver and process the contract correctly?**

---

## 9.15 Neither Test Replaces the Other

A Producer can generate perfectly schema-valid JSON while RabbitMQ configuration is broken.

Likewise, RabbitMQ can transport invalid JSON.

Therefore:

```text
Contract Test
+
RabbitMQ Integration Test
```

are complementary, not interchangeable.

---

## 9.16 CI Contract Gate

The CI gate conceptually becomes:

```text
Change contract / Producer / Consumer
        |
        v
Validate canonical schemas
        |
        v
Producer Contract Tests
        |
        v
Consumer Contract Tests
        |
        v
Messaging Integration Tests
where infrastructure behavior changed
        |
        v
PASS
```

A contract-affecting change is not considered complete merely because the solution compiles.

---

## 9.17 Contract Testing Invariants

The following rules are normative:

1. Every active Integration Contract version has Producer Contract Tests.
2. Every Consumer declares and tests every contract version it supports.
3. Producer tests serialize local classes and validate the resulting JSON against the canonical JSON Schema.
4. Consumer tests deserialize canonical contract JSON using Consumer-owned local classes.
5. Consumers must handle omission of optional properties correctly.
6. Consumers must tolerate compatible unknown additive properties.
7. Unknown serialized values are handled according to the canonical schema rather than guessed.
8. Consumers must not silently map unknown values to unrelated known meanings.
9. Consumers preserve `eventId` as the duplicate-message identity.
10. Catalog projection Consumers verify aggregate-version regression protection.
11. Contract Tests do not require RabbitMQ.
12. RabbitMQ Integration Tests use real RabbitMQ.
13. Contract Tests validate the compatibility language.
14. Integration Tests validate the infrastructure behavior.
15. Neither testing category replaces the other.
16. Durable Inbox/Outbox/ACK behavior requires infrastructure Integration Tests.
17. Contract drift must fail CI before deployment.

---

# 10. Compatibility & Deployment Procedure

## 10.1 Purpose

A breaking contract change must not require:

```text
Producer and every Consumer
deploy at exactly the same second
```

The compatibility procedure therefore follows:

> **Expand Consumers first, change Producer second, contract support last.**

This is the asynchronous-contract equivalent of an expand-and-contract migration.

---

## 10.2 Starting State

Suppose production currently uses:

```text
CatalogItemUpdated v1
```

with:

```text
Catalog
   |
   | v1
   v
RabbitMQ
   |
   v
Tracking
supports v1
```

Now a real breaking change requires:

```text
CatalogItemUpdated v2
```

Under Section 5, v1 cannot be silently redefined.

Therefore both schemas exist:

```text
CatalogItemUpdated/
├── v1.schema.json
└── v2.schema.json
```

---

## 10.3 Phase 1 — Define v2 Without Modifying v1

First create:

```text
CatalogItemUpdated v2
```

as a new contract version.

The existing:

```text
CatalogItemUpdated v1
```

remains unchanged.

Conceptually:

```text
v1 = existing published semantics
v2 = new breaking semantics
```

Contract Tests for v2 are added before production switches to it.

At this stage the Producer continues emitting v1.

---

## 10.4 Phase 2 — Expand Consumers

Before the Producer emits v2, every Consumer that must continue processing the event is updated to understand:

```text
v1
+
v2
```

Conceptually:

```text
Catalog
still emits v1
       |
       v
RabbitMQ
       |
       v
Tracking
supports:
v1 + v2
```

Nothing has changed for the Producer yet.

This makes the Consumer deployment safe because existing v1 traffic continues to work.

---

## 10.5 Consumer Version Dispatch

During the migration window, the Consumer explicitly dispatches using:

```text
eventType
+
eventVersion
```

Conceptually:

```text
CatalogItemUpdated
        |
        v
 eventVersion?
    /       \
   v1       v2
   |         |
handler     handler
/mapping   /mapping
```

The Consumer does not guess the schema by examining which payload properties happen to exist.

---

## 10.6 Phase 3 — Verify Consumer Readiness

The Producer must not switch to v2 merely because the v2 Consumer code exists in source control.

The deployment must be complete enough that every production Consumer instance expected to receive the message can understand v2.

Conceptually:

```text
All required Consumers support v2?
        |
   +----+----+
   |         |
   NO       YES
   |         |
   v         v
keep v1   Producer may move
```

The exact operational evidence or deployment tooling used to prove readiness belongs to implementation/deployment policy.

The architectural requirement is:

```text
Consumer capability first.
Producer emission second.
```

---

## 10.7 Phase 4 — Producer Begins Emitting v2

After Consumers are compatible, the Producer can switch new messages from:

```text
CatalogItemUpdated v1
```

to:

```text
CatalogItemUpdated v2
```

Conceptually:

```text
Catalog
   |
   | v2
   v
RabbitMQ
   |
   v
Tracking
supports:
v1 + v2
```

Tracking intentionally continues supporting v1 because old v1 messages may still exist in:

```text
Outbox backlog
RabbitMQ queue
retry path
dead-letter/replay path
in-flight processing
```

The fact that the Producer now creates v2 does not guarantee that every previously created v1 message has disappeared.

---

## 10.8 No Mandatory Dual Publishing

The default migration procedure does **not** require the Producer to publish both:

```text
v1
+
v2
```

for every single business fact.

That could create two separate messages representing one logical fact and would require additional deduplication semantics across versions.

The normal migration is:

```text
Consumers learn v2
        ↓
Producer switches new emission to v2
        ↓
Consumers temporarily retain v1 support
```

If a future migration genuinely requires dual publishing, that behavior must be designed explicitly for that specific contract.

It is not the default compatibility strategy.

---

## 10.9 Phase 5 — Compatibility Drain Window

After Producer v2 is active:

```text
new messages
=
v2
```

but Consumers continue accepting:

```text
v1 + v2
```

until old v1 traffic can no longer reasonably appear through active delivery/recovery paths.

Conceptually:

```text
Producer:
v2 only

Consumer:
v1 + v2
      |
      v
wait for v1 delivery/recovery paths to drain
```

The exact number of hours/days and retention policies are not defined here.

The architecture only requires that v1 support is not removed while recoverable v1 messages may still legitimately be delivered.

---

## 10.10 Phase 6 — Retire v1 Consumption

Only after the compatibility drain condition is satisfied may Consumers remove active handling for:

```text
CatalogItemUpdated v1
```

The final state becomes:

```text
Catalog
emits v2
       |
       v
RabbitMQ
       |
       v
Tracking
supports v2
```

At this point v1 is retired from active production behavior.

Its schema remains preserved in contract history.

---

## 10.11 Complete Migration Timeline

```text
PHASE 0
────────
Producer: v1
Consumer: v1


PHASE 1
────────
Define v2 schema/tests
Producer: v1
Consumer: v1


PHASE 2
────────
Deploy expanded Consumer
Producer: v1
Consumer: v1 + v2


PHASE 3
────────
Verify all Consumers v2-ready
Producer: v1
Consumer: v1 + v2


PHASE 4
────────
Switch Producer
Producer: v2
Consumer: v1 + v2


PHASE 5
────────
Drain old v1 delivery/recovery paths
Producer: v2
Consumer: v1 + v2


PHASE 6
────────
Retire active v1 support
Producer: v2
Consumer: v2
```

The critical safety property is:

```text
There is never a normal phase where:

Producer emits v2
        |
        v
Consumer only understands v1
```

---

## 10.12 Unsafe Deployment Order

Rejected:

```text
1. Deploy Producer v2
2. RabbitMQ publishes v2
3. Tracking still only understands v1
4. Consumer fails
```

Also rejected:

```text
Change v1 schema incompatibly
        |
        v
Deploy Producer
        |
        v
Hope old Consumers survive
```

That is not versioning.

It is an uncoordinated breaking change.

---

## 10.13 Rollback Consideration

Consumer-first deployment also improves rollback safety.

During:

```text
Consumer supports v1 + v2
Producer still emits v1
```

rolling back the Consumer does not require the Producer to understand anything new.

Once the Producer begins emitting v2, operational rollback must not restore a Consumer version that only understands v1 while v2 messages may be present.

The invariant is:

> **Never roll a Consumer back behind the oldest contract version that may still legitimately arrive.**

---

## 10.14 Command Migration Uses the Same Procedure

The same lifecycle applies to Integration Commands.

Example:

```text
HydrateCatalogItems v1
        ↓
breaking requirement
        ↓
HydrateCatalogItems v2
```

Here Catalog is the Consumer of the command, so Catalog must first support:

```text
v1 + v2
```

before Tracking starts emitting:

```text
HydrateCatalogItems v2
```

Conceptually:

```text
PHASE 1
Catalog accepts v1

PHASE 2
Catalog accepts v1 + v2

PHASE 3
Tracking emits v2

PHASE 4
Catalog still accepts v1 + v2
while old v1 commands drain

PHASE 5
Catalog retires v1
```

The Producer/Consumer labels follow the message direction.

---

## 10.15 Schema Retirement Is Not Schema Deletion

After v1 is retired:

```text
contracts/integration/events/
CatalogItemUpdated/
v1.schema.json
```

remains preserved.

Retirement means:

```text
No new active production flow depends on v1.
```

It does not mean:

```text
Rewrite history as if v1 never existed.
```

This keeps compatibility history explicit and old messages diagnosable.

---

## 10.16 Breaking Migration Invariants

The following rules are normative:

1. Breaking changes create a new contract version.
2. Existing versions are never incompatibly rewritten.
3. Consumers gain support for the new version before Producers emit it.
4. During migration, Consumers may support multiple versions simultaneously.
5. Producers switch only after all required Consumers are v2-capable.
6. Consumers continue accepting the old version after Producer migration while old messages may still arrive.
7. Exact drain duration is determined by later retention/operational policy.
8. Removing old Consumer support is the final migration step.
9. Old JSON Schemas remain preserved after retirement.
10. Type plus version determines deserialization; Consumers do not infer versions from payload shape.
11. Mandatory dual publishing is not part of the default migration strategy.
12. Rollback must never reintroduce a Consumer incapable of processing versions that may still arrive.
13. Integration Commands follow the same Consumer-first compatibility model.
14. The procedure preserves independent deployment rather than requiring synchronized releases.
15. Contract Tests for the new version exist before production starts emitting that version.

---

# 11. STEP 5 Completion Gate

This document is considered ready to close STEP 5 when the final validation confirms that:

- Sections 1–10 contain no unresolved contract decisions that block implementation.
- `ADR.md`, `SYSTEM_DESIGN.md`, `ROADMAP.md`, and this document use consistent terminology for active MVP integration contracts.
- The earlier architectural mention of `ProgressUpdated` is aligned with Section 7: `ProgressUpdated.v1` is **not published for the MVP**.
- Canonical JSON Schemas will be created under `contracts/integration/` during implementation according to Section 8.
- Producer and Consumer Contract Tests will be enforced according to Section 9.
- Breaking contract migrations follow the Consumer-first procedure in Section 10.

After this consistency pass is approved, `EVENT_CONTRACTS.md` becomes the accepted STEP 5 architecture contract.

