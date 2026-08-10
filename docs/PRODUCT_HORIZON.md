# Shiori — Product Horizon

**Status:** Draft — final product-document synchronization still pending  
**Last updated:** August 2026  
**Purpose:** Explore future product pressure without quietly expanding the MVP.

---

## Why this document exists

I wanted one place where I could think seriously about Shiori's future without turning every interesting idea into current scope.

The question behind this document is simple:

> **If Shiori gains this feature later, is there anything I need to preserve now so I don't create an avoidable migration or architecture problem?**

That is different from asking whether the feature should be implemented now.

Most future features should **not** influence the MVP at all. If a feature can be added later with a new table, endpoint, projection, consumer, or client capability, that is usually good enough.

The cases that matter here are the ones where postponing a decision could mean:

- losing historical data that cannot be reconstructed later
- locking user identity to one authentication method
- making privacy expensive to retrofit
- breaking stable API/event meanings
- forcing one service to read another service's database
- turning an MVP shortcut into a permanent domain model
- making a future feature require destructive migration

So this document is mostly about **avoiding dead ends**, not predicting the future.

---

# 1. Relationship to the rest of the project

`PRODUCT_HORIZON.md` does not replace the product or architecture documents.

### `FEATURES.md`

`FEATURES.md` remains the source of truth for approved MVP and Phase 2 scope.

A capability appearing here does not automatically become approved scope.

### `ROADMAP.md`

`ROADMAP.md` decides implementation order for approved work.

This document does not assign milestones to speculative features.

### `ADR.md`

Horizon can identify architecture pressure, but it does not silently make the final architecture decision.

For example:

> Rewatch/Reread must remain possible without destroying previous completions.

is a valid Horizon finding.

> Add a `cycle_number` column now.

would be an implementation decision and belongs somewhere else.

---

# 2. Product direction

Shiori is a tracking product first.

The core value remains:

- preserving progress accurately
- helping users understand connected works
- helping users continue where they left off
- keeping historical tracking data useful
- giving users control over their library and privacy
- allowing selected tracking information to be shared when the user wants it

That still leaves room for profiles, favorites, list comparison, and lightweight connections.

It does **not** imply that Shiori should become a general social network.

The current social boundary is intentionally narrow:

> Users may share tracking information and inspect tracking information that someone else has explicitly chosen to expose.

That can include:

- shareable profiles
- public lists
- favorites
- aggregate statistics
- recent progress when enabled
- list comparison
- lightweight mutual connections

It does not imply:

- a global activity feed
- posts
- likes on tracking activity
- direct messaging
- chat
- follower counts
- influencer mechanics
- engagement systems designed mainly to maximize time in the app

A user who never adds a friend or makes a profile public should still get the full core value of Shiori.

---

# 3. Recorded tracking is not verified consumption

Shiori usually knows what the user recorded, not exactly what happened in the real world.

If someone changes:

```text
Chapter 42 -> Chapter 48
```

Shiori knows the stored progress changed.

It does not automatically know:

- when those chapters were actually read
- whether all of them were read that day
- whether the user was correcting old data
- whether the state came from an import

That distinction matters for future features such as Wrapped, timelines, analytics, and anything that could otherwise sound more certain than the data allows.

This is also one reason I do not want streaks or similar gamification mechanics in the current product direction. They could reward people for producing updates rather than maintaining an honest record.

---

# 4. Data minimization

Shiori should not collect personal information just because it might be useful one day.

Core tracking only needs enough information to:

- create and secure an account
- identify the user inside Shiori
- operate the tracking product

If future aggregate analytics ever justify optional demographics, the data should be collected for a concrete reason and at the smallest useful granularity.

For example, country may eventually be useful.

That does not automatically justify:

- city
- precise location
- street address
- full demographic profiles

Optional demographic information should remain separate from authentication and core tracking.

Country is the only demographic field currently considered for optional public display, and even then only through an explicit user choice.

---

# 5. Future-proofing without pre-building

Thinking about a feature is not permission to create infrastructure for it.

This document should not cause Shiori to accumulate unused:

- tables
- columns
- services
- queues
- endpoints
- workers
- provider integrations
- domain entities

Preparation is justified only when waiting would create a real future cost.

Examples:

- data would be irrecoverably lost
- a later migration would reinterpret millions of rows
- the identity model would make provider linking destructive
- a published contract would later need a breaking semantic change
- privacy would have to be retrofitted after data was already exposed

If a future feature can be added normally later, I would rather wait.

That keeps the MVP understandable.

---

# 6. How I classify future work

A product feature and its architecture pressure are separate questions.

A Phase 2 feature can still require preparation now.

A Future Candidate can also require no preparation at all.

For Horizon, the useful conclusions are:

### SAFE

No special MVP preparation is needed.

The future feature can be added through normal additive work.

### PREPARE NOW

Do not implement the feature, but preserve one or more architectural properties before the affected MVP component hardens.

### NEEDS PRODUCT DECISION

The idea is not defined well enough to design responsibly.

Do not guess the infrastructure.

---

# 7. Product classifications

## 7.1 MVP baseline relevant to Horizon

These already belong to the approved MVP and matter because future features build on them:

- Shareable Profile
- List Privacy
- Work-Focused Global Search
- Core Statistics
- Progress History / Progress Vault

---

## 7.2 MVP candidates

These are **not approved MVP scope yet**.

### Favorites

Let a user mark a tracked work as personally important independently from progress, status, and rating.

**Horizon conclusion:** SAFE

Likely owner: Tracking.

No historical backfill is needed, so there is no reason to prepare storage before approval.

---

### Search Autocomplete

Fast work suggestions while typing.

**Horizon conclusion:** SAFE

Likely owner: Catalog.

Catalog already owns title variants and indexed search. This can be added later without involving Identity or Tracking.

---

### Unlisted Profile

Possible future visibility:

```text
Private
Unlisted
Public
```

Unlisted would mean accessible through the normal URL but not publicly discoverable.

**Horizon conclusion:** PREPARE NOW

The main risk is not the feature itself. It is hard-coding profile visibility everywhere as one permanent `isPublic` boolean.

The exact model does not need to be implemented now.

---

# 8. Phase 2 approved capabilities

## Franchise Autopilot

Help users decide what to continue with next when the relationship data is unambiguous enough.

**Conclusion:** SAFE

The existing Catalog relationship graph is already the right foundation.

The important rule is to avoid pretending every franchise has one universal order.

---

## Interactive Franchise Tree

A visual, explorable graph of franchise relationships.

**Conclusion:** SAFE

Catalog already stores the relationships. The future client can visualize them without changing the underlying model.

---

## Annual Wrapped

A year-in-review based on activity Shiori actually recorded during that year.

**Conclusion:** PREPARE NOW

This depends on historical facts that may not be recoverable later.

A 2028 import of a 2022 completion must not be treated the same as activity Shiori recorded during 2028.

The MVP therefore needs meaningful immutable history, not a future analytics service.

---

## Deep Statistics / Personal Analytics

Richer analysis of one user's own tracking history.

**Conclusion:** PREPARE NOW

Current state can answer some questions, but not historical ones.

The important preparation is to preserve meaningful state transitions and timestamps. The actual analytics architecture can wait.

---

## Push Notifications

Notify users about supported new releases on the release track they follow.

**Conclusion:** PREPARE NOW

RabbitMQ already gives the right integration direction. The thing to protect is event meaning.

A generic “publication unit changed” event should not later be reinterpreted as “a verified user-relevant release became available.”

The Notification capability itself does not need to exist now.

---

## Full Progress Timeline

Expose the historical sequence currently hidden behind the MVP Progress Vault foundation.

**Conclusion:** PREPARE NOW

A future timeline may need more context than a generic JSON snapshot:

- previous state
- resulting state
- timestamp
- status transition
- source/origin
- client/device context where actually required
- future Consumption Run association

The exact physical history model belongs to Tracking design.

---

## Granular Scoring

Per-episode or per-chapter ratings in addition to the overall work rating.

**Conclusion:** PREPARE NOW

The main dependency is future Consumption Run identity.

A second watch may rate the same episode differently, so overall work rating and per-run/per-unit rating must remain different concepts.

---

## Custom Lists

Freeform user lists.

**Conclusion:** SAFE

Tracking can add them later with normal additive persistence.

No custom-list tables should be pre-created.

---

## Rewatch & Reread Tracking

Allow repeated consumption without overwriting previous completions.

**Conclusion:** PREPARE NOW

This is one of the highest-risk future cases.

The MVP must not permanently assume:

```text
library relationship
=
one consumption run
```

A future run model can be designed later, but the persistent user-to-work relationship must remain separable from it.

---

## Personalized Recommendations

Recommendations based on the user's own library, history, ratings, favorites, and Catalog metadata.

**Conclusion:** SAFE

The algorithm and infrastructure can be chosen later.

No Recommendation Service, vector database, or ML stack is justified now.

---

## List Comparison

Compare only tracking information both users are allowed to expose.

**Conclusion:** SAFE

The current privacy model already provides the right direction.

Comparison must not create new permission.

---

## Friends / Connections

Lightweight mutual relationships whose purpose is making another permitted tracking profile easier to reach.

**Conclusion:** SAFE

This does not imply followers, feeds, posts, likes, or messaging.

No social subsystem needs to exist in the MVP.

---

## Installable PWA with read-only offline mode

Offline access to recently synchronized profile, library, and statistics data.

**Conclusion:** SAFE

This is mainly a client capability.

The existing platform-neutral APIs and sync-friendly direction are enough preparation.

Offline mutation is not part of the approved scope considered here.

---

## Home Screen Widget

Compact access to selected Tracking information or actions from supported devices.

**Conclusion:** SAFE

No special server architecture is required today.

---

## Ownership Tracking

Track what a user owns separately from what they have consumed.

**Conclusion:** PREPARE NOW

The important distinction is:

```text
progress
!=
ownership
```

and more specifically:

```text
publication/progress unit
!=
commercial edition / owned item
```

A permanent work-level `owns = true` field would be too weak if the future product wants physical/digital, language, edition, or volume-level ownership.

No edition subsystem is required now.

---

## Licensing Availability

Structured official availability by language, market, publisher/provider, and verification state.

**Conclusion:** SAFE

Catalog already has release/provenance concepts that can be extended later.

The product should avoid reducing licensing to one global boolean.

---

## Illustrator Gallery

Extended cover-art and illustrator-credit exploration.

**Conclusion:** SAFE

Catalog can add this from provider-backed metadata later.

---

## Extended Localization

More interface languages and clearer independent language preferences.

**Conclusion:** PREPARE NOW

The key rule is:

```text
UI Language
!=
Preferred Title Language
!=
Preferred Release Language
!=
Per-Work Release Track
```

Changing one should not silently change the others.

No new localization service or tables are required now.

---

## Full Cast Directory

Expand the MVP's bounded cast preview into full cast and language-specific voice credits.

**Conclusion:** SAFE

The current bounded subset already leaves room for a larger representation later.

---

# 9. Future candidates

## Curated Franchise Consumption Guides

Possible future guide types:

```text
Recommended Order
Release Order
Chronological Order
Anime-Only Order
Source-Material Order
```

**Conclusion:** PREPARE NOW

The important distinction is:

```text
Relationship Graph
!=
Curated Consumption Guide
```

and provenance must remain clear:

```text
Provider-Derived
Shiori-Derived
Shiori-Curated
```

Shiori-authored guidance must not look like provider truth.

No guide collection or editorial workflow should be built now.

---

## External Authentication Providers

Potential Google, Apple, or other standards-compatible login methods.

**Conclusion:** PREPARE NOW

Identity must preserve:

```text
Canonical Shiori User
!=
Credential
!=
External Provider Identity
```

Tracking should continue referencing only the stable Shiori `UserId`.

Google/Apple persistence and account-linking rules can wait until the feature is real.

---

## Granular Profile Privacy

Future section-level privacy such as:

```text
Show Statistics
Show Favorites
Hide Recent Progress
Show Public Lists
```

**Conclusion:** PREPARE NOW

Privacy should follow data ownership.

Identity owns profile-level policy; Tracking must enforce visibility for Tracking-owned data before that data leaves Tracking.

The MVP should not assume every public-profile field forever shares one global visibility flag.

---

## Optional Demographics for Aggregate Analytics

Potential voluntarily supplied demographic information for clearly defined aggregate analysis.

**Conclusion:** SAFE

The safest preparation today is actually **not collecting it**.

If this ever becomes real, collection purpose, consent, visibility, retention, and aggregation rules must be designed first.

---

## Aggregate Product Analytics

Cross-user aggregate product analytics.

**Conclusion:** NEEDS PRODUCT DECISION

The product questions are too vague to justify architecture today.

Do not pre-create:

- an Analytics Service
- a warehouse
- OLAP storage
- extra telemetry
- demographic tables
- analytical event streams

The only general rule preserved now is Database-per-Service: a future analytics capability should not directly query operational databases.

---

# 10. Needs Product Review

## Per-Work Discussion

This used to appear in Phase 2 ideas, but it no longer fits cleanly with the clarified Tracker-First direction.

A discussion feature introduces:

- user-generated content
- moderation
- abuse/reporting
- community governance
- content lifecycle
- engagement dynamics unrelated to tracking

**Conclusion:** NEEDS PRODUCT DECISION

No discussion infrastructure should be designed while the product value is still unresolved.

---

# 11. Rejected / not planned under the current direction

These ideas should not influence MVP architecture:

- Streaks
- XP
- Levels
- Invite-only registration
- Global Activity Feed
- Chat / Direct Messaging
- General-purpose Posts
- Likes on user activity
- Influencer / Follower model
- A gamification-focused Engagement subsystem

These are not claims that Shiori could never change.

They mean I do not want to spend architecture complexity preparing for them under the current product direction.

---

# 12. Architecture pressure summary

The interesting outcome of Horizon is that most future ideas do **not** need special preparation.

The features that do need attention are concentrated around a small set of domain boundaries.

---

## Tracking

The future pressure on Tracking produces these rules:

```text
Library Relationship
!=
Consumption Run

Current State
!=
Complete Historical Record

Overall Work Rating
!=
Per-Run / Per-Unit Rating

Progress
!=
Ownership

Recorded Tracking Activity
!=
Verified Real-World Consumption
```

These distinctions matter more than guessing future table names.

---

## Identity

The future pressure on Identity produces:

```text
Shiori User
!=
Credential
!=
External Identity

Profile Identity
!=
Tracking Data

Profile Discoverability
!=
Visibility of Every Tracking Field

Core Account Data
!=
Optional Demographic Data
```

---

## Catalog

The future pressure on Catalog produces:

```text
Relationship Graph
!=
Guaranteed Consumption Order

Provider-Derived Fact
!=
Shiori-Curated Knowledge

Publication Unit
!=
Commercial Edition

Official Availability
!=
one global language/region boolean
```

---

## Cross-cutting

```text
UI Language
!=
Preferred Title Language
!=
Preferred Release Language

Public Profile
!=
permission to read every piece of user data

Future Capability
!=
reason to create a service today
```

---

# 13. High-risk stress tests

The detailed Horizon review stress-tested the architecture against several concrete future scenarios.

The results are summarized here because these are the cases that actually influenced later architecture work.

---

## 13.1 Rewatch/Reread without destroying earlier history

Scenario:

1. User completes a work.
2. Years later starts it again.
3. Rates individual units differently on the second run.
4. Later starts a third run.

The architecture must still answer:

- when the first run was completed
- what progress belongs to each run
- which ratings belong to which run
- what the current relationship with the work is
- what belongs in year-based history

The test passes only if the persistent user-to-work relationship can remain separate from future run identity.

This eventually became one of the strongest inputs to the Tracking lifecycle/history ADR work.

---

## 13.2 History that can support Timeline, Wrapped, and Personal Analytics

A future year may contain:

```text
Planned
-> In Progress
-> Episode 4
-> Episode 5
-> Paused
-> In Progress
-> Episode 6
-> Completed
```

with some changes coming from ordinary tracking and others from import or correction.

The history model must preserve enough context to distinguish those facts where the product needs the distinction.

A generic snapshot is a useful foundation, but it is not automatically a complete semantic history model.

The important lesson was:

> Preserve meaningful Tracking history now; decide analytical infrastructure later.

---

## 13.3 External login providers

A user can start with a password, later link Google, remove the password, and eventually add Apple.

Tracking should still point to the same Shiori `UserId`.

No mass ownership migration should be necessary just because authentication changed.

This stress test confirmed that external-provider identifiers must remain inside Identity.

---

## 13.4 Public, Unlisted, and granular privacy

A future user may want:

```text
Profile: Unlisted
Statistics: Visible
Favorites: Visible
Recent Progress: Hidden
Public Lists: Visible
```

The architecture must not interpret:

```text
profile visible
```

as:

```text
all Tracking data visible
```

Identity handles profile-level eligibility; Tracking filters Tracking-owned data.

A known URL or a mutual connection should not become a privacy bypass.

---

## 13.5 One release event, many users

Catalog may verify a new supported release while different users follow different tracks or have notifications disabled.

Catalog must publish release facts without knowing recipients.

Tracking remains the source of truth for user release-track behavior.

A future Notification capability can consume the relevant facts asynchronously.

This test confirmed the RabbitMQ direction but also showed why event semantics matter more than simply having a broker.

---

## 13.6 Independent language preferences

A user should be able to have:

```text
UI: Spanish
Titles: Romaji
Release preference: English
```

and change only one of them.

This stress test confirmed that a generic `language` field would be a bad long-term abstraction.

---

## 13.7 Ownership while reading a different edition

A user may follow one release track, read another edition, and physically own a third.

The system must not assume:

```text
Progress Unit
=
Release Track Unit
=
Commercial Edition
=
Owned Item
```

This did not justify an MVP edition model. It only justified preserving the conceptual boundary.

---

## 13.8 Shiori-curated franchise guidance

Provider relationships and Shiori recommendations are different kinds of knowledge.

The Catalog must be able to contain both later without pretending one came from the other.

That is why provenance matters.

---

# 14. Architecture impact matrix

| Capability | Product status | Likely owner | Historical dependency | Prepare now? | Horizon conclusion |
|---|---|---|---|---|---|
| Favorites | MVP Candidate | Tracking | None | No | SAFE |
| Search Autocomplete | MVP Candidate | Catalog | None | No | SAFE |
| Unlisted Profile | MVP Candidate | Identity | None | Yes | PREPARE NOW |
| Franchise Autopilot | Phase 2 Approved | Catalog | Low | No | SAFE |
| Interactive Franchise Tree | Phase 2 Approved | Catalog | None | No | SAFE |
| Annual Wrapped | Phase 2 Approved | Tracking | High | Yes | PREPARE NOW |
| Deep Statistics | Phase 2 Approved | Tracking | High | Yes | PREPARE NOW |
| Push Notifications | Phase 2 Approved | Future capability | None | Yes | PREPARE NOW |
| Full Progress Timeline | Phase 2 Approved | Tracking | High | Yes | PREPARE NOW |
| Granular Scoring | Phase 2 Approved | Tracking | None | Yes | PREPARE NOW |
| Custom Lists | Phase 2 Approved | Tracking | None | No | SAFE |
| Rewatch / Reread | Phase 2 Approved | Tracking | High | Yes | PREPARE NOW |
| Personalized Recommendations | Phase 2 Approved | Future capability / Tracking read model | Medium | No | SAFE |
| List Comparison | Phase 2 Approved | Tracking | None | No | SAFE |
| Friends / Connections | Phase 2 Approved | Identity | None | No | SAFE |
| Read-Only Offline PWA | Phase 2 Approved | Client | None | No | SAFE |
| Home Screen Widget | Phase 2 Approved | Client | None | No | SAFE |
| Ownership Tracking | Phase 2 Approved | Tracking | None | Yes | PREPARE NOW |
| Licensing Availability | Phase 2 Approved | Catalog | Low | No | SAFE |
| Illustrator Gallery | Phase 2 Approved | Catalog | None | No | SAFE |
| Extended Localization | Phase 2 Approved | Mixed | None | Yes | PREPARE NOW |
| Full Cast Directory | Phase 2 Approved | Catalog | None | No | SAFE |
| Curated Franchise Guides | Future Candidate | Catalog | None | Yes | PREPARE NOW |
| External Authentication | Future Candidate | Identity | None | Yes | PREPARE NOW |
| Granular Profile Privacy | Future Candidate | Identity / Tracking | None | Yes | PREPARE NOW |
| Optional Demographics | Future Candidate | Identity | None | No | SAFE |
| Aggregate Product Analytics | Future Candidate | Future capability | Unknown | No | NEEDS PRODUCT DECISION |
| Per-Work Discussion | Needs Product Review | TBD | None | No | NEEDS PRODUCT DECISION |

The original Horizon analysis counted:

- **14 SAFE**
- **12 PREPARE NOW**
- **2 NEEDS PRODUCT DECISION**

That distribution is healthy. If nearly every future idea required MVP preparation, the document would be quietly expanding the MVP instead of protecting it.

---

# 15. What Horizon required from the later architecture work

The important outputs from Horizon were not new services.

They were a bounded set of questions that the later ADR/System Design/API/Event work needed to settle.

These included:

### Internal service architecture

Identity, Catalog, and Tracking needed a consistent internal structure that keeps Infrastructure from leaking into Domain/Application logic.

### Tracking lifecycle and history

The architecture needed to preserve:

```text
persistent relationship
current state
immutable history
future consumption run
```

as distinct concepts.

### Stable Identity

The user identity needed to remain independent from credentials and future external login providers.

### Public profile/privacy composition

Identity and Tracking needed explicit ownership and safe composition rules.

### Event semantics

Future consumers needed meaningful business facts rather than generic entity-change messages.

### Language preference ownership

UI, title, release, and per-work release semantics needed separate meanings.

### Catalog provenance

Provider-derived and Shiori-curated knowledge needed to remain distinguishable.

### Ownership guardrail

Progress/publication units could not become the permanent identity of a future commercial edition/owned item.

These became inputs to the architecture work that followed Product Horizon.

---

# 16. What Horizon explicitly did not require

The document did **not** justify deciding or building:

- Notification Service topology
- push provider
- device-token storage
- Recommendation Service
- recommendation algorithm
- ML stack
- vector database
- analytics warehouse
- OLAP database
- stream-processing platform
- demographic event model
- discussion architecture
- full ownership/edition schema
- curated-guide storage

Those remain future decisions.

---

# 17. Current product-document synchronization note

This source document still contains a pending synchronization item.

During Horizon review, the following classifications were established here:

```text
Friends / Connections
-> Phase 2 Approved

Installable PWA with Read-Only Offline Mode
-> Phase 2 Approved

Per-Work Discussion
-> Needs Product Review
```

If `FEATURES.md` or `ROADMAP.md` still contain older classifications, those files need to be synchronized explicitly.

I do not want this document to silently pretend the product source of truth has already changed.

Likewise, these remain **MVP candidates**, not approved MVP work:

```text
Favorites
Search Autocomplete
Unlisted Profile
```

They only enter implementation if `FEATURES.md` is updated deliberately.

---

# 18. Final Horizon conclusion

The Product Horizon did **not** reveal a need for a larger MVP architecture.

Identity, Catalog, and Tracking remain a workable macro structure for the future direction considered here.

The biggest risk was not choosing the wrong number of services.

It was accidentally collapsing concepts that look similar in Phase 1 but need to become separate later.

The most useful takeaway from this entire exercise is:

> **Do not merge concepts today that the product is likely to need separately tomorrow.**

That principle is enough to protect the important extension points without pre-building Phase 2.

Once the synchronization note above is resolved, this document can move from Draft to Approved without changing its architectural conclusions.
