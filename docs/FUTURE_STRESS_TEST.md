# Shiori — Future Stress Test

**Status:** Complete  
**Step:** 7 — Future Stress Test  
**Result:** No architecture blockers found; several extension boundaries must be preserved before the affected parts of the MVP harden.

---

## Why I did this stress test

Before freezing Shiori's architecture, I wanted to test it against features that are not part of the MVP but are plausible enough to expose bad decisions early.

The goal was not to design Phase 2 in advance. It was to answer a narrower question:

> **If Shiori grows in the directions I already know about, can the current architecture evolve mostly through additive changes, or am I creating a dead end that will require rewriting core services later?**

A future feature only matters here when postponing a decision could cause one of these problems:

- historical data would be lost and could not be reconstructed later
- a stable identifier would need to change
- one bounded context would need to take over another's data
- an API or integration contract would need to be reinterpreted
- privacy would become expensive to retrofit
- a future feature would force direct cross-service database access
- an MVP model would accidentally become too narrow to extend safely

The test deliberately avoids creating future tables, services, queues, or APIs just because a feature might exist one day.

---

# 1. What I tested

The stress test covered these future pressures:

| Area | Main question | Result |
|---|---|---|
| Rewatch / Reread | Can one user consume the same work multiple times without destroying earlier history? | Needs one semantic safeguard |
| Granular Scoring | Can future per-run or per-unit ratings coexist with the MVP overall rating? | Additive if meanings stay separate |
| Annual Wrapped / Deep Statistics | Will the MVP preserve enough history to answer future historical questions? | Needs richer history semantics |
| Full Progress Timeline | Can history later become navigable without guessing what happened? | Needs provenance/context |
| External Authentication | Can Password, Google, Apple, etc. map to one stable Shiori user? | Architecture supports it |
| Unlisted / Granular Privacy | Can privacy become more expressive without exposing Tracking data incorrectly? | Architecture supports it if visibility stays extensible |
| Push Notifications | Can a future consumer react to release facts without coupling Catalog to users? | Messaging model supports it; release semantics need care |
| Curated Franchise Guides | Can Shiori add recommended orders without corrupting provider relationship data? | Additive if provenance stays explicit |
| Ownership Tracking | Can collection ownership grow beyond a work-level boolean? | Additive if ownership remains distinct from progress |
| Extended Localization | Can UI, title, and release language evolve independently? | Additive if meanings stay separate |
| Favorites, Autocomplete, Custom Lists, Recommendations, Connections, PWA | Do these require special MVP preparation? | No |
| Aggregate Analytics / Per-Work Discussion | Is the product defined enough to design architecture now? | No; intentionally deferred |

The macro architecture survived all of these tests. The interesting findings were mostly **semantic boundaries**, not missing infrastructure.

---

# 2. Rewatch and reread

This was the highest-risk Tracking case.

The MVP currently needs one active/current Tracking representation for a user and a work:

```text
User + CatalogItem
        |
        v
TrackingItem
        |
        +-- current progress
        +-- status
        +-- dates
        +-- overall rating
        +-- history
```

A future Rewatch/Reread feature needs to support something closer to:

```text
Persistent user-to-work relationship
        |
        +-- Consumption Run 1
        +-- Consumption Run 2
        +-- Consumption Run 3
```

The important distinction is:

> **The persistent relationship with a work is not the same thing as one consumption run.**

If the MVP permanently treats a `TrackingItem` as one watch/read cycle, a second run could force Shiori to reinterpret IDs, dates, APIs, events, and historical data.

That would be a bad migration to discover after the Tracking model has years of real data.

---

## 2.1 Data preservation

Suppose a user completes a work in 2026:

```text
Started:   2026-03-01
Completed: 2026-03-20
```

Then rereads or rewatches it in 2028:

```text
Started:   2028-07-10
Completed: 2028-07-28
```

The 2028 current state may reasonably contain the latest completion date. What must not happen is for the 2026 completion to disappear because it only ever existed in one mutable column.

This is why immutable history matters.

However, history alone does not magically give Shiori perfect future run boundaries. A sequence such as:

```text
Completed -> In Progress -> Episode 1
```

could mean a new rewatch, a correction, an import, or an Undo depending on context.

So the MVP must preserve historical facts without pretending it can infer future concepts that were never explicitly recorded.

---

## 2.2 What has to remain stable

The safe direction is:

```text
TrackingItem
    = persistent user-to-work relationship

Current state
    = latest mutable Tracking state

Future Consumption Run
    = separate concept when Rewatch/Reread is actually implemented
```

The following meanings should not be collapsed:

```text
TrackingItemId != ConsumptionRunId

Current state != complete historical record

Overall work rating != future per-run rating
```

No `consumption_runs` table is needed for the MVP. The preparation is semantic, not physical.

---

## 2.3 API and event compatibility

The existing API and event-contract strategies already support additive evolution.

A future run-aware API can add new resources or fields where compatible. What it cannot do is silently change the meaning of an existing identifier.

For example, this would be a breaking semantic change even if the JSON type stayed the same:

```text
today:
trackingItemId = persistent tracked work

later:
trackingItemId = one consumption run
```

The same rule applies to integration events. A fact about the persistent Tracking relationship should not later be reinterpreted as a fact about one run.

The stress test found no need for a forced API `v2` or a new event system. It only requires stable meanings.

---

## 2.4 Granular scoring

The MVP rating is a work-level score.

Future Phase 2 scoring may want:

```text
Run 1 -> Episode 1 -> 5 stars
Run 2 -> Episode 1 -> 3 stars
```

That is a different concept.

The MVP score should remain:

> **the user's overall rating for the work**

rather than quietly becoming “the rating for whichever run is active.”

Future run/unit scoring can then be added beside it.

---

# 3. Historical integrity

Wrapped, Deep Statistics, and Full Progress Timeline all depend on a stronger idea than “we have a JSON history row.”

The current direction is good because Shiori already treats history as immutable and first-class. The remaining risk is **semantic richness**.

A historical record may need to explain more than the resulting progress value.

Depending on the mutation, future features may need to know:

- what changed
- the previous state
- the resulting state
- when Shiori recorded the change
- whether library status changed
- whether the change came from normal tracking, import, correction, or Undo
- client/device context where the product actually requires it
- a future Consumption Run association

The exact columns or JSON shape do not need to be selected during this stress test.

The important requirement is that a supported write path cannot create an incomplete history record when the product depends on that context.

---

## 3.1 Recorded activity is not proof of consumption

This distinction appears repeatedly in the future features.

If a user imports in 2028 a work they actually completed in 2022, Shiori knows that the import was recorded in 2028 and that the imported data claims a 2022 completion.

It should not automatically report:

> “You consumed this in 2028.”

Likewise, if a user jumps from Chapter 42 to Chapter 48, Shiori knows the recorded progress changed. It does not prove the exact time each chapter was read.

That matters for Wrapped, statistics, and any future activity surface.

The safe product language is based on **recorded Tracking activity**, not invented certainty about real-world behavior.

---

## 3.2 Annual Wrapped

Wrapped can remain additive if Tracking preserves enough history to distinguish normal Shiori activity from historical imports or corrections.

A future yearly summary can safely say things such as:

- Shiori recorded this completion during the year.
- This progress transition was recorded during the year.
- This state entered through an import.

It should not infer facts the system never observed.

No Analytics Service or warehouse is required to prepare for this. The important part is preserving the source history.

---

## 3.3 Deep Statistics

Current-state statistics are easy to derive later.

Historical questions are different:

```text
How did status change over the year?
How many completion transitions were recorded each month?
How did a work's progress evolve?
```

Those cannot be reconstructed from only the final row.

The immutable Tracking history is the right foundation as long as the necessary transition context survives.

Again, this does not justify building an analytics stack now.

---

## 3.4 Full Progress Timeline and Undo

Progress Vault only needs enough information to restore the most recent progress update.

A full timeline eventually needs to explain a sequence of recorded Tracking changes.

Undo should change current state without deleting the historical fact that the original update happened.

Conceptually:

```text
Update A
    -> immutable history A

Undo A
    -> current state restored
    -> history A remains
```

The exact way Undo itself appears in future history can be designed later.

---

# 4. External authentication

The authentication stress test was mostly a confirmation that the current identity boundary is correct.

The stable identity is:

```text
Shiori UserId
```

not:

```text
email
password credential
Google subject
Apple subject
```

Tracking stores the stable Shiori `UserId`, so authentication providers can change inside Identity without rewriting the library.

---

## 4.1 Example evolution

A user might start with:

```text
Shiori User U1
└── Password
```

later become:

```text
Shiori User U1
├── Password
└── Google
```

then:

```text
Shiori User U1
├── Google
└── Apple
```

Tracking still contains:

```text
user_id = U1
```

No Tracking rows need to move.

That is exactly the behavior the architecture should preserve.

---

## 4.2 What stays for later

OpenIddict gives Shiori the OAuth2/OIDC foundation, but it does not decide all account-linking behavior.

Future external authentication still needs product/security decisions such as:

- whether accounts can ever be linked automatically
- what confirmation is required
- whether the last login method can be removed
- how provider loss is recovered
- how linking conflicts are resolved
- which provider claims are trusted

Those decisions belong to the future Identity feature.

They do not require Google or Apple tables in the MVP.

---

# 5. Privacy evolution

ADR-013 gives Shiori a useful future-proof boundary:

```text
Identity
    -> profile identity and profile-level visibility

Tracking
    -> lists, progress, ratings, statistics, and Tracking-owned privacy

Profile BFF
    -> authorized read composition only
```

The most important rule is that private Tracking data is filtered **inside Tracking** before it crosses the boundary.

The BFF should not receive a full private library and then try to remove sensitive fields afterward.

---

## 5.1 Unlisted profiles

A future profile model may evolve from:

```text
Private
Public
```

to:

```text
Private
Unlisted
Public
```

That works if Shiori keeps these concepts separate:

```text
authorization
discoverability
```

An unlisted URL is not a bearer token and knowing it does not bypass Tracking privacy.

The main preparation is to avoid spreading an irreversible `isPublic` boolean across persistence, API, cache, and authorization semantics.

The future state itself does not need to be implemented now.

---

## 5.2 Granular privacy

A future user may want:

```text
Statistics:      Public
Favorites:       Public
Recent Progress: Private
List A:          Public
List B:          Private
```

The architecture supports this because privacy follows data ownership.

Tracking can return only the sections that are allowed to leave its boundary. The BFF then composes that safe representation with the Identity profile.

If Identity cannot determine whether the profile is eligible for exposure, the flow fails closed and Tracking data is not exposed.

Friends, connections, shared URLs, or list comparison must not become privacy overrides later.

---

# 6. Push notifications

RabbitMQ already gives Shiori the right topology for adding a future consumer:

```text
Catalog
    |
    v
RabbitMQ
    |
    +--> Tracking
    |
    +--> future Notification capability
```

Catalog does not need to know which consumers exist or which users follow a work.

That keeps recipient selection, preferences, device tokens, and delivery concerns out of Catalog.

---

## 6.1 The important event-semantic caveat

The stress test found one subtle issue.

Existing events such as:

```text
PublicationUnitCreated.v1
PublicationUnitUpdated.v1
```

describe Catalog lifecycle/projection facts.

They do **not** necessarily mean:

> “A new chapter or episode just became officially available for this user's selected release track.”

For example, `PublicationUnitUpdated.v1` might represent a label correction.

Treating every update as a notification trigger would create false notifications.

So the future Notification capability may need:

- an existing event with sufficient release semantics,
- a backward-compatible additive field where that is genuinely safe, or
- a new semantic event describing verified release availability.

The stress test does not choose between those options.

The rule is simply:

> **Do not reinterpret an already-published event version to mean something stronger than it originally meant.**

---

## 6.2 Ownership stays clear

Catalog owns verified release facts.

Tracking owns user-specific state such as the selected release track and Manual Track behavior.

A future notification capability may combine the facts it legitimately receives through explicit contracts.

It should not query Catalog, Tracking, or Identity operational databases directly.

No Notification Service needs to exist in the MVP.

---

# 7. Curated franchise guides

Catalog already stores structured relationships between works:

```text
prequel
sequel
adaptation
source
side story
spin-off
```

A future Shiori guide may want to present:

```text
Recommended Order
Release Order
Chronological Order
Anime-Only Order
Source-Material Order
```

These are not the same type of knowledge.

The relationship graph answers:

> **How are these works related?**

A curated guide answers:

> **In what order does Shiori recommend consuming some of them for a particular purpose?**

The relationship graph should remain intact while curated guides are added as a separate Catalog-owned representation.

---

## 7.1 Provenance

This is the important future-proofing requirement:

```text
Provider-Derived
Shiori-Derived
Shiori-Curated
```

must remain distinguishable.

If Shiori recommends a particular franchise order, it should not look as though AniList supplied that recommendation.

Likewise, a guide can change over time without rewriting the canonical relationship edges or `CatalogItemId`s.

No guide collection, editor workflow, or revision model needs to be created now.

---

# 8. Ownership tracking

A future collection feature may need to represent facts such as:

```text
owns Spanish physical volumes 1-8
owns English digital volume 3
has only read volumes 1-4
follows a different release track
```

That cannot be represented faithfully by one permanent:

```text
owns = true
```

flag.

The important domain distinction is:

```text
Progress Unit
    !=
Commercial Edition / Owned Item
```

Catalog is the natural place for future canonical edition/variant metadata if the product eventually needs it.

Tracking is the natural owner of:

> “this user owns this edition/item.”

No ownership subsystem or edition model needs to be pre-built because historical ownership backfill is not required. Users can begin recording ownership when the feature exists.

---

# 9. Extended localization

The future-safe language model keeps these meanings separate:

```text
UI Language
Preferred Title Language
Preferred Release Language
Per-Work Release Track
```

They may all contain `"es"` or `"en"` at the same time, but they represent different choices.

For example, this should be valid:

```text
UI Language:               Spanish
Preferred Title Language:  Romaji
Preferred Release Language: English
Per-Work Release Track:    Official English
```

Changing the UI to English should not silently change the release track.

The dangerous model would be one generic:

```text
language = "es"
```

whose meaning expands over time.

The exact persistence fields can be chosen when the relevant preference contracts are implemented.

---

# 10. Features that remain safe to add later

The stress test rechecked several lower-risk Horizon features after the architecture had become more detailed.

None now requires special MVP preparation.

---

## Favorites

Likely Tracking-owned user-to-work state.

Can be added with normal Tracking persistence and privacy behavior.

No favorite storage needs to exist before the feature is approved.

---

## Search autocomplete

Catalog already owns titles and search.

Autocomplete can be added as a fast bounded Catalog query without involving Identity, Tracking, RabbitMQ, or a new service.

---

## Custom lists

Tracking remains the natural owner.

The future feature can add its own tables/use cases and reuse Tracking's privacy boundary.

No custom-list tables need to be created now.

---

## Personalized recommendations

Recommendations may eventually require significant computation, but that does not make the current architecture destructive.

Future work can consume approved Tracking/Catalog data through explicit contracts or projections and decide later whether a dedicated capability is justified.

There is no reason to pre-create an ML stack, vector database, or Recommendation Service.

---

## Friends / Connections

Connections remain tracker-scoped convenience, not a social authorization bypass.

A connection does not automatically expose private Tracking data.

No Social Service, follower system, or activity feed is needed.

---

## Read-only offline PWA

The current platform-neutral APIs and stable identifiers are sufficient preparation.

A future PWA can cache recently synchronized read data on the client without creating a PWA-specific backend domain.

Offline mutation is outside the approved scope considered by this test.

---

# 11. Product questions intentionally left unresolved

Some ideas are not defined well enough to deserve architecture yet.

That is a valid outcome.

---

## Aggregate product analytics

Before building anything, the product would need to answer questions about:

- what analytics are actually useful
- which data is needed
- whether demographics are needed at all
- consent and retention
- aggregation/privacy requirements
- whether a separate analytics capability is justified

Until then, there is no reason to choose a warehouse, OLAP database, telemetry pipeline, or Analytics Service.

The only general rule that already applies is Database-per-Service: a future analytics capability does not get permission to query operational service databases directly.

---

## Per-work discussion

Discussion introduces user-generated content, moderation, abuse/reporting, and community-governance questions.

Because the product direction is not settled, the architecture should remain unsettled too.

No Discussion Service, comments database, moderation queue, or social infrastructure is justified yet.

If the feature is approved later, it will need its own product and architecture review.

---

# 12. Preconditions carried forward to the Architecture Freeze

The stress test produced a small set of semantic safeguards.

These are the things that matter; the dozens of hypothetical future tables do not.

### Tracking lifecycle and history

```text
persistent user-to-work relationship
    !=
one Consumption Run

current Tracking state
    !=
complete historical record

overall work rating
    !=
future per-run / per-unit rating
```

History must be able to preserve required mutation context instead of relying on future guesswork.

### Identity

```text
canonical Shiori User
    !=
credential
    !=
external provider identity
```

Tracking continues to reference only the stable Shiori `UserId`.

### Privacy

```text
profile authorization
    !=
discoverability

profile-level visibility
    !=
visibility of every Tracking field
```

Tracking filters its own private data before it leaves Tracking.

### Messaging

```text
publication unit exists/changed
    !=
verified user-relevant release became available
```

Published event versions keep their original semantics.

### Catalog provenance

```text
relationship graph
    !=
curated consumption guide

provider-derived
    !=
Shiori-derived
    !=
Shiori-curated
```

### Ownership

```text
progress unit
    !=
commercial edition / owned item
```

### Localization

```text
UI language
    !=
preferred title language
    !=
preferred release language
    !=
per-work release track
```

These safeguards were the actual output of STEP 7.

---

# 13. What the stress test did not justify building

Thinking about the future did **not** justify adding these to the MVP:

- Consumption Run tables
- run-specific rating tables
- Notification Service
- Recommendation Service
- Analytics Service
- Discussion Service
- Social Service
- edition/ownership subsystem
- curated-guide collections
- Google/Apple integrations
- demographic tables
- custom-list tables
- friend tables
- a PWA backend service
- Kafka
- Event Sourcing
- a graph database
- a shared operational database

If one of those becomes necessary later, it should be introduced because a real feature needs it, not because STEP 7 predicted its name.

---

# 14. Final assessment

The known product horizon does not require Shiori to replace its macro architecture.

The current structure remains viable:

```text
Clients
   |
   v
YARP
   |
   +---- Identity
   +---- Catalog
   +---- Tracking

Profile BFF
   |
   +---- Identity-first profile authorization
   +---- Tracking privacy-filtered reads

RabbitMQ
   |
   +---- asynchronous integration
   +---- future consumers can be added independently
```

The stress test found no future feature that currently forces Shiori to:

- merge Identity, Catalog, and Tracking
- create a fourth MVP business service
- replace PostgreSQL, MongoDB, RabbitMQ, or YARP
- introduce Kafka or Event Sourcing
- use a graph database
- share operational databases
- move business orchestration into the Gateway
- replace stable Shiori identifiers
- break every public client or event consumer

The result is therefore:

> **The architecture is compatible with the known product horizon, provided the semantic boundaries identified above are preserved before the affected contracts and schemas become expensive to change.**

There were **no macro-architecture blockers**.

That is enough for STEP 7 to be considered complete and for the findings to move into the later Architecture Freeze.

---

## Source documents used by the original stress test

The analysis was based on the Shiori project documents available at STEP 7:

- `FEATURES.md`
- `PRODUCT_HORIZON.md`
- `ADR.md`
- `SYSTEM_DESIGN.md`
- `API_CONVENTIONS.md`
- `EVENT_CONTRACTS.md`
- `ROADMAP.md`

No future feature in this document is automatically approved for MVP implementation just because it was used as a stress scenario.
