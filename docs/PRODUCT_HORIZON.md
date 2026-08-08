# Shiori — Product Horizon

**Status:** Draft — Product Horizon Review in progress  
**Last updated:** August 2026  
**Scope:** Architectural evolution analysis for capabilities that are not part of the currently approved MVP, plus a small set of explicit MVP candidates that require review before implementation.

---

# 1. Purpose & Boundaries

## 1.1 Purpose

`PRODUCT_HORIZON.md` exists to protect Shiori's long-term ability to evolve **without expanding the MVP by default**.

Its purpose is not to design future features in full and not to create an implementation roadmap for Phase 2.

Its purpose is to answer a narrower architectural question:

> **If Shiori may reasonably gain this capability later, is there anything the MVP architecture must preserve, avoid, or decide now so that adding it later remains an additive evolution rather than a destructive redesign?**

Shiori is intentionally being designed before significant domain implementation begins. This gives the project an opportunity to identify future architectural pressure while changes are still inexpensive.

The document therefore evaluates future capabilities primarily for:

- Irrecoverable historical data requirements.
- Destructive or expensive migration risk.
- Service-boundary pressure.
- Data ownership.
- Cross-service communication.
- Integration-event requirements.
- External dependencies.
- Authentication and authorization implications.
- Privacy and consent implications.
- Performance and scaling pressure.
- Potential future bounded contexts.
- Backward compatibility.
- Operational complexity.

The desired outcome is not an architecture that predicts every future feature.

The desired outcome is an architecture that preserves **reasonable extension points** for known and plausible product evolution.

---

## 1.2 Relationship to the Official Product Documents

Shiori maintains a strict separation between product scope, future exploration, architecture, and implementation sequencing.

### `FEATURES.md`

`FEATURES.md` remains the authoritative product specification.

It defines:

- The approved MVP.
- The officially approved Phase 2 product direction.

`PRODUCT_HORIZON.md` cannot silently add a capability to the MVP or Phase 2.

If Horizon analysis concludes that an MVP Candidate should become part of the MVP, `FEATURES.md` must be changed explicitly before that capability becomes approved scope.

---

### `ROADMAP.md`

`ROADMAP.md` remains the authoritative implementation sequence for approved product scope.

Horizon does not assign milestones, deadlines, or implementation order to speculative capabilities.

A future capability only enters the development roadmap after it becomes approved product scope.

---

### `ADR.md`

`ADR.md` remains the authoritative record of accepted architectural decisions.

Horizon may identify that a future capability requires architectural preparation, but Horizon itself does not silently make that architecture decision.

For example, Horizon may conclude:

> Rewatch and reread support must remain possible without destructive migration.

It should not automatically conclude:

> Add a `cycle_number` column to `tracking_entries`.

The first statement is an architectural requirement.

The second is one possible implementation decision and belongs in an ADR or later detailed design review.

When Horizon identifies a decision that must be resolved before implementation begins, that decision becomes an input to the architecture review that follows this document.

---

## 1.3 Tracker-First Product Boundary

Shiori is a **tracking platform first**.

Its core value remains:

- Preserve a user's progress accurately.
- Preserve historical consumption data.
- Help users understand connected works.
- Help users determine what they may want to continue next.
- Give users control over their library and data.
- Allow users to share selected parts of their tracking when they choose.

Future capabilities may enrich these goals, but they must not replace them.

The existence of profiles or user-to-user connections does not change Shiori into a social network.

The intended social boundary is:

> **Users may share tracking data and may inspect tracking data that another user has explicitly chosen to expose.**

This may eventually include:

- Public or shareable profiles.
- Public lists.
- Favorites.
- Aggregate statistics.
- Recent progress when explicitly enabled.
- Library comparison.
- Lightweight friend or connection relationships used to reach another person's profile more easily.

It does not imply:

- A global activity feed.
- An algorithmic social feed.
- Posts.
- Likes on user activity.
- Direct messaging.
- Chat.
- Influencer mechanics.
- Public follower counts.
- Engagement systems designed primarily to maximize time spent in the product.

A user with no friends, no public profile, and no participation in any future social capability must still receive the complete core value of Shiori.

---

## 1.4 Shared Tracking Is Not Proof of Consumption

Shiori records what users report as their progress.

It does not directly observe most consumption.

For example, if a user changes:

`Chapter 42 → Chapter 48`

Shiori knows that the recorded progress changed.

It does not necessarily know:

- When those chapters were actually read.
- Whether all six chapters were consumed on the day of the update.
- Whether the user is correcting old data.
- Whether the progress came from another tracker or historical import.

This distinction is part of the product model.

Future capabilities must not represent recorded tracking activity as verified real-world consumption unless Shiori has a reliable source that supports that claim.

This principle is one reason gamification mechanics such as streaks are not part of the current product direction.

---

## 1.5 Data-Minimization Boundary

Shiori should not collect personal information merely because it may be useful someday.

Core tracking must require only the information necessary to:

- Create and secure an account.
- Identify the user within Shiori.
- Operate the tracking product.

Future demographic information, if introduced, must remain separate from core account requirements.

The general rule is:

> **Collect the minimum granularity required to answer a defined product or analytical question.**

For example, aggregate regional analytics may justify optionally collecting a country.

They do not automatically justify collecting:

- City.
- Precise location.
- Street address.
- Full demographic profiles.

Optional demographic information must never become a prerequisite for using Shiori's core tracking functionality unless a future legal or product requirement explicitly changes that decision.

Providing demographic information for aggregate analytics also does not imply permission to display it publicly.

The only currently anticipated exception is **country**, which may eventually be displayed on a user's public profile if that user explicitly chooses to expose it.

All other contemplated demographic data remains non-public under the current product direction.

---

## 1.6 Horizon Does Not Mean Pre-Building the Future

A capability being listed in this document does not justify creating speculative infrastructure.

Horizon must not produce unused:

- Database tables.
- Columns.
- Microservices.
- Queues.
- APIs.
- Background workers.
- Provider integrations.
- Domain entities.

solely because a future capability might need them.

Preparation is justified only when postponing a decision would create a meaningful future cost.

Examples include:

- Data that cannot be reconstructed later.
- A destructive schema migration.
- An identity model that prevents future account linking.
- An event contract that prevents future consumers from receiving required information.
- A service boundary that would need to be broken.
- A security or privacy model that would be expensive to retrofit.
- A public contract that would later require a breaking change.

If a future feature can be added cleanly through new tables, new consumers, new endpoints, new projections, or another additive mechanism, no speculative implementation is required.

---

## 1.7 Architecture Evolution Principle

Shiori does not require a zero-change future.

That is neither realistic nor desirable.

The architectural goal is instead:

> **Normal product growth should result primarily in additive change rather than destructive change.**

Preferred evolution:

```text
Existing service
    +
new table / collection
    +
new vertical slice
    +
new event consumer
    +
new endpoint
```

Acceptable evolution may also include a new bounded context or deployable service when a real product capability justifies one.

The architecture should avoid situations where a normal future feature requires:

```text
rewriting a core service
+
changing stable Shiori identifiers
+
reinterpreting historical data
+
breaking public APIs
+
breaking integration events
+
migrating large amounts of live data
```

Some migrations will always be unavoidable.

Horizon exists to identify the avoidable ones.

---

## 1.8 Future Services Are Not Pre-Approved

A future capability may eventually justify a separate service.

Examples could include:

- Notifications.
- Recommendations.
- Analytics.

Mentioning such a possibility does not approve that service today.

The current accepted business boundaries remain:

- Identity.
- Catalog.
- Tracking.

A new service requires a real product requirement and an explicit architectural decision.

Shiori will not create empty microservices merely to reserve names for hypothetical future functionality.

---

## 1.9 Horizon Review Is Evidence, Not Commitment

Capabilities in this document may change status over time.

A capability may move from:

```text
Future Candidate
        ↓
Phase 2 Approved
        ↓
Roadmap
        ↓
Implementation
```

It may also move from:

```text
Future Candidate
        ↓
Needs Product Review
        ↓
Rejected / Not Planned
```

For that reason, rejected capabilities are not described as permanently impossible.

They are:

> **Rejected / Not Planned Under Current Product Direction**

Reconsidering one requires an explicit product decision and, when relevant, a new architecture-impact review.

---

# 2. Evaluation Rules

## 2.1 Every Capability Is Evaluated Against the Same Questions

Each Horizon capability will eventually receive a dedicated Feature Fiche.

The analysis must answer the same core questions so that architectural risk is evaluated consistently instead of intuitively.

Each capability is evaluated for:

1. Product purpose.
2. Expected user capability.
3. Likely business owner.
4. Secondary affected services.
5. Required future data.
6. Dependency on historical data.
7. Ability to reconstruct or backfill that data later.
8. Cross-service communication.
9. Event and integration requirements.
10. External dependencies.
11. Authentication and authorization impact.
12. Privacy and security impact.
13. Performance and scaling impact.
14. Architectural risk.
15. Migration risk if ignored.
16. Whether preparation is required before MVP implementation.
17. Whether an ADR or explicit design decision is required.
18. Open product or architecture questions.

---

## 2.2 Product Status and Architecture Preparation Are Independent

A capability's product phase does not determine whether it requires architectural preparation.

For example:

```text
Product Status:
Phase 2

Implement Now:
NO

Prepare Now:
YES
```

is valid.

Likewise:

```text
Product Status:
Future Candidate

Implement Now:
NO

Prepare Now:
NO
```

is valid.

This distinction is central to the document.

`Prepare Now: YES` does **not** mean:

> Implement part of the feature now.

It means:

> Preserve or decide something now because failing to do so could create disproportionate future cost.

---

## 2.3 Criteria for `Prepare Now: YES`

A future capability should be marked `Prepare Now: YES` only when postponing architectural consideration creates a credible risk of one or more of the following:

### Irrecoverable Data Loss

The future feature requires historical information that Shiori will not be able to reconstruct later.

Example pattern:

```text
MVP stores only current state
        ↓
future feature needs historical state
        ↓
history cannot be recreated
```

---

### Destructive Migration

The future capability would require major restructuring of heavily used live data if the MVP model is allowed to harden in its current shape.

---

### Identity Lock-In

The MVP identity model would prevent or substantially complicate future account-linking, external authentication, or identity evolution.

---

### Service-Boundary Violation

Supporting the future feature would require one service to take ownership of data or behavior that conceptually belongs elsewhere.

---

### Breaking Public Contract

The future feature would require breaking:

- Public APIs.
- Stable identifiers.
- Integration events.
- Client synchronization contracts.

when a reasonable preparation today could avoid that break.

---

### Security or Privacy Retrofitting

A future capability involves consent, visibility, sensitive information, or authorization rules that would be unsafe or disproportionately expensive to add after data has already been collected or exposed under a weaker model.

---

### Architectural Dead End

The current design would make the capability technically possible only through a workaround that contradicts Shiori's accepted architecture.

---

## 2.4 Criteria for `Prepare Now: NO`

A future capability should remain `Prepare Now: NO` when it can reasonably be introduced through additive design.

Examples include the future ability to add:

- A new table.
- A new MongoDB collection.
- A new read model.
- A new vertical slice.
- A new optional field where historical population is unnecessary.
- A new RabbitMQ consumer of an already sufficient event.
- A new API endpoint.
- A new client-only capability.

The fact that a feature will require engineering work later is not itself a reason to prepare for it now.

---

## 2.5 Historical Data Dependency

Every capability must explicitly state whether it depends on historical information.

Classification:

### `NONE`

The capability can operate using current state or future data collected after the feature launches.

### `LOW`

Historical information improves the feature but is not essential.

### `MEDIUM`

Some historical behavior is needed, but meaningful backfilling or approximation is possible.

### `HIGH`

The feature depends on historical facts that may not be reconstructable later.

A `HIGH` Historical Data Dependency requires explicit review of what the MVP records from the first production release.

Historical data should not be collected indiscriminately.

Only data tied to a credible future requirement should influence MVP retention.

---

## 2.6 Backfill Capability

Historical dependency and backfill capability are evaluated separately.

A Feature Fiche may classify future data as:

```text
Backfill:
FULL
PARTIAL
NONE
UNKNOWN
```

For example, a future feature based on a user's current favorite genres may be partially reconstructable from existing library data.

A feature requiring the exact date of every historical progress transition may not be reconstructable if those timestamps were never stored.

The purpose of this field is to expose cases where:

> "We can add the schema later"

is technically true but:

> "We can recover the missing data later"

is false.

---

## 2.7 Architecture Risk

Architecture Risk measures how strongly a capability pressures Shiori's current architectural boundaries.

### `LOW`

The feature fits naturally within the accepted architecture and is primarily additive.

### `MEDIUM`

The capability introduces meaningful new models, contracts, privacy rules, or integration behavior, but the existing architecture can support it without major restructuring if handled carefully.

### `HIGH`

A poor MVP decision could make the feature substantially more expensive, require major migration, create cross-service coupling, or cause loss of important historical information.

### `CRITICAL`

The current architecture fundamentally blocks the capability or would require a major redesign of core service boundaries, identifiers, consistency guarantees, or persistence models.

A `CRITICAL` rating does not automatically mean the current architecture is wrong.

It means the capability and architecture are materially incompatible and a deliberate product decision is required.

---

## 2.8 Migration Risk If Ignored

Migration Risk measures the future cost of doing nothing today.

### `LOW`

The feature can be introduced through additive migration with little or no reinterpretation of existing data.

### `MEDIUM`

Existing data may require migration or backfill, but the process is bounded and operationally manageable.

### `HIGH`

The feature could require large live-data migrations, reinterpretation of existing records, changes to stable identifiers, or difficult historical backfills.

### `CRITICAL`

Important required information would already have been lost, or adopting the feature would require a fundamental replacement of a core model.

Architecture Risk and Migration Risk are intentionally separate.

A concept may be architecturally simple while still being dangerous to postpone if it requires data that cannot be recreated.

---

## 2.9 Likely Ownership Does Not Approve a New Service

Each feature identifies a likely owner.

Possible values include:

```text
Identity
Catalog
Tracking
Client
Future Capability
TBD
```

`Future Capability` means that none of the current services should automatically absorb the responsibility.

It does not mean a new microservice has been approved.

A new deployable boundary requires a future ADR.

---

## 2.10 Cross-Service Impact Must Be Explicit

Every fiche must identify:

- Primary owner.
- Secondary affected services.
- Required synchronous communication, if any.
- Required asynchronous communication, if any.

The default architectural preference remains:

> Do not introduce synchronous cross-service dependencies into critical write paths when local ownership, projections, or asynchronous integration can solve the problem safely.

A Horizon feature that appears to require frequent synchronous service fan-out receives additional architectural scrutiny.

---

## 2.11 Events Are Evaluated Semantically, Not Designed Here

Horizon may identify that a future capability needs facts such as:

- A publication unit becoming available.
- A progress record changing.
- A work being completed.
- A profile-visibility change.

However, Horizon does not define the final:

- Event name.
- Event schema.
- Routing key.
- Exchange.
- Queue.
- Retry policy.

Those belong to later architecture and event-contract work.

A Horizon conclusion should therefore say:

> Future consumers must be able to observe this business fact.

rather than prematurely declaring an implementation contract.

---

## 2.12 Privacy Is Evaluated Separately from Visibility

The existence of data and permission to expose that data are separate decisions.

For example:

```text
Country = Ecuador
```

may eventually exist for optional aggregate analytics.

That does not automatically imply:

```text
Show country publicly = YES
```

Where relevant, future features must distinguish:

- Data collection.
- Processing purpose.
- Consent.
- Retention.
- Public visibility.
- Revocation or withdrawal.

Under the current Horizon direction, country may eventually be publicly visible **only through an explicit user choice**.

Other contemplated demographic data remains non-public.

---

## 2.13 Shared Tracking Is Opt-In

A future public or shareable surface must never expose private Tracking data merely because:

- Another user knows an identifier.
- Two users are connected.
- A profile exists.
- A list exists.
- A comparison operation is requested.

Visibility must be evaluated from the data owner's privacy configuration.

This remains true for future:

- Connections.
- List comparison.
- Recent progress.
- Favorites.
- Statistics.
- Public lists.

---

## 2.14 Social Capabilities Must Remain Tracker-Scoped

Any future user-to-user capability must answer:

> Does this help users inspect, compare, understand, or share tracking?

If the primary value instead becomes:

- Generating engagement between users.
- Publishing general-purpose content.
- Maximizing social interaction.
- Creating popularity metrics.
- Building an attention feed.

the feature no longer fits automatically within Shiori's Tracker-First direction and requires renewed product review.

---

## 2.15 Client Evolution Must Not Redesign the Domain API

Shiori may eventually serve:

- Desktop web.
- Mobile web.
- Installable PWA.
- Future native mobile clients.
- Future widgets.

The existence of another client does not justify duplicating the business API by platform.

Public APIs remain platform-neutral unless a future architecture decision explicitly identifies a client-specific boundary such as a BFF as necessary.

---

## 2.16 `ADR Required?`

Every fiche must state:

```text
ADR Required:
YES
NO
MAYBE
```

### `YES`

The Horizon analysis discovered a decision involving:

- Service ownership.
- Persistence model.
- Identity model.
- Consistency.
- Security.
- Privacy architecture.
- Integration strategy.
- Long-term compatibility.

that should be resolved before implementation reaches the affected area.

### `NO`

The current accepted architecture already supports the future capability adequately.

### `MAYBE`

The feature does not currently block MVP architecture, but future product clarification may reveal an architectural decision.

---

## 2.17 Allowed Horizon Conclusions

Every detailed fiche must end in one of three architectural conclusions.

### `SAFE`

No special MVP architectural preparation is required.

The feature can reasonably be introduced later through additive work.

---

### `PREPARE NOW`

The feature itself remains unimplemented, but one or more architectural properties must be preserved or decided before the affected MVP component is frozen.

---

### `NEEDS PRODUCT DECISION`

The feature is not defined precisely enough to make a reliable architecture decision.

No speculative infrastructure should be created until the product behavior is clarified.

---

# 3. Product Classification

## 3.1 Classification Model

Horizon uses the following product-status categories.

### `MVP APPROVED`

Already part of the official MVP in `FEATURES.md`.

These capabilities are listed in Horizon only when they establish an important baseline for future evolution.

Horizon cannot remove them from the MVP.

---

### `MVP CANDIDATE`

A small capability discovered after the current MVP specification was approved that may reasonably belong in Phase 1.

An MVP Candidate is **not approved scope**.

Promotion requires an explicit update to `FEATURES.md` and, when necessary, `ROADMAP.md`.

---

### `PHASE 2 APPROVED`

Part of Shiori's currently accepted post-MVP product direction.

These capabilities are expected after the MVP but are not scheduled by this document.

---

### `FUTURE CANDIDATE`

A capability worth preserving in the product horizon but not currently approved for Phase 2.

Its architecture may still require preparation if postponing a decision would create significant future cost.

---

### `NEEDS PRODUCT REVIEW`

A capability whose product behavior, value, or compatibility with Tracker-First is not sufficiently settled.

No implementation commitment should be inferred from its presence.

---

### `REJECTED / NOT PLANNED UNDER CURRENT PRODUCT DIRECTION`

A capability that has been evaluated and is intentionally excluded from the current Shiori direction.

The architecture should not incur speculative complexity to support it.

Reconsideration requires an explicit future product decision.

---

## 3.2 Existing MVP Baseline Relevant to Horizon

The following capabilities remain approved parts of the existing MVP and are important foundations for Horizon analysis.

### Shareable Profile

**Status:** MVP Approved

Shiori already supports the concept of a read-only profile centered on a user's tracking rather than on followers or social activity.

The profile is a presentation of tracking data the user has chosen to expose.

It is not a social feed.

---

### List Privacy

**Status:** MVP Approved

Lists are private by default and may be made public individually.

Future sharing capabilities must preserve this owner-controlled visibility model.

---

### Work-Focused Global Search

**Status:** MVP Approved

The primary global search remains focused on entertainment content rather than people.

Future friend or connection capabilities do not automatically change the global search into user discovery.

---

### Core Statistics

**Status:** MVP Approved

Shiori already includes aggregate statistics in Phase 1.

Future Deep Statistics and Annual Wrapped extend this foundation rather than redefine the existence of statistics.

---

### Progress History / Progress Vault Foundation

**Status:** MVP Approved

Shiori already preserves progress history and exposes the latest undoable change through Progress Vault.

Future Full Progress Timeline, Rewatch/Reread, Annual Wrapped, and Deep Statistics must be evaluated against the historical information preserved by this foundation.

---

## 3.3 MVP Candidates

The following capabilities are **not yet approved for the MVP**.

They remain candidates pending Horizon analysis and an explicit product-scope decision.

### Favorites

Allow a user to mark a tracked work as a personal favorite independently from:

- Library status.
- Progress.
- Overall rating.

Current classification:

```text
MVP Candidate
```

---

### Search Autocomplete

Provide fast title suggestions while the user types a work-focused search query.

The capability complements the existing MVP search rather than changing its purpose.

Current classification:

```text
MVP Candidate
```

---

### Unlisted Profile

Extend the profile-visibility model with an intermediate state conceptually similar to:

```text
Private
Unlisted
Public
```

An Unlisted profile would be accessible through a shared link but would not automatically become discoverable through future user-discovery mechanisms.

Current classification:

```text
MVP Candidate
```

The exact visibility model is not approved merely by listing this candidate.

---

## 3.4 Phase 2 Approved

The following capabilities form the currently approved post-MVP product horizon.

### Franchise Autopilot

Provide proactive "what should I continue with next?" guidance when Shiori can determine the answer with sufficient confidence from verified franchise relationships.

This remains distinct from broader curated consumption guides.

---

### Interactive Franchise Tree

Provide an explorable visual representation of franchise relationships beyond the MVP relationship list.

---

### Annual Wrapped

Provide a shareable year-in-review derived from tracking activity actually recorded by Shiori during that calendar year.

Historical data imported after the fact must not be treated as equivalent to activity that Shiori observed and recorded during the original year.

---

### Deep Statistics / Personal Analytics

Provide richer historical and per-work analytical views derived exclusively from the individual user's own tracking data, beyond MVP aggregate totals.

This capability is distinct from Aggregate Product Analytics, which concerns cross-user aggregate analysis and remains a separate Future Candidate.

---

### Push Notifications

Provide proactive notifications for newly available supported episodes and chapters on the release track selected by the user.

Phase 2 notification scope is intentionally limited to new-release notifications.

---

### Full Progress Timeline

Expose the historical progression currently preserved behind Progress Vault as a navigable user-visible timeline.

---

### Granular Scoring

Support per-episode and per-chapter scoring in addition to the existing overall work rating.

---

### Custom Lists

Allow users to create freeform lists beyond the predefined Watchlist and Read-list concepts.

---

### Rewatch & Reread Tracking

Allow repeated consumption of the same work without destroying or replacing the history of previous completions.

---

### Personalized Recommendations

Generate recommendations derived from a user's own library, completion history, and ratings.

Recommendations remain personalized tracking intelligence, not a social popularity feed.

---

### List Comparison

Allow two libraries to be compared through an explicit sharing flow.

A persistent connection is not required for this capability.

---

### Friends / Connections

Allow a user to maintain lightweight mutual connections whose purpose is to make another person's permitted tracking profile easier to access.

This capability does **not** imply:

- A home activity feed.
- Posts.
- Likes.
- Messages.
- Public follower metrics.
- An influencer model.

Connections exist around shared tracking, not around general social networking.

---

### Installable PWA with Read-Only Offline Mode

Provide an installable Progressive Web App experience for supported browsers and mobile devices.

The Phase 2 PWA should support read-only offline access to the user's most recently synchronized:

- Profile.
- Library.
- Statistics.

Offline mutation of tracking data is not part of the currently approved scope.

This remains a client capability and does not change Shiori's Tracker-First product model.

---

### Home Screen Widget

Provide native or platform-supported quick access to relevant tracking actions from a device home screen.

---

### Ownership Tracking

Allow users to distinguish:

> I have consumed this work

from:

> I physically own this edition or volume.

Ownership remains separate from progress.

---

### Licensing Availability

Provide structured information about whether supported works have verified official releases in particular languages or markets.

---

### Illustrator Gallery

Provide extended cover-art and illustrator-credit exploration, particularly for Light Novel volumes.

---

### Extended Localization

Extend the interface beyond the initial English and Spanish languages.

---

### Full Cast Directory

Expand the MVP bounded character preview into complete cast browsing and language-specific voice-acting information.

---

## 3.5 Future Candidates

The following capabilities are intentionally preserved in Horizon but are **not currently approved for Phase 2**.

### Curated Franchise Consumption Guides

Provide explicit consumption paths such as:

```text
Recommended Order
Release Order
Chronological Order
Anime-Only Order
Source-Material Order
```

where product requirements and trustworthy editorial or structured data support them.

This is distinct from Phase 2 Franchise Autopilot.

---

### External Authentication Providers

Potentially allow users to authenticate through providers such as:

- Google.
- Apple.
- Other standards-compatible identity providers.

External authentication must remain linked to a Shiori-owned user identity rather than making an external provider identifier the user's canonical Shiori identity.

---

### Granular Profile Privacy

Extend the existing shareable-profile model beyond a single coarse visibility decision.

Potential future controls may include independently choosing whether to expose:

- Aggregate statistics.
- Favorites.
- Recent progress.
- Public lists.
- Selected profile metadata.

The controls themselves are not approved yet.

The architecture should nevertheless avoid assuming that all public-profile data must share one indivisible visibility flag.

---

### Optional Demographics for Aggregate Analytics

Potentially allow users to voluntarily provide limited demographic information for future aggregate analytics.

Current Horizon principles:

- Optional.
- Never required for core tracking.
- Separate from credentials.
- Separate from the default public profile.
- Collected only for a defined purpose.
- Subject to explicit privacy and consent rules when introduced.

Potential examples may include:

- Country.
- Age range.
- Other future demographic dimensions only when a concrete analytical purpose exists.

Under the current direction:

- Country may optionally be shown on a public profile if the user explicitly enables that visibility.
- Other demographic information remains non-public.

No demographic collection is approved for the MVP merely because this capability exists in Horizon.

---

### Aggregate Product Analytics

Potentially analyze aggregate usage patterns across Shiori users for product understanding while remaining separate from personal Deep Statistics / Personal Analytics.

Potential future questions may include:

- Which formats are most commonly tracked by country or region.
- Which genres are most represented within a sufficiently large aggregate cohort.
- How aggregate format or genre interest changes over time.

This capability is intentionally separate from:

- Personal Analytics.
- Personalized Recommendations.
- Optional Demographics.

Its product requirements are not yet defined enough to approve:

- An analytics service.
- An analytical warehouse.
- Additional telemetry pipelines.
- Demographic collection.
- Cross-service analytical projections.

Current classification:

```text
Future Candidate
```

Architecture for this capability must remain deferred until concrete analytical questions, privacy requirements, aggregation rules, retention requirements, and consent requirements exist.

The current architectural requirement is limited to preserving Database-per-Service boundaries and avoiding assumptions that a future Analytics capability may directly query operational service databases.

---

## 3.6 Needs Product Review

### Per-Work Discussion

`Per-Work Discussion` previously appeared in the Phase 2 product direction.

It is now reclassified as:

```text
NEEDS PRODUCT REVIEW
```

Reason:

Shiori's clarified social philosophy is centered on **sharing tracking**, not building community discussion surfaces.

A discussion system introduces concerns such as:

- User-generated content.
- Moderation.
- Abuse handling.
- Reporting.
- Community governance.
- Content lifecycle.
- Potential engagement dynamics unrelated to personal tracking.

That does not automatically make the feature incompatible with Shiori, but its value and product fit are no longer sufficiently clear to treat it as approved Phase 2 scope.

No architecture should be created for discussions until the product decision is revisited explicitly.

---

## 3.7 Rejected / Not Planned Under Current Product Direction

The following capabilities have been evaluated and are intentionally excluded from the current Shiori direction.

They should not influence the MVP architecture unless a future explicit product decision reopens them.

### Streaks

Rejected because Shiori normally records user-reported progress rather than directly verified consumption.

A streak system could reward users for generating tracking activity rather than preserving an honest record.

---

### XP

Rejected because it would convert ordinary tracking actions into reward-producing behavior without improving Shiori's core tracking value.

---

### Levels

Rejected for the same product reason as XP.

Shiori does not currently need an artificial progression system layered on top of personal consumption tracking.

---

### Invite-Only Registration

Rejected.

Shiori should allow normal account creation without requiring an invitation from an existing user.

Artificial registration scarcity does not align with the current product direction.

---

### Global Activity Feed

Rejected.

Shiori's Home experience should remain centered on the current user:

- Continue.
- Release Intelligence.
- Discovery.
- Personal library context.

It should not become a stream of other users' activity.

---

### Chat / Direct Messaging

Rejected.

Real-time or asynchronous interpersonal messaging is outside Shiori's tracking purpose.

---

### Posts and General-Purpose Social Publishing

Rejected.

Shiori is not intended to become a general-purpose publishing platform.

---

### Likes on User Activity

Rejected.

Tracking updates are records of personal progress, not content objects designed to compete for social engagement.

---

### Influencer / Follower Model

Rejected.

Shiori does not currently intend to expose:

- Follower counts.
- Following counts.
- Popularity based on social graph size.
- Influencer-style profiles.

Future Friends / Connections are conceptually different: they exist only to make permitted tracking profiles easier to reach.

---

### Gamification-Focused Engagement Subsystem

Rejected as an architectural direction under the current product model.

Shiori should not create a dedicated subsystem whose purpose is primarily to support:

- XP.
- Levels.
- Streaks.
- Usage-manipulation mechanics.

Future engagement with Shiori should come from the usefulness of the tracking product itself rather than from artificial progression mechanics.

# 4. Architecture Horizon Fiches

## 4.0 Reading This Section

The fiches below evaluate product capabilities against the rules defined in Section 2.

They do not define final:

- Database schemas.
- API contracts.
- Integration-event schemas.
- Queue topologies.
- Deployment units.
- UI implementations.

When a fiche states `Prepare Now: YES`, it means that the MVP architecture must preserve or explicitly decide an architectural property before the affected component is frozen.

It does not mean that the future feature itself should be partially implemented.

Rejected capabilities from Section 3.7 are not repeated as full fiches because the current product direction intentionally does not spend architectural complexity preparing for them.

---

# 4.1 Favorites

### Product Classification

**Status:** MVP Candidate

### Purpose

Allow a user to express that a work is personally important or favored independently from progress, library status, and rating.

A work may therefore be:

- Planned and Favorite.
- In Progress and Favorite.
- Completed and Favorite.
- Dropped and Favorite.

Favorite status must not imply a particular rating or library state.

### Expected Capability

A user may mark or unmark a tracked catalog item as a favorite.

The favorite state may later be exposed through the user's shareable profile when privacy rules allow it.

### Likely Ownership

**Primary Owner:** Tracking  
**Secondary Affected Services:** Identity only when profile visibility is composed.

### Current Architecture

Tracking already owns:

- User library membership.
- Library status.
- Ratings.
- Progress.
- Consumption history.

Favorite state is therefore naturally related to the user's relationship with a catalog item.

### Future Data Requirements

A stable association between:

- User.
- Catalog item.
- Favorite state.

No historical favorite timeline is currently required.

### Historical Data Dependency

**NONE**

The capability can begin recording favorite state when it is introduced.

### Backfill Capability

**FULL / NOT REQUIRED**

Existing users can begin selecting favorites after the capability launches.

No historical favorite state needs to be reconstructed.

### Cross-Service Impact

Minimal.

Tracking owns the favorite state.

A public profile may consume a privacy-filtered representation of favorites, but Identity does not become the owner of favorite data.

### Event / Integration Impact

No mandatory cross-service event requirement has been identified.

Future consumers may eventually need to observe favorite changes, but Horizon does not require such an event today.

### External Dependencies

None.

### Privacy / Security Impact

**LOW**

Favorites may become part of shared tracking.

They must only appear publicly when the applicable profile/privacy configuration permits it.

### Scale / Performance Impact

**LOW**

Favorite queries are bounded by a user's own library.

### Architecture Risk

**LOW**

### Migration Risk If Ignored

**LOW**

### Prepare Now?

**NO**

### Preparation Required

None beyond preserving Tracking as the owner of user-to-work state.

Favorite must not be coupled to:

- Rating.
- Status.
- Completion.
- Recommendation score.

### Implement Now?

**NO**

MVP Candidate status does not mean approved MVP scope.

### ADR Required?

**NO**

### Future Trigger

Explicit approval to move Favorites into `FEATURES.md`.

### Open Questions

- Whether favorites appear publicly by default when a profile is public.
- Whether users may order their favorites manually.

These questions do not block architecture.

### Conclusion

**SAFE**

Favorites can be introduced additively without special MVP preparation.

---

# 4.2 Search Autocomplete

### Product Classification

**Status:** MVP Candidate

### Purpose

Reduce search friction by returning fast, compact work suggestions while the user types.

### Expected Capability

Autocomplete should search across the same title identity space used by Shiori discovery, including where available:

- Canonical title.
- Native title.
- Romaji title.
- Alternative titles.

Autocomplete responses should remain intentionally small and optimized for rapid repeated requests.

### Likely Ownership

**Primary Owner:** Catalog  
**Secondary Affected Services:** Gateway / Client.

### Current Architecture

Catalog already owns:

- Canonical titles.
- Native titles.
- Alternative titles.
- Media metadata.

The existing roadmap already requires an indexed search strategy for canonical, native, and alternative titles.

### Future Data Requirements

No new historical data.

Search indexes must be able to represent the supported title variants.

### Historical Data Dependency

**NONE**

### Backfill Capability

**FULL**

Search indexes can be rebuilt from Catalog's current canonical data.

### Cross-Service Impact

None.

Autocomplete should remain a Catalog read capability.

It must not require Tracking or Identity.

### Event / Integration Impact

None beyond normal Catalog indexing and synchronization.

### External Dependencies

AniList remains the primary upstream source for general title metadata under the existing Catalog Anti-Corruption Layer.

### Privacy / Security Impact

**NONE**

The global search remains work-focused and does not become user search.

### Scale / Performance Impact

**MEDIUM**

Autocomplete may generate a high number of short requests.

The implementation should favor:

- Small payloads.
- Indexed lookup.
- Request cancellation.
- Client debounce.
- Appropriate caching.

These are implementation concerns, not new service boundaries.

### Architecture Risk

**LOW**

### Migration Risk If Ignored

**LOW**

### Prepare Now?

**NO**

The existing Catalog search direction already provides the required architectural foundation.

### Preparation Required

None beyond ensuring the canonical Catalog model retains all supported title variants.

### Implement Now?

**NO**

Pending explicit MVP approval.

### ADR Required?

**NO**

### Future Trigger

Explicit MVP-scope approval.

### Open Questions

- Minimum query length.
- Ranking strategy.
- Maximum suggestion count.
- Typo tolerance.

These belong to search design rather than Horizon.

### Conclusion

**SAFE**

---

# 4.3 Unlisted Profile

### Product Classification

**Status:** MVP Candidate

### Purpose

Allow a user to share a profile through its normal URL without making that profile discoverable through future user-discovery mechanisms.

### Expected Capability

The conceptual visibility model may become:

```text
Private
Unlisted
Public
```

`Unlisted` means:

- The profile has a normal stable URL.
- A user may share that URL.
- No secret access token is required.
- The profile should not appear in future public user discovery.

Unlisted does not bypass per-section privacy.

### Likely Ownership

**Primary Owner:** Identity  
**Secondary Affected Services:** Tracking.

### Current Architecture

Identity owns:

- Public profile data.
- Profile visibility.

Tracking owns the library and progress information that may be exposed through the profile.

The MVP already includes a shareable read-only profile and list-level privacy.

### Future Data Requirements

A profile visibility state that is not limited to a permanent binary public/private assumption.

### Historical Data Dependency

**NONE**

### Backfill Capability

**FULL**

Existing profiles could be migrated to an explicit visibility state later.

### Cross-Service Impact

Identity determines profile-level discoverability.

Tracking must still independently enforce the visibility of tracking data exposed through the composed profile.

### Event / Integration Impact

No mandatory event is required for the candidate itself.

Any future cached public-profile projection must be able to invalidate or update when visibility changes.

### External Dependencies

None.

### Privacy / Security Impact

**HIGH**

`Unlisted` is a discoverability property, not an authorization secret.

The architecture must not treat an Unlisted URL as equivalent to possession of a secure access token.

### Scale / Performance Impact

**LOW**

### Architecture Risk

**MEDIUM**

The risk comes from hard-coding visibility as a single boolean throughout Identity, Tracking, API contracts, and authorization policies.

### Migration Risk If Ignored

**MEDIUM**

Expanding one database boolean is easy.

Expanding a boolean that has been duplicated into:

- Authorization logic.
- DTOs.
- Cache keys.
- Public APIs.
- Tracking composition logic.

is substantially more expensive.

### Prepare Now?

**YES**

### Preparation Required

Do not design public-profile authorization around the assumption that visibility will always be represented as only:

```text
is_public = true / false
```

The final enum or policy model belongs to later architecture design.

### Implement Now?

**NO**

Pending MVP approval.

### ADR Required?

**YES**

This should be resolved as part of the public-profile/privacy architecture before Identity's profile model is frozen.

### Future Trigger

MVP candidate review.

### Open Questions

- Whether `Unlisted` applies to the whole profile only or may eventually exist at list level.
- Whether a future public user directory will exist at all.

### Conclusion

**PREPARE NOW**

---

# 4.4 Franchise Autopilot

### Product Classification

**Status:** Phase 2 Approved

### Purpose

Help a user understand what comes next in a franchise without pretending that every franchise has one universally correct consumption order.

### Expected Capability

Shiori may recommend the next work only when the continuation is sufficiently unambiguous.

If multiple valid paths exist, Autopilot must not make an arbitrary choice for the user.

Examples of ambiguity include:

- Source material versus adaptation.
- Parallel adaptations.
- Side stories.
- Spin-offs.
- Alternative versions.
- Multiple sequels with different continuity.

### Likely Ownership

**Primary Owner:** Catalog  
**Secondary Affected Services:** Tracking for user-specific completion/progress context.

### Current Architecture

Catalog already owns:

- Franchise grouping.
- Catalog-item relationships.
- Relationship types such as sequel, prequel, source, adaptation, and side story.

Tracking owns the user's current library and progress.

### Future Data Requirements

Primarily existing relationship data.

Autopilot may additionally require derived confidence or eligibility information.

### Historical Data Dependency

**LOW**

The user's current tracking state is usually sufficient to determine whether a candidate continuation has already been consumed.

### Backfill Capability

**FULL**

Franchise relationships can be reprocessed from current Catalog state.

### Cross-Service Impact

User-independent continuation logic belongs with Catalog.

User-specific filtering may require composition with Tracking state.

Autopilot must not introduce Catalog calls into Tracking's critical progress write path.

### Event / Integration Impact

No mandatory new event requirement has been identified.

### External Dependencies

AniList relationship data remains the primary structured input.

### Privacy / Security Impact

**LOW**

Autopilot operates on the authenticated user's own tracking data.

### Scale / Performance Impact

**LOW / MEDIUM**

Relationship traversal should remain bounded.

### Architecture Risk

**MEDIUM**

The principal risk is treating relationship edges as a guaranteed total order when they are not.

### Migration Risk If Ignored

**LOW**

### Prepare Now?

**NO**

The existing relationship graph already preserves the required foundation.

### Preparation Required

Continue storing relationship semantics without collapsing them into a single numerical consumption order.

### Implement Now?

**NO**

### ADR Required?

**NO**

unless later Autopilot requirements introduce a new ownership or consistency model.

### Future Trigger

Phase 2 implementation planning.

### Open Questions

- Exact confidence rules for declaring a continuation unambiguous.
- How conflicting provider relationships are handled.
- Whether the user may disable Autopilot suggestions.

### Conclusion

**SAFE**

The current relationship-first Catalog model already supports conservative future Autopilot behavior.

---

# 4.5 Interactive Franchise Tree

### Product Classification

**Status:** Phase 2 Approved

### Purpose

Provide a visual, explorable representation of a franchise's works and relationships beyond the MVP relationship list.

### Expected Capability

Users may navigate a graph containing relationships such as:

- Source.
- Adaptation.
- Prequel.
- Sequel.
- Side story.
- Spin-off.
- Alternative version.

The graph is descriptive.

It does not automatically imply a recommended consumption order.

### Likely Ownership

**Primary Owner:** Catalog  
**Secondary Affected Services:** Client.

### Current Architecture

Catalog already stores franchise grouping and item-to-item relationships.

### Future Data Requirements

Existing relationship graph plus presentation metadata.

### Historical Data Dependency

**NONE**

### Backfill Capability

**FULL**

The tree can be generated from current Catalog state.

### Cross-Service Impact

None required for the core graph.

Tracking may optionally enrich nodes with personal progress, but that enrichment must remain separate from Catalog ownership.

### Event / Integration Impact

None beyond normal Catalog synchronization.

### External Dependencies

AniList relationship data.

### Privacy / Security Impact

**NONE**

unless personal Tracking overlays are shown.

### Scale / Performance Impact

**MEDIUM**

Large franchises may require:

- Bounded graph depth.
- Lazy loading.
- Compact graph DTOs.

### Architecture Risk

**LOW**

### Migration Risk If Ignored

**LOW**

### Prepare Now?

**NO**

### Preparation Required

Continue representing franchise relationships as explicit edges rather than a flattened ordered list.

### Implement Now?

**NO**

### ADR Required?

**NO**

### Future Trigger

Phase 2 implementation planning.

### Open Questions

- Maximum graph depth.
- Whether personal tracking status appears on graph nodes.
- Handling of disputed or weak franchise grouping.

### Conclusion

**SAFE**

---

# 4.6 Annual Wrapped

### Product Classification

**Status:** Phase 2 Approved

### Purpose

Provide a personalized year-in-review based on activity actually recorded inside Shiori during that year.

### Expected Capability

Wrapped may summarize a user's recorded Shiori activity for a calendar year.

It must not attempt to manufacture reliable past Wrapped reports from historical data imported after the fact.

If a user joins Shiori in 2028, Shiori does not pretend that an imported 2025 history is equivalent to activity observed by Shiori during 2025.

### Likely Ownership

**Primary Owner:** Tracking  
**Secondary Affected Services:** Catalog for descriptive metadata; Client for presentation.

### Current Architecture

Tracking already plans to store:

- Current progress.
- Progress history.
- Consumption dates.
- Ratings.
- Status.

`progress_history` is currently intended as immutable historical state.

### Future Data Requirements

Potentially:

- Recorded-at timestamp.
- Type of tracking change.
- Catalog item.
- Previous and resulting state.
- Client/source metadata where relevant.
- Sufficient distinction between user tracking activity and historical import/backfill.

The exact Wrapped metrics are not defined here.

### Historical Data Dependency

**HIGH**

A year-in-review cannot be reconstructed accurately if Shiori never preserved what happened during that year.

### Backfill Capability

**NONE / PARTIAL**

Current state can provide some totals.

It cannot reliably reconstruct the temporal sequence of activity that was never recorded.

### Cross-Service Impact

Wrapped calculations should primarily use Tracking-owned history.

Catalog metadata may enrich the final read model but should not become part of Tracking write operations.

### Event / Integration Impact

No final event contract is defined here.

The architecture must preserve enough historical facts to generate future annual summaries.

### External Dependencies

None required.

### Privacy / Security Impact

**MEDIUM**

Wrapped is personal data.

Sharing a Wrapped must be an explicit user action.

### Scale / Performance Impact

**MEDIUM**

Yearly analytics should not execute unbounded aggregate work on hot progress-write paths.

Precomputation or dedicated read models may be introduced later if necessary.

### Architecture Risk

**MEDIUM**

### Migration Risk If Ignored

**HIGH**

Missing historical facts cannot be recreated later.

### Prepare Now?

**YES**

### Preparation Required

Before Tracking history is frozen, verify that Shiori preserves enough information to distinguish meaningful tracking activity over time.

In particular, architecture review must consider:

- Event timestamp semantics.
- Status transitions.
- Progress transitions.
- Import-originated changes versus normal Shiori activity.
- Client/source metadata where required by future history.

This does not require implementing Wrapped.

### Implement Now?

**NO**

### ADR Required?

**YES**

The Tracking history/audit model should be explicitly reviewed before implementation.

### Future Trigger

Phase 2 planning after sufficient first-party Shiori activity exists.

### Open Questions

- Whether a current-year import contributes to that year's Wrapped.
- Exact Wrapped metrics.
- Whether ratings/favorites affect Wrapped.
- Retention duration required for annual reports.

### Conclusion

**PREPARE NOW**

---

# 4.7 Deep Statistics / Personal Analytics

### Product Classification

**Status:** Phase 2 Approved

### Purpose

Provide richer analysis of the individual user's own tracking history beyond MVP aggregate totals.

This capability is intentionally separate from cross-user or demographic product analytics.

### Expected Capability

Potential personal statistics may include future breakdowns by:

- Format.
- Genre.
- Time period.
- Completion behavior.
- Ratings.
- Work.
- Franchise.
- Consumption volume.

The exact metric set is intentionally not frozen here.

### Likely Ownership

**Primary Owner:** Tracking  
**Secondary Affected Services:** Catalog for metadata dimensions.

### Current Architecture

Tracking owns:

- Library state.
- Ratings.
- Progress.
- Progress history.
- Consumption dates.

Catalog owns metadata such as:

- Format.
- Genres.
- Tags.
- Franchise relationships.

### Future Data Requirements

Historical Tracking facts plus the metadata dimensions required to interpret them.

### Historical Data Dependency

**HIGH**

Many useful personal statistics depend on when state changed, not merely the latest state.

### Backfill Capability

**PARTIAL**

Current library state can reconstruct some totals.

Historical patterns cannot always be reconstructed if temporal facts are missing.

### Cross-Service Impact

Heavy analytics should not require synchronous fan-out from Tracking to Catalog for each historical record.

Future implementation may use:

- Additional local projections.
- Analytical read models.
- Precomputed summaries.

The exact mechanism is deferred.

### Event / Integration Impact

Future analytical projections may consume Tracking and Catalog business facts.

No event schema is defined here.

### External Dependencies

None.

### Privacy / Security Impact

**MEDIUM**

Personal analytics are private by default.

Individual statistics may only become publicly visible under explicit profile privacy rules.

### Scale / Performance Impact

**MEDIUM / HIGH**

Analytical queries can become expensive as history grows.

They must not degrade normal progress writes.

### Architecture Risk

**MEDIUM**

### Migration Risk If Ignored

**HIGH**

Missing historical dimensions cannot always be recovered later.

### Prepare Now?

**YES**

### Preparation Required

Audit the MVP history model before Tracking implementation to ensure important state transitions are actually retained.

Do not attempt to predict every future statistic.

Preserve reliable history rather than precomputing speculative metrics.

### Implement Now?

**NO**

### ADR Required?

**YES**

as part of the Tracking history model review.

### Future Trigger

Phase 2 Personal Analytics planning.

### Open Questions

- Which statistics are considered canonical.
- Required retention period.
- Whether derived analytics are recalculated or incrementally projected.
- Which Catalog dimensions need local analytical projection.

### Conclusion

**PREPARE NOW**

---

# 4.8 Push Notifications

### Product Classification

**Status:** Phase 2 Approved

### Purpose

Notify users when a new supported episode or chapter becomes available on the release track they follow.

Phase 2 notification scope is intentionally limited to new releases.

### Expected Capability

A user may receive a proactive notification when verified release data indicates a newly available:

- Episode.
- Chapter.

Notifications must respect the user's selected release track and notification preferences.

### Likely Ownership

**Primary Owner:** Future Notification Capability  
**Secondary Affected Services:** Catalog, Tracking, Identity / Client registration.

### Current Architecture

Catalog already publishes lifecycle information for:

- Catalog items.
- Publication units.

Tracking already owns:

- The user's library.
- Selected release track.
- Manual Track state.

RabbitMQ already provides asynchronous integration.

### Future Data Requirements

A future notification capability may need:

- User identifier.
- Catalog item.
- Selected release track.
- Notification preference.
- New publication-unit fact.
- Delivery target or subscription.
- Deduplication state.

### Historical Data Dependency

**NONE**

Notifications only need to operate from the time the capability is enabled.

### Backfill Capability

**NOT REQUIRED**

Old releases do not need retroactive notifications.

### Cross-Service Impact

Catalog must remain responsible for verified release facts.

Tracking remains responsible for which release track the user follows.

A Notification capability must not require direct reads of Catalog or Tracking databases.

### Event / Integration Impact

**SIGNIFICANT**

Future consumers must be able to observe:

- Relevant verified release changes.
- Relevant user release-track/preference changes.

Horizon does not define final event contracts.

### External Dependencies

Future push-delivery infrastructure will be required.

No provider is selected here.

### Privacy / Security Impact

**MEDIUM**

Notification content can reveal what a user follows.

Delivery tokens/subscriptions are security-sensitive data.

### Scale / Performance Impact

**MEDIUM / HIGH**

One publication event may fan out to many users.

Fan-out must happen asynchronously.

### Architecture Risk

**MEDIUM**

### Migration Risk If Ignored

**MEDIUM**

The feature is additive, but poor event semantics could force producer changes later.

### Prepare Now?

**YES**

### Preparation Required

Ensure that event-contract design later in the architecture phase does not publish only generic "item changed" signals when a semantic publication fact is required.

Tracking must remain the source of truth for selected release-track preference.

Do not implement Notification infrastructure now.

### Implement Now?

**NO**

### ADR Required?

**MAYBE**

The event architecture must support future consumers; a dedicated Notification Service decision can wait until implementation.

### Future Trigger

Phase 2 notification planning.

### Open Questions

- Per-work versus global opt-out.
- Push provider.
- Notification deduplication.
- Notification language.
- Behavior when release data is corrected or withdrawn.

### Conclusion

**PREPARE NOW**

---

# 4.9 Full Progress Timeline

### Product Classification

**Status:** Phase 2 Approved

### Purpose

Expose the user's full historical evolution for a tracked work rather than only the latest undoable update.

### Expected Capability

The timeline must be capable of representing:

- Progress changes.
- Library-status changes.
- Relevant timestamps.
- Client/device that produced the change.

Examples include:

```text
Planned → In Progress
Episode 3 → Episode 4
In Progress → Paused
Paused → In Progress
In Progress → Completed
```

The timeline is an audit of recorded Tracking state.

It is not proof of exactly when real-world consumption occurred.

### Likely Ownership

**Primary Owner:** Tracking  
**Secondary Affected Services:** None required in the critical write path.

### Current Architecture

The current ADR defines `progress_history` as immutable JSONB snapshots populated through database triggers.

The MVP Progress Vault exposes only the latest undoable update.

### Future Data Requirements

The history model must preserve sufficient information to reconstruct:

- Previous state.
- Resulting state.
- Progress family.
- Status transition.
- Timestamp.
- Client/device metadata.
- Tracking/consumption-run identity when applicable.

### Historical Data Dependency

**HIGH**

### Backfill Capability

**NONE / PARTIAL**

A state transition that was never recorded cannot later be reconstructed with confidence.

Device/client metadata is also impossible to recover after the fact if it was never captured.

### Cross-Service Impact

None should be required for writes.

Human-readable Catalog metadata may be resolved when displaying the timeline.

### Event / Integration Impact

No cross-service event is inherently required.

The internal historical recording mechanism itself requires architectural review.

### External Dependencies

None.

### Privacy / Security Impact

**MEDIUM / HIGH**

Device/client metadata can expose sensitive information.

The history must remain private unless an explicit future privacy feature exposes part of it.

### Scale / Performance Impact

**MEDIUM / HIGH**

History is append-heavy and grows continuously.

Cursor pagination and retention rules are important.

### Architecture Risk

**HIGH**

The current trigger-based snapshot concept may be insufficient by itself if application-level context such as client/device identity must also be preserved.

### Migration Risk If Ignored

**HIGH**

Missing historical context cannot be backfilled.

### Prepare Now?

**YES**

### Preparation Required

Before implementing Tracking history, explicitly decide:

- What constitutes a history event.
- Which state transitions are captured.
- How client/device identity is associated with a history record.
- How the mechanism remains impossible to bypass.
- How Progress Vault undo relates to immutable history.

Do not assume that a database trigger alone is automatically sufficient for every required future timeline field.

### Implement Now?

**NO**

Only the MVP Progress Vault remains in Phase 1.

### ADR Required?

**YES**

### Future Trigger

Tracking architecture review before Milestone 3, followed by Phase 2 UI/API work later.

### Open Questions

- Definition of a device versus a client application.
- Whether device labels are user-visible.
- History retention duration.
- Whether changes generated by imports appear in the same timeline.
- Whether release-track changes are timeline events.

### Conclusion

**PREPARE NOW**

---

# 4.10 Granular Scoring

### Product Classification

**Status:** Phase 2 Approved

### Purpose

Allow a user to rate individual episodes or chapters rather than only assigning one overall score to the work.

### Expected Capability

Granular scores may be attached to individual publication/consumption units.

When Rewatch/Reread exists, a granular score belongs to the specific **consumption run** in which the unit was consumed.

A second watch may therefore have different episode ratings from the first watch.

### Likely Ownership

**Primary Owner:** Tracking  
**Secondary Affected Services:** Catalog for stable unit identifiers.

### Current Architecture

Tracking already references projected Catalog publication units for granular progress.

The MVP includes one overall 1–5 star work rating.

### Future Data Requirements

Potentially:

- User.
- Consumption run.
- Catalog item.
- Unit identifier.
- Score.
- Timestamp.

Exact persistence is deferred.

### Historical Data Dependency

**NONE**

Past granular ratings never existed and do not need to be invented.

### Backfill Capability

**NONE / NOT REQUIRED**

### Cross-Service Impact

Tracking should use stable projected Catalog unit identifiers.

No synchronous Catalog dependency should be introduced into rating writes.

### Event / Integration Impact

No mandatory cross-service event.

### External Dependencies

None.

### Privacy / Security Impact

**LOW / MEDIUM**

Granular ratings are private unless future profile settings expose them.

### Scale / Performance Impact

**MEDIUM**

Per-unit ratings may produce substantially more records than overall work ratings.

### Architecture Risk

**MEDIUM**

The major architectural dependency is the future identity of a consumption run.

### Migration Risk If Ignored

**MEDIUM**

Granular ratings can be added later, but a poorly defined Rewatch model could make it unclear which run owns them.

### Prepare Now?

**YES**

Only through the Rewatch/Reread architecture review.

No granular-rating tables should be created now.

### Preparation Required

Ensure future consumption runs have stable identity so per-unit ratings can belong to a specific run.

Do not overload the MVP overall work rating to represent run-specific granular ratings.

### Implement Now?

**NO**

### ADR Required?

**MAYBE**

The consumption-run ADR should define enough ownership semantics; a separate scoring ADR may not be necessary.

### Future Trigger

Phase 2 Rewatch/Granular Scoring design.

### Open Questions

- Whether the existing overall work rating remains independent.
- Whether an overall rating can be derived from run ratings.
- Whether unrated units are distinguishable from explicit zero-like values.

### Conclusion

**PREPARE NOW**

---

# 4.11 Custom Lists

### Product Classification

**Status:** Phase 2 Approved

### Purpose

Allow users to organize catalog items into freeform collections beyond predefined Watchlist and Read-list concepts.

### Expected Capability

A custom list may contain:

- Name.
- Description.
- Manual item order.
- Visibility: private or public.
- Catalog items selected by the user.

### Likely Ownership

**Primary Owner:** Tracking  
**Secondary Affected Services:** Identity only for profile composition.

### Current Architecture

Tracking already owns user library state and list privacy.

### Future Data Requirements

A list entity plus membership/order information.

### Historical Data Dependency

**NONE**

### Backfill Capability

**FULL / NOT REQUIRED**

### Cross-Service Impact

Custom lists should reference Shiori Catalog identifiers already known to Tracking.

Public list presentation must respect the owning user's privacy.

### Event / Integration Impact

None required.

### External Dependencies

None.

### Privacy / Security Impact

**MEDIUM**

Private lists must never become visible through profile or comparison APIs.

### Scale / Performance Impact

**LOW / MEDIUM**

Large lists require pagination.

Manual ordering requires deterministic ordering semantics.

### Architecture Risk

**LOW**

### Migration Risk If Ignored

**LOW**

The relational model is additive.

### Prepare Now?

**NO**

### Preparation Required

Do not hard-code Watchlist/Read-list in a way that makes them the only list model the product can ever expose.

This is a design caution, not a request to implement generic custom lists now.

### Implement Now?

**NO**

### ADR Required?

**NO**

unless future list-sharing requirements become substantially more complex.

### Future Trigger

Phase 2 implementation.

### Open Questions

- Whether an item may appear multiple times in one list.
- Maximum list size.
- Whether list ordering supports sections/groups later.

### Conclusion

**SAFE**

---

# 4.12 Rewatch & Reread Tracking

### Product Classification

**Status:** Phase 2 Approved

### Purpose

Allow a user to consume the same work multiple times while preserving each historical experience independently.

### Expected Capability

A user's relationship with a work may contain multiple historical consumption runs.

Conceptually:

```text
Work
└── User Library Relationship
    ├── First Watch / Read
    ├── Second Watch / Read
    └── Third Watch / Read
```

Each run may preserve its own:

- Start date.
- Completion date.
- Progress.
- Status/history.
- Future granular scoring.

A new run must not destroy the previous one.

### Likely Ownership

**Primary Owner:** Tracking  
**Secondary Affected Services:** None required.

### Current Architecture

The current ADR states that Tracking enforces one active `tracking_entry` per user and catalog item.

Current progress and `progress_history` are centered around that tracking entry.

### Future Data Requirements

The architecture needs to distinguish conceptually between:

1. The user's persistent relationship/library entry for a catalog item.
2. A particular consumption run of that item.

The final schema is not selected in Horizon.

### Historical Data Dependency

**HIGH**

If repeat consumption occurs before the architecture can distinguish run boundaries, exact historical runs may become ambiguous.

### Backfill Capability

**PARTIAL / UNKNOWN**

Status and completion history might allow some reconstruction.

It cannot be assumed that every future run boundary can be derived safely.

### Cross-Service Impact

None.

This remains entirely within Tracking ownership.

### Event / Integration Impact

Future consumers may need to distinguish:

- Library relationship changes.
- Consumption-run changes.

No event schema is defined here.

### External Dependencies

None.

### Privacy / Security Impact

**LOW / MEDIUM**

Future shared profiles may expose repeat-consumption counts only if explicitly permitted.

### Scale / Performance Impact

**MEDIUM**

History grows with each run, but remains naturally partitioned by user and work.

### Architecture Risk

**HIGH**

The current one-entry model can become a conceptual dead end if `tracking_entry` simultaneously represents:

- Library membership.
- Current progress.
- One specific consumption run.

### Migration Risk If Ignored

**HIGH**

A mature Tracking database may require reinterpretation and restructuring of heavily used records.

### Prepare Now?

**YES**

### Preparation Required

Before Tracking schema freeze, explicitly decide how the domain distinguishes:

- Persistent user-to-work relationship.
- Active/current progress.
- Historical consumption runs.

Horizon does not mandate:

- `cycle_number`.
- A `consumption_runs` table.
- A particular primary-key structure.

Those are ADR/design decisions.

### Implement Now?

**NO**

The MVP still needs only one current consumption flow.

### ADR Required?

**YES**

### Future Trigger

Before Milestone 3 Tracking schema implementation.

### Open Questions

- Whether multiple runs may be active simultaneously.
- How a newly started run affects top-level Library Status.
- Which dates remain work-level versus run-level.
- How overall rating relates to individual runs.

### Conclusion

**PREPARE NOW**

---

# 4.13 Personalized Recommendations

### Product Classification

**Status:** Phase 2 Approved

### Purpose

Recommend works based solely on the individual user's own tracking information.

This capability must not depend on social-network similarity, follower behavior, or aggregate "users like you" profiling under the current product direction.

### Expected Capability

Recommendations may consider the user's own:

- Ratings.
- Completion history.
- Favorites.
- Formats.
- Catalog genres/tags.
- Franchise relationships.

The exact algorithm is intentionally not selected.

### Likely Ownership

**Primary Owner:** Future Recommendation Capability or Tracking read model  
**Secondary Affected Services:** Tracking, Catalog.

### Current Architecture

Tracking owns user preferences expressed through activity.

Catalog owns descriptive metadata.

### Future Data Requirements

User-specific tracking history plus relevant Catalog dimensions.

### Historical Data Dependency

**MEDIUM**

Current library and ratings provide substantial signal.

Additional history may improve quality but is not strictly required for a first recommendation system.

### Backfill Capability

**PARTIAL / FULL**

Many recommendation inputs can be reconstructed from current library state and Catalog metadata.

### Cross-Service Impact

Recommendation computation must not be placed inside normal progress writes.

A future implementation may use:

- Read models.
- Background processing.
- Local analytical projections.

No final architecture is selected here.

### Event / Integration Impact

Future recommendation projections may consume Tracking changes.

No producer-specific contract is required by Horizon beyond meaningful integration facts.

### External Dependencies

None required.

An external recommendation provider is not part of the current product direction.

### Privacy / Security Impact

**MEDIUM**

Recommendation inputs are personal tracking data.

They must not be shared across users for collaborative filtering under the current approved scope.

### Scale / Performance Impact

**MEDIUM / HIGH**

Recommendation generation may become computationally expensive.

It should be isolated from critical request/write paths.

### Architecture Risk

**MEDIUM**

### Migration Risk If Ignored

**LOW**

The capability can be introduced later with additive read infrastructure.

### Prepare Now?

**NO**

Existing history and Catalog ownership already provide a reasonable foundation.

### Preparation Required

None beyond preserving accurate user history for other already-approved reasons.

### Implement Now?

**NO**

### ADR Required?

**MAYBE**

A future dedicated Recommendation capability may require an ADR when implementation becomes real.

### Future Trigger

Phase 2 recommendation planning.

### Open Questions

- Explainability requirements.
- How favorites influence recommendations.
- How negative signals such as Dropped status are interpreted.
- Recommendation refresh frequency.

### Conclusion

**SAFE**

---

# 4.14 List Comparison

### Product Classification

**Status:** Phase 2 Approved

### Purpose

Allow users to compare tracking information that both users have explicitly chosen to expose.

### Expected Capability

A comparison may show intersections such as:

- Works both users completed.
- Works both are currently tracking.
- Shared favorites.
- Works visible only in one public library.

Comparison never creates permission to read private Tracking data.

### Likely Ownership

**Primary Owner:** Tracking  
**Secondary Affected Services:** Identity for profile identity/visibility composition.

### Current Architecture

The MVP already includes:

- Shareable profiles.
- Private-by-default lists.
- A public-library API exposing only explicitly public lists.

### Future Data Requirements

No fundamentally new historical data.

### Historical Data Dependency

**NONE**

### Backfill Capability

**FULL**

Comparison can use current visible library state.

### Cross-Service Impact

Tracking performs or serves comparison data.

Identity may provide public profile identity.

No service may bypass another service's data ownership.

### Event / Integration Impact

None required.

### External Dependencies

None.

### Privacy / Security Impact

**HIGH**

Comparison authorization is the feature's most important constraint.

The comparison result must contain only data that both users have made eligible for that comparison.

There are no "magic links" that override privacy.

### Scale / Performance Impact

**MEDIUM**

Large libraries require efficient set comparison and pagination/bounded result design.

### Architecture Risk

**LOW**

The existing public-library boundary already supports the intended model.

### Migration Risk If Ignored

**LOW**

### Prepare Now?

**NO**

### Preparation Required

Continue enforcing privacy at the data owner/service boundary.

### Implement Now?

**NO**

### ADR Required?

**NO**

unless a future comparison design introduces new authorization semantics.

### Future Trigger

Phase 2 implementation.

### Open Questions

- Whether both profiles must be public or only compared lists must be public.
- Maximum comparison size.
- Whether comparison results can themselves be shared.

### Conclusion

**SAFE**

---

# 4.15 Friends / Connections

### Product Classification

**Status:** Phase 2 Approved

### Purpose

Allow users to maintain a lightweight, mutual connection so another person's permitted tracking profile is easier to find again.

Connections exist around shared tracking.

They do not create a social feed.

### Expected Capability

A connection requires:

1. Request.
2. Acceptance.

The relationship is mutual.

The capability does not introduce:

- Followers.
- Following counts.
- Posts.
- Likes.
- Messaging.
- Global activity.
- Algorithmic feeds.

### Likely Ownership

**Primary Owner:** Identity  
**Secondary Affected Services:** Tracking for the tracking data shown on profiles.

### Current Architecture

Identity already owns public user profiles.

Tracking already owns public/private tracking data.

### Future Data Requirements

A mutual relationship/request lifecycle associated with Shiori user identifiers.

### Historical Data Dependency

**NONE**

### Backfill Capability

**NOT REQUIRED**

### Cross-Service Impact

Identity may answer whether two users are connected.

Tracking still evaluates its own visibility rules.

A connection must not grant direct database access or bypass Tracking authorization.

### Event / Integration Impact

No mandatory event requirement for the core relationship.

### External Dependencies

None.

### Privacy / Security Impact

**HIGH**

Connections require consent from both users.

Connection state must not silently expose data that the tracking owner has kept private.

### Scale / Performance Impact

**LOW / MEDIUM**

The product intentionally avoids global social-graph ranking and feed queries.

### Architecture Risk

**MEDIUM**

The primary risk is accidental evolution toward an asymmetric follower/social graph.

### Migration Risk If Ignored

**LOW**

The relationship can be added later as a new Identity-owned model.

### Prepare Now?

**NO**

No persistent relationship model needs to exist in the MVP.

### Preparation Required

Maintain the existing principle that public Tracking authorization does not depend on social popularity or follower semantics.

### Implement Now?

**NO**

### ADR Required?

**MAYBE**

A small Identity-domain ADR may be appropriate when Phase 2 implementation begins.

### Future Trigger

Phase 2 implementation planning.

### Open Questions

- Whether accepted connections ever create a distinct "friends-only" visibility tier.
- Whether removing a connection is immediate and unilateral.
- Whether blocked-user behavior is required.

### Conclusion

**SAFE**

The capability can be added later without pre-building a social subsystem.

---

# 4.16 Installable PWA with Read-Only Offline Mode

### Product Classification

**Status:** Phase 2 Approved

### Purpose

Provide an installable mobile-friendly Shiori experience that remains useful during temporary loss of connectivity.

### Expected Capability

The installed PWA should allow read-only offline access to the most recently synchronized:

- User profile.
- Library.
- Statistics.

Offline mode does not allow progress mutation under the currently approved scope.

Writes require connectivity.

### Likely Ownership

**Primary Owner:** Client  
**Secondary Affected Services:** Identity, Tracking, Catalog APIs.

### Current Architecture

Shiori APIs are already designed to be platform-neutral and mobile-friendly.

The accepted API direction includes:

- Compact DTOs.
- Cursor pagination.
- Batch operations.
- Incremental synchronization using opaque synchronization tokens.

These existing principles are favorable to offline-capable clients.

### Future Data Requirements

No new server-side business domain data is inherently required.

The PWA will maintain a local client cache/snapshot of data the user has already synchronized.

### Historical Data Dependency

**NONE**

### Backfill Capability

**NOT REQUIRED**

### Cross-Service Impact

The PWA consumes existing public APIs.

It must not access service databases directly.

### Event / Integration Impact

None required for read-only offline mode.

### External Dependencies

Client/browser technologies such as:

- Service Workers.
- IndexedDB or equivalent browser storage.

Exact implementation belongs to frontend design.

### Privacy / Security Impact

**HIGH**

Offline data persists on the user's device.

Future client design must address:

- Logout cleanup.
- Shared-device risk.
- Token storage.
- Cache invalidation.
- Sensitive profile/library data at rest in browser storage.

### Scale / Performance Impact

**POSITIVE / LOW SERVER RISK**

Local cache may reduce repeated server reads.

Synchronization endpoints must remain efficient.

### Architecture Risk

**MEDIUM**

The business architecture already supports mobile synchronization reasonably well.

Most remaining risk is client-security and synchronization behavior.

### Migration Risk If Ignored

**LOW**

### Prepare Now?

**NO**

Existing incremental-sync and platform-neutral API decisions already provide the server-side extension point.

### Preparation Required

Do not remove mobile synchronization conventions from later API design.

No Service Worker, IndexedDB schema, or offline infrastructure is required during backend MVP implementation.

### Implement Now?

**NO**

### ADR Required?

**MAYBE**

A client/offline architecture decision should be written when Phase 2 PWA implementation begins.

### Future Trigger

Phase 2 client work.

### Open Questions

- Maximum offline retention.
- Logout/cache wipe semantics.
- Whether Catalog covers are cached offline.
- Stale-data indicators.
- Multi-account browser behavior.

### Conclusion

**SAFE**

The existing API direction already anticipates this form of client evolution.

---

# 4.17 Home Screen Widget

### Product Classification

**Status:** Phase 2 Approved

### Purpose

Provide fast access to selected Tracking information or quick-update actions from a device's home screen.

### Expected Capability

A widget may eventually display:

- Continue items.
- Current progress.
- A small number of quick actions.

### Likely Ownership

**Primary Owner:** Client  
**Secondary Affected Services:** Tracking API.

### Current Architecture

Tracking already plans compact responses, batch reads, idempotency, and optimistic concurrency.

### Future Data Requirements

No new historical data.

### Historical Data Dependency

**NONE**

### Backfill Capability

**NOT REQUIRED**

### Cross-Service Impact

Widget communication should occur through the public API boundary.

### Event / Integration Impact

None necessarily required.

### External Dependencies

Platform-specific widget frameworks.

### Privacy / Security Impact

**MEDIUM**

Widget content may be visible on a locked or shared device.

### Scale / Performance Impact

**LOW / MEDIUM**

Polling or refresh frequency must be controlled.

### Architecture Risk

**LOW**

### Migration Risk If Ignored

**LOW**

### Prepare Now?

**NO**

### Preparation Required

None beyond maintaining compact client-safe Tracking APIs.

### Implement Now?

**NO**

### ADR Required?

**NO / MAYBE**

depending on future client architecture.

### Future Trigger

Phase 2 client implementation.

### Open Questions

- Which platforms are supported.
- Whether quick updates are allowed directly from the widget.
- Lock-screen privacy behavior.

### Conclusion

**SAFE**

---

# 4.18 Ownership Tracking

### Product Classification

**Status:** Phase 2 Approved

### Purpose

Track what a user owns separately from what the user has consumed.

### Expected Capability

Ownership should be capable of becoming more precise than a single boolean.

Potential ownership dimensions include:

- Physical or digital.
- Specific volume.
- Specific edition.
- Language.
- Potential publisher/provider identity.

Progress and ownership remain independent.

A user may:

- Own something without having read it.
- Read something without owning it.
- Own only some volumes.

### Likely Ownership

**Primary Owner:** Tracking  
**Secondary Affected Services:** Catalog for edition/unit metadata.

### Current Architecture

Tracking owns the user's relationship with catalog content.

Catalog owns publication units and release metadata.

However, the current Catalog model is primarily consumption/release oriented rather than a complete commercial-edition database.

### Future Data Requirements

Potential future concepts include:

- Owned item.
- Catalog work.
- Specific volume/unit.
- Edition/language identity.
- Physical/digital form.

Exact schema is not selected.

### Historical Data Dependency

**NONE**

Users can begin recording ownership when the feature launches.

### Backfill Capability

**NOT REQUIRED**

### Cross-Service Impact

Tracking should store the user's ownership state.

Catalog may need to provide stable identifiers for editions or publication variants.

Tracking must not invent Catalog metadata independently.

### Event / Integration Impact

No mandatory event requirement today.

### External Dependencies

Potential metadata-provider limitations may become relevant.

No new provider is approved here.

### Privacy / Security Impact

**MEDIUM**

A physical collection can reveal purchasing/ownership information.

Public exposure must be opt-in.

### Scale / Performance Impact

**MEDIUM**

Per-volume ownership can produce substantially more records than one work-level flag.

### Architecture Risk

**MEDIUM**

The risk is incorrectly equating:

```text
publication progress unit
```

with:

```text
commercially owned edition
```

when those identities may differ.

### Migration Risk If Ignored

**MEDIUM**

A simple `owns = true` MVP-era model could become difficult to evolve if treated as the permanent ownership identity.

### Prepare Now?

**YES**

### Preparation Required

Do not add a speculative MVP `owns` boolean that becomes the permanent ownership model.

Keep progress-unit identity and future edition/ownership identity conceptually separate.

No edition subsystem needs to be implemented now.

### Implement Now?

**NO**

### ADR Required?

**MAYBE**

A Catalog/Tracking edition-ownership decision will likely be required before Phase 2 implementation.

### Future Trigger

Ownership feature design.

### Open Questions

- Source of edition metadata.
- Definition of digital ownership.
- Box sets and omnibus editions.
- Multiple copies/editions of the same volume.
- Whether price/purchase date ever belongs in scope.

### Conclusion

**PREPARE NOW**

---

# 4.19 Licensing Availability

### Product Classification

**Status:** Phase 2 Approved

### Purpose

Tell users whether a work has a verified official licensed release relevant to a language and region.

### Expected Capability

Availability may distinguish:

- Language.
- Country/region.
- Provider/publisher.
- Verification state.

Example:

```text
Language: Spanish
Region: Spain
Provider: <verified provider>
Status: Officially available
```

A global boolean such as `has_spanish_license = true` is not sufficient.

### Likely Ownership

**Primary Owner:** Catalog  
**Secondary Affected Services:** Tracking only when release-track selection uses this information.

### Current Architecture

Catalog already supports:

- Release tracks.
- Source/provenance.
- Official platform links.
- Region when known.
- Verification timestamps.

### Future Data Requirements

More structured licensing information may be required.

### Historical Data Dependency

**NONE / LOW**

Current verified state matters more than complete historical licensing history under the current scope.

### Backfill Capability

**FULL / PARTIAL**

Current licensing information can be hydrated from providers when available.

### Cross-Service Impact

Catalog remains the sole owner of licensing metadata.

Tracking may consume relevant projections when needed.

### Event / Integration Impact

Future licensing changes may need to be projected downstream.

No contract is defined here.

### External Dependencies

**HIGH PRODUCT DEPENDENCY**

Reliable licensing data depends on external data quality and provider coverage.

No additional provider is approved by Horizon.

### Privacy / Security Impact

**NONE**

### Scale / Performance Impact

**MEDIUM**

Region/language/provider dimensions can increase metadata cardinality.

### Architecture Risk

**MEDIUM**

### Migration Risk If Ignored

**LOW**

The Catalog document model is flexible and can add structured licensing data later.

### Prepare Now?

**NO**

Existing region, provenance, verification, and release-track concepts already provide appropriate architectural direction.

### Preparation Required

Do not reduce official availability to one global scalar field.

### Implement Now?

**NO**

### ADR Required?

**MAYBE**

only if new providers or a new canonical licensing model are introduced.

### Future Trigger

Phase 2 provider and licensing research.

### Open Questions

- Authoritative provider sources.
- Staleness/expiration.
- Licensing versus actual current availability.
- Territory granularity.
- Handling removed licenses.

### Conclusion

**SAFE**

---

# 4.20 Illustrator Gallery

### Product Classification

**Status:** Phase 2 Approved

### Purpose

Expose richer visual and credit metadata, particularly for Light Novel volumes.

### Expected Capability

Users may browse:

- Cover art.
- Illustrator credit.
- Volume association.

### Likely Ownership

**Primary Owner:** Catalog  
**Secondary Affected Services:** Client.

### Current Architecture

Catalog already owns:

- Images.
- Publication metadata.
- Provider synchronization.

### Future Data Requirements

Additional provider-backed media/credit metadata.

### Historical Data Dependency

**NONE**

### Backfill Capability

**FULL / PROVIDER-DEPENDENT**

### Cross-Service Impact

None.

### Event / Integration Impact

None beyond normal Catalog synchronization.

### External Dependencies

Primary dependence is provider metadata coverage.

Shiori should not become a manually maintained art wiki.

### Privacy / Security Impact

**NONE**

### Scale / Performance Impact

**MEDIUM**

Image delivery should use appropriate caching/CDN strategies when implemented.

### Architecture Risk

**LOW**

### Migration Risk If Ignored

**LOW**

### Prepare Now?

**NO**

### Preparation Required

None.

### Implement Now?

**NO**

### ADR Required?

**NO**

unless new external media providers are introduced.

### Future Trigger

Phase 2 provider coverage review.

### Open Questions

- Source coverage.
- Image rights/allowed usage.
- Volume-credit normalization.

### Conclusion

**SAFE**

---

# 4.21 Extended Localization

### Product Classification

**Status:** Phase 2 Approved

### Purpose

Expand Shiori beyond the initial English/Spanish interface while keeping independent user preferences for different language concerns.

### Expected Capability

Shiori should treat at least three language dimensions as conceptually separate:

1. **UI Language**
2. **Preferred Title Language**
3. **Preferred Release Language**

Changing one does not automatically change the others.

Example:

```text
UI Language: Spanish
Preferred Title Language: Romaji
Preferred Release Language: English
```

### Likely Ownership

**Primary Owner:** Mixed  
- UI preference: Identity/User Preferences.
- Title presentation: Catalog + user preference.
- Release-track preference: Tracking.

### Current Architecture

The MVP already includes:

- English and Spanish UI.
- Preferred title language.
- Per-work release-track selection.

### Future Data Requirements

Independent preference dimensions.

### Historical Data Dependency

**NONE**

### Backfill Capability

**FULL**

Defaults can be assigned when new language dimensions are introduced.

### Cross-Service Impact

This capability crosses service boundaries because language has different meanings in different domains.

No service should own a generic `language` field and attempt to use it for every concern.

### Event / Integration Impact

No mandatory event requirement.

### External Dependencies

Localization resources and provider metadata coverage.

### Privacy / Security Impact

**LOW**

### Scale / Performance Impact

**LOW / MEDIUM**

Localized metadata may increase cache variation.

### Architecture Risk

**MEDIUM**

The primary risk is collapsing distinct preferences into one global language field.

### Migration Risk If Ignored

**MEDIUM**

A generic language model can spread into APIs, caches, and service logic.

### Prepare Now?

**YES**

### Preparation Required

Model language concerns according to domain responsibility.

Do not treat:

```text
UI language
title language
release language
```

as synonyms.

No additional languages need to be implemented now.

### Implement Now?

**NO**

beyond the existing MVP English/Spanish requirements.

### ADR Required?

**MAYBE / YES**

The preference-ownership model should be clarified during Identity/Tracking architecture design.

### Future Trigger

Before user-preference contracts are frozen.

### Open Questions

- Fallback precedence.
- Per-device versus account-level UI language.
- Whether Preferred Release Language acts as a default for newly tracked works only.

### Conclusion

**PREPARE NOW**

---

# 4.22 Full Cast Directory

### Product Classification

**Status:** Phase 2 Approved

### Purpose

Expand the MVP's bounded character preview into complete cast exploration, including language-specific voice-acting information where providers support it.

### Expected Capability

Users may browse:

- Full cast.
- Character roles.
- Voice actors.
- Voice-language filters.

### Likely Ownership

**Primary Owner:** Catalog  
**Secondary Affected Services:** Client.

### Current Architecture

Catalog currently embeds a bounded subset of up to 10 main characters.

The accepted ADR explicitly leaves room to store or retrieve a full cast separately without changing that bounded subset.

### Future Data Requirements

Provider-backed full cast and voice-credit information.

### Historical Data Dependency

**NONE**

### Backfill Capability

**FULL / PROVIDER-DEPENDENT**

### Cross-Service Impact

None.

### Event / Integration Impact

None required outside Catalog unless another future consumer needs cast data.

### External Dependencies

Metadata-provider coverage.

Shiori should primarily use external metadata rather than become a manually maintained wiki.

### Privacy / Security Impact

**NONE**

### Scale / Performance Impact

**MEDIUM**

Full cast can be significantly larger than the MVP subset and should use separate pagination/read paths.

### Architecture Risk

**LOW**

### Migration Risk If Ignored

**LOW**

The current Subset Pattern already anticipates a separate full representation.

### Prepare Now?

**NO**

### Preparation Required

None beyond keeping the 10-character subset bounded rather than treating it as the complete canonical cast.

### Implement Now?

**NO**

### ADR Required?

**MAYBE**

The current ADR already lists full cast ownership as a future decision; a short follow-up ADR may be appropriate when implemented.

### Future Trigger

Phase 2 full-cast work.

### Open Questions

- Provider completeness.
- Cache-versus-on-demand strategy.
- Voice actor identity normalization across providers/languages.

### Conclusion

**SAFE**

---

# 4.23 Curated Franchise Consumption Guides

### Product Classification

**Status:** Future Candidate

### Purpose

Provide richer franchise guidance for cases where a single automatic "next work" is insufficient.

This capability is separate from Franchise Autopilot.

### Expected Capability

A franchise may eventually expose multiple explicit guide types such as:

```text
Recommended Order
Release Order
Chronological Order
Anime-Only Order
Source-Material Order
```

Structured Catalog relationships should provide the primary foundation.

Shiori may add its own curated guidance when the structured graph alone cannot express a useful route.

### Likely Ownership

**Primary Owner:** Catalog  
**Secondary Affected Services:** Client.

### Current Architecture

Catalog currently acts as an Anti-Corruption Layer over AniList and MangaDex and owns Shiori's internal franchise relationships.

Most current Catalog data is provider-backed, normalized, cached, or derived.

### Future Data Requirements

Potentially:

- Guide identity.
- Guide type.
- Ordered steps.
- Relationship to catalog items.
- Provenance.
- Revision/version.
- Editorial status.

The exact model is deferred.

### Historical Data Dependency

**NONE**

### Backfill Capability

**FULL**

Guides can be created later from existing Catalog relationships plus future curation.

### Cross-Service Impact

None required for the core guide.

Tracking may optionally annotate guide items with user progress.

### Event / Integration Impact

No mandatory event requirement.

### External Dependencies

AniList relationships remain the structured base.

Curated additions are Shiori-owned data.

### Privacy / Security Impact

**LOW**

### Scale / Performance Impact

**LOW / MEDIUM**

### Architecture Risk

**MEDIUM**

The important boundary question is whether Catalog is allowed to own first-party curated data in addition to normalized provider data.

### Migration Risk If Ignored

**LOW**

The MongoDB model can add a new collection later.

### Prepare Now?

**YES**

Not by building guide storage.

By explicitly preserving the architectural possibility that Catalog may own Shiori-authored franchise knowledge distinct from provider cache data.

### Preparation Required

The future ADR should distinguish:

- Provider-derived facts.
- Shiori-derived relationships.
- Shiori-curated guidance.

Curated content must not be disguised as provider truth.

### Implement Now?

**NO**

### ADR Required?

**YES**

A Catalog ownership/provenance decision should be recorded before this feature is ever implemented.

### Future Trigger

Product approval of Curated Guides.

### Open Questions

- Who may author/review a guide.
- Revision workflow.
- Provenance display.
- Whether users can choose among multiple official Shiori guides.
- How uncertain guidance is represented.

### Conclusion

**PREPARE NOW**

---

# 4.24 External Authentication Providers

### Product Classification

**Status:** Future Candidate

### Purpose

Allow users to authenticate through third-party identity providers such as Google or Apple without making those providers the canonical identity of the Shiori account.

### Expected Capability

A single Shiori user may eventually have multiple authentication methods:

```text
Shiori User
├── Local credential
├── Google identity
├── Apple identity
└── Future provider
```

Possible flows include:

```text
Register with password
→ link Google later
```

and:

```text
Register with Google
→ add local credential later
```

### Likely Ownership

**Primary Owner:** Identity

### Current Architecture

Identity uses:

- OpenIddict.
- PostgreSQL.
- Separate credential/authentication and public-profile concerns.

### Future Data Requirements

Stable Shiori user identity plus provider-link identities.

### Historical Data Dependency

**NONE**

### Backfill Capability

**FULL**

Existing accounts can link providers later if the canonical Shiori identity remains independent.

### Cross-Service Impact

None.

Other services should continue using the same stable Shiori user identifier regardless of login method.

### Event / Integration Impact

None required for other business services.

### External Dependencies

Future OAuth2/OIDC identity providers.

### Privacy / Security Impact

**HIGH**

Account linking introduces:

- Identity collision.
- Account takeover risk.
- Provider-email changes.
- Revocation/unlinking.
- Recovery flows.

### Scale / Performance Impact

**LOW**

### Architecture Risk

**HIGH**

If a Shiori user identity is made inseparable from one credential/provider, external linking becomes substantially harder.

### Migration Risk If Ignored

**HIGH**

Changing canonical user identity after Tracking data references it would be expensive and risky.

### Prepare Now?

**YES**

### Preparation Required

Identity v1 must preserve:

```text
Shiori User Identity
≠
Login Credential
≠
External Provider Identity
```

No Google/Apple integration is required now.

### Implement Now?

**NO**

### ADR Required?

**YES**

This should be part of the Identity internal architecture decision before Identity persistence is frozen.

### Future Trigger

Future approval of external authentication.

### Open Questions

- Account-link collision rules.
- Verified-email semantics.
- Unlinking the last login method.
- Provider-specific account deletion/revocation behavior.

### Conclusion

**PREPARE NOW**

---

# 4.25 Granular Profile Privacy

### Product Classification

**Status:** Future Candidate

### Purpose

Allow users to decide independently which parts of their tracking profile are publicly visible.

### Expected Capability

Potential controls may eventually include:

```text
Show Statistics
Show Favorites
Show Recent Progress
Show Public Lists
Show Selected Profile Metadata
```

The exact control set is not approved yet.

### Likely Ownership

**Primary Owner:** Identity for profile privacy policy  
**Secondary Affected Services:** Tracking for enforcing visibility of Tracking-owned data.

### Current Architecture

The MVP includes:

- Shareable profile.
- Private-by-default lists.
- Public-profile data in Identity.
- Public-library data in Tracking.

### Future Data Requirements

A privacy policy capable of expressing more than one global boolean.

### Historical Data Dependency

**NONE**

### Backfill Capability

**FULL**

Defaults can be assigned to existing accounts.

### Cross-Service Impact

**HIGH**

Identity may own profile-level preferences.

Tracking must enforce the resulting visibility for data it owns.

No service should leak private data simply because the overall profile is visible.

### Event / Integration Impact

Future cached/profile projections may need to observe privacy changes.

No contract is defined here.

### External Dependencies

None.

### Privacy / Security Impact

**CRITICAL TO FEATURE**

Privacy is the feature.

Default-deny behavior should be preferred when a visibility rule is missing or ambiguous.

### Scale / Performance Impact

**LOW / MEDIUM**

Authorization checks must remain efficient.

### Architecture Risk

**HIGH**

A coarse privacy model replicated through multiple APIs can become difficult to evolve safely.

### Migration Risk If Ignored

**HIGH**

Retrofitting granular authorization after public endpoints already exist risks accidental exposure.

### Prepare Now?

**YES**

### Preparation Required

Before public-profile APIs are finalized:

- Do not model privacy as one indivisible `IsPublic` assumption throughout the system.
- Keep Identity profile visibility and Tracking data visibility responsibilities explicit.
- Define safe composition rules.

Do not implement all future privacy toggles now.

### Implement Now?

**NO**

beyond the currently approved MVP profile/list privacy.

### ADR Required?

**YES**

### Future Trigger

Public-profile architecture design before the related MVP endpoints are frozen.

### Open Questions

- Exact future privacy dimensions.
- Default visibility of Favorites if Favorites enters MVP.
- Whether Friends ever introduce a friends-only tier.
- Interaction between Public and Unlisted profile visibility.

### Conclusion

**PREPARE NOW**

---

# 4.26 Optional Demographics for Aggregate Analytics

### Product Classification

**Status:** Future Candidate

### Purpose

Allow users to voluntarily provide limited demographic information for clearly defined future aggregate analytics.

### Expected Capability

Potential fields may include:

- Country.
- Age range.
- Other future dimensions only after a specific analytical need exists.

Data is explicitly supplied by the user.

Shiori must perform **no automatic demographic inference from IP address** under the current product direction.

Country is the only currently anticipated demographic field that may optionally be shown on the user's public profile.

All other demographic information remains non-public.

### Likely Ownership

**Primary Owner:** Identity  
**Secondary Affected Services:** Future Analytics capability.

### Current Architecture

Identity owns user/profile information.

No demographic collection is currently required by the MVP.

### Future Data Requirements

Only fields approved at the time the analytical purpose exists.

Potentially:

- Value.
- Collection purpose.
- Consent metadata where required.
- Visibility setting for country.

### Historical Data Dependency

**NONE**

Analytics begins with voluntarily supplied data after the capability exists.

### Backfill Capability

**NOT REQUIRED**

Users cannot and should not be assigned demographic data retroactively without their input.

### Cross-Service Impact

Future Analytics must not directly read Identity's database.

An approved privacy-preserving integration mechanism will be required if this feature is implemented.

### Event / Integration Impact

Potential future demographic/consent changes may need to be projected into analytics.

No event contract is approved.

### External Dependencies

None.

### Privacy / Security Impact

**HIGH**

Demographic data has stronger privacy requirements than ordinary display preferences.

### Scale / Performance Impact

**LOW** for Identity storage.

Aggregate analytics impact belongs to the separate Aggregate Product Analytics capability.

### Architecture Risk

**MEDIUM**

### Migration Risk If Ignored

**LOW**

Because no demographic data is being collected during MVP.

### Prepare Now?

**NO**

The correct preparation is primarily a product/privacy rule:

do not collect the data before the capability and consent model exist.

Granular Profile Privacy already covers the broader public-visibility architecture.

### Preparation Required

None in persistence today.

Continue separating:

```text
Core Account
Public Profile
Optional Demographic Data
```

conceptually.

### Implement Now?

**NO**

### ADR Required?

**MAYBE**

A dedicated privacy/data-use ADR will likely be required before collection begins.

### Future Trigger

A concrete approved aggregate-analytics requirement.

### Open Questions

- Exact demographic fields.
- Retention.
- Withdrawal semantics.
- Minimum aggregate cohort size.
- Whether country changes preserve historical analytical geography or use current country only.

### Conclusion

**SAFE**

No speculative demographic storage should be created now.

---

# 4.27 Aggregate Product Analytics

### Product Classification

**Status:** Future Candidate

### Purpose

Analyze aggregate usage patterns across Shiori users for product understanding, while remaining separate from personal Deep Statistics.

Potential examples include questions such as:

```text
What formats are most commonly tracked by country?
Which genres are most represented in a region?
How does aggregate format interest change over time?
```

The product requirements are intentionally not yet detailed enough to approve a specific analytics architecture.

### Expected Capability

Unknown beyond aggregate, privacy-conscious product analytics.

This capability must not automatically imply:

- Selling user data.
- Advertising profiles.
- Social recommendation profiles.
- Public individual-level analytics.

### Likely Ownership

**Primary Owner:** Future Analytics Capability  
**Secondary Affected Services:** Potentially Identity, Catalog, Tracking.

### Current Architecture

Identity, Catalog, and Tracking each own separate operational databases.

RabbitMQ is designed for business messaging, not as a permanent replayable analytical event log.

### Future Data Requirements

**NOT YET DEFINED**

Possible dimensions may include:

- Tracking activity.
- Catalog metadata.
- Time.
- Optional country.
- Other approved aggregate dimensions.

### Historical Data Dependency

**UNKNOWN / POTENTIALLY HIGH**

This depends entirely on the future questions the product wants to answer.

### Backfill Capability

**UNKNOWN / PARTIAL**

Some aggregates can be derived from current operational state.

Historical trends cannot necessarily be reconstructed.

### Cross-Service Impact

Potentially significant.

A future analytics capability must not solve the problem by directly coupling itself to every operational database without an explicit architectural decision.

### Event / Integration Impact

Potentially significant.

However, creating speculative analytics events now would violate Horizon's no-prebuilding rule.

### External Dependencies

None currently approved.

### Privacy / Security Impact

**HIGH**

Aggregate analytics may combine data from multiple domains.

Privacy boundaries, aggregation thresholds, retention, and consent requirements must be explicitly defined before implementation.

### Scale / Performance Impact

**HIGH**

Analytical queries must not degrade operational Identity, Catalog, or Tracking workloads.

### Architecture Risk

**HIGH**

### Migration Risk If Ignored

**UNKNOWN**

The risk cannot be classified reliably until the required historical questions are known.

### Prepare Now?

**NO — PENDING PRODUCT DEFINITION**

It would be premature to build event warehouses, analytical stores, or extra telemetry without concrete questions.

### Preparation Required

Preserve Database-per-Service boundaries.

Do not assume future Analytics may directly query operational databases merely because it is internal.

### Implement Now?

**NO**

### ADR Required?

**YES — WHEN PRODUCT SCOPE EXISTS**

### Future Trigger

A concrete approved set of aggregate questions and privacy requirements.

### Open Questions

- Exact business questions.
- Historical period required.
- Consent basis.
- Aggregation/anonymization rules.
- Whether demographics are necessary at all.
- Data retention.
- Analytical storage technology.
- Whether near-real-time analytics are required.

### Conclusion

**NEEDS PRODUCT DECISION**

The capability is plausible, but its architecture must not be guessed in advance.

---

# 4.28 Per-Work Discussion

### Product Classification

**Status:** Needs Product Review

### Purpose

Previously proposed as commentary attached to a catalog work.

Its fit with the clarified Tracker-First direction is now uncertain.

### Expected Capability

Not sufficiently approved to define beyond the historical concept of work-scoped user discussion.

### Likely Ownership

**Primary Owner:** TBD  
**Secondary Affected Services:** Catalog identity/reference only.

### Current Architecture

No current service owns:

- User-generated discussion content.
- Moderation.
- Reports.
- Community governance.

This is intentional.

### Future Data Requirements

Undefined.

### Historical Data Dependency

**NONE**

### Backfill Capability

**NOT REQUIRED**

### Cross-Service Impact

Unknown.

A discussion system should not be placed into Catalog merely because discussions reference catalog items.

### Event / Integration Impact

Undefined.

### External Dependencies

Potential moderation/abuse tooling may eventually be required.

### Privacy / Security Impact

**HIGH**

Any user-generated discussion capability introduces:

- Abuse.
- Harassment.
- Moderation.
- Reporting.
- Content lifecycle.
- Potential legal/compliance concerns.

### Scale / Performance Impact

**UNKNOWN / POTENTIALLY HIGH**

### Architecture Risk

**HIGH IF IMPLEMENTED WITHOUT PRODUCT REVIEW**

### Migration Risk If Ignored

**LOW**

No current architecture needs to prepare for discussion.

### Prepare Now?

**NO**

### Preparation Required

None.

Do not introduce:

- Comment tables.
- Moderation services.
- Social feeds.
- User-generated content infrastructure.

during MVP architecture work.

### Implement Now?

**NO**

### ADR Required?

**NO NOW**

A new architecture review would be mandatory if the product ever re-approves the capability.

### Future Trigger

Explicit product reconsideration demonstrating that discussion adds meaningful Tracker-First value.

### Open Questions

Essentially the entire product capability remains open.

### Conclusion

**NEEDS PRODUCT DECISION**

# 5. Architecture Impact Matrix

## 5.0 Purpose

This section compresses the detailed Architecture Horizon Fiches from Section 4 into one decision-oriented view.

The matrix is not a replacement for the fiches.

Its purpose is to make the architectural pressure visible at a glance:

- Which capabilities are safe to defer.
- Which capabilities require preparation before MVP architecture is frozen.
- Which capabilities depend on historical data that cannot be reconstructed later.
- Which capabilities create meaningful migration risk.
- Which capabilities require an ADR or explicit architecture review.
- Which capabilities are still too undefined to justify architectural preparation.

The matrix must always be interpreted together with the detailed reasoning in Section 4.

---

## 5.1 Master Architecture Impact Matrix

| Capability | Product Status | Primary Owner | Historical Data Dependency | Backfill Capability | Architecture Risk | Migration Risk If Ignored | Prepare Now | ADR Required | Horizon Conclusion |
|---|---|---|---|---|---|---|---|---|---|
| Favorites | MVP Candidate | Tracking | None | Full / Not required | Low | Low | No | No | SAFE |
| Search Autocomplete | MVP Candidate | Catalog | None | Full | Low | Low | No | No | SAFE |
| Unlisted Profile | MVP Candidate | Identity | None | Full | Medium | Medium | **Yes** | **Yes** | **PREPARE NOW** |
| Franchise Autopilot | Phase 2 Approved | Catalog | Low | Full | Medium | Low | No | No | SAFE |
| Interactive Franchise Tree | Phase 2 Approved | Catalog | None | Full | Low | Low | No | No | SAFE |
| Annual Wrapped | Phase 2 Approved | Tracking | **High** | None / Partial | Medium | **High** | **Yes** | **Yes** | **PREPARE NOW** |
| Deep Statistics / Personal Analytics | Phase 2 Approved | Tracking | **High** | Partial | Medium | **High** | **Yes** | **Yes** | **PREPARE NOW** |
| Push Notifications | Phase 2 Approved | Future Notification Capability | None | Not required | Medium | Medium | **Yes** | Maybe | **PREPARE NOW** |
| Full Progress Timeline | Phase 2 Approved | Tracking | **High** | None / Partial | **High** | **High** | **Yes** | **Yes** | **PREPARE NOW** |
| Granular Scoring | Phase 2 Approved | Tracking | None | None / Not required | Medium | Medium | **Yes** | Maybe | **PREPARE NOW** |
| Custom Lists | Phase 2 Approved | Tracking | None | Full / Not required | Low | Low | No | No | SAFE |
| Rewatch & Reread Tracking | Phase 2 Approved | Tracking | **High** | Partial / Unknown | **High** | **High** | **Yes** | **Yes** | **PREPARE NOW** |
| Personalized Recommendations | Phase 2 Approved | Future Recommendation Capability / Tracking read model | Medium | Partial / Full | Medium | Low | No | Maybe | SAFE |
| List Comparison | Phase 2 Approved | Tracking | None | Full | Low | Low | No | No | SAFE |
| Friends / Connections | Phase 2 Approved | Identity | None | Not required | Medium | Low | No | Maybe | SAFE |
| Installable PWA with Read-Only Offline Mode | Phase 2 Approved | Client | None | Not required | Medium | Low | No | Maybe | SAFE |
| Home Screen Widget | Phase 2 Approved | Client | None | Not required | Low | Low | No | No / Maybe | SAFE |
| Ownership Tracking | Phase 2 Approved | Tracking | None | Not required | Medium | Medium | **Yes** | Maybe | **PREPARE NOW** |
| Licensing Availability | Phase 2 Approved | Catalog | None / Low | Full / Partial | Medium | Low | No | Maybe | SAFE |
| Illustrator Gallery | Phase 2 Approved | Catalog | None | Full / Provider-dependent | Low | Low | No | No | SAFE |
| Extended Localization | Phase 2 Approved | Mixed | None | Full | Medium | Medium | **Yes** | Maybe / Yes | **PREPARE NOW** |
| Full Cast Directory | Phase 2 Approved | Catalog | None | Full / Provider-dependent | Low | Low | No | Maybe | SAFE |
| Curated Franchise Consumption Guides | Future Candidate | Catalog | None | Full | Medium | Low | **Yes** | **Yes** | **PREPARE NOW** |
| External Authentication Providers | Future Candidate | Identity | None | Full | **High** | **High** | **Yes** | **Yes** | **PREPARE NOW** |
| Granular Profile Privacy | Future Candidate | Identity | None | Full | **High** | **High** | **Yes** | **Yes** | **PREPARE NOW** |
| Optional Demographics for Aggregate Analytics | Future Candidate | Identity | None | Not required | Medium | Low | No | Maybe | SAFE |
| Aggregate Product Analytics | Future Candidate | Future Analytics Capability | Unknown / Potentially High | Unknown / Partial | **High** | Unknown | No — pending product definition | Yes — when product scope exists | **NEEDS PRODUCT DECISION** |
| Per-Work Discussion | Needs Product Review | TBD | None | Not required | High if implemented without product review | Low | No | No now | **NEEDS PRODUCT DECISION** |

---

## 5.2 Matrix Totals

The current Horizon contains **28 evaluated capabilities**.

### By Horizon Conclusion

| Conclusion | Count | Meaning |
|---|---:|---|
| **SAFE** | 14 | No special MVP architectural preparation is required. |
| **PREPARE NOW** | 12 | The future feature remains unimplemented, but an architectural property must be preserved or decided before the affected MVP component is frozen. |
| **NEEDS PRODUCT DECISION** | 2 | The capability is not defined sufficiently to justify speculative architecture. |

This distribution is intentional.

Horizon is not expected to classify every future feature as `PREPARE NOW`.

If most future capabilities required speculative MVP construction, the document would be expanding the MVP rather than protecting it.

---

## 5.3 Capabilities Requiring Preparation Before Architecture Freeze

The following capabilities currently produce a `PREPARE NOW` conclusion:

| Capability | Main Reason for Preparation |
|---|---|
| **Unlisted Profile** | Avoid hard-coding profile visibility as one irreversible public/private boolean across persistence, APIs, caches, and authorization. |
| **Annual Wrapped** | Preserve first-party Shiori historical activity that cannot be recreated accurately later. |
| **Deep Statistics / Personal Analytics** | Preserve meaningful historical state transitions and analytical dimensions without running future analytics against hot write paths. |
| **Push Notifications** | Ensure future consumers can observe meaningful release facts rather than only generic entity-change signals. |
| **Full Progress Timeline** | Preserve status/progress transitions plus client/device context that cannot be backfilled after the fact. |
| **Granular Scoring** | Ensure future per-unit scores can belong to a stable consumption-run identity. |
| **Rewatch & Reread Tracking** | Prevent the current Tracking model from permanently collapsing library membership and one specific consumption run into the same concept. |
| **Ownership Tracking** | Avoid treating a simple work-level ownership boolean as the permanent model when future ownership may be edition-, language-, and volume-specific. |
| **Extended Localization** | Keep UI language, preferred title language, and preferred release language as separate domain concerns. |
| **Curated Franchise Consumption Guides** | Preserve the possibility that Catalog may own clearly identified Shiori-curated knowledge in addition to provider-derived data. |
| **External Authentication Providers** | Keep canonical Shiori identity separate from local credentials and external provider identities. |
| **Granular Profile Privacy** | Avoid public-profile authorization becoming dependent on one coarse visibility flag and define safe Identity/Tracking composition boundaries. |

These capabilities are the main architectural inputs that will later feed Sections 6 through 8.

---

## 5.4 High Historical-Data Pressure

The following capabilities have the strongest dependency on data that may be impossible or unreliable to reconstruct after launch:

| Capability | Historical Dependency | Backfill | Why It Matters |
|---|---|---|---|
| **Annual Wrapped** | High | None / Partial | A yearly activity sequence cannot be reconstructed accurately if Shiori never recorded it during that year. |
| **Deep Statistics / Personal Analytics** | High | Partial | Current state can recover some totals, but not all historical behavior or transitions. |
| **Full Progress Timeline** | High | None / Partial | Missing transitions, timestamps, and client/device context cannot be recreated later. |
| **Rewatch & Reread Tracking** | High | Partial / Unknown | Historical run boundaries may become ambiguous if repeat consumption is recorded without an explicit run concept. |

These four capabilities create the strongest pressure on the Tracking history model.

They do **not** justify collecting arbitrary telemetry.

They justify preserving only the historical facts that have a defined product purpose.

---

## 5.5 Highest Architecture and Migration Risk

### High Architecture Risk

The current fiches classify the following as especially sensitive:

- Full Progress Timeline.
- Rewatch & Reread Tracking.
- External Authentication Providers.
- Granular Profile Privacy.
- Aggregate Product Analytics.
- Per-Work Discussion if implemented without renewed product review.

These features pressure core architectural boundaries rather than merely adding data.

### High Migration Risk If Ignored

The following have clearly identified high migration risk:

- Annual Wrapped.
- Deep Statistics / Personal Analytics.
- Full Progress Timeline.
- Rewatch & Reread Tracking.
- External Authentication Providers.
- Granular Profile Privacy.

These deserve special attention because delaying the relevant architecture decision could make later adoption substantially more expensive or unsafe.

---

## 5.6 Safe-to-Defer Capabilities

The following capabilities currently require no special MVP preparation:

- Favorites.
- Search Autocomplete.
- Franchise Autopilot.
- Interactive Franchise Tree.
- Custom Lists.
- Personalized Recommendations.
- List Comparison.
- Friends / Connections.
- Installable PWA with Read-Only Offline Mode.
- Home Screen Widget.
- Licensing Availability.
- Illustrator Gallery.
- Full Cast Directory.
- Optional Demographics for Aggregate Analytics.

`SAFE` does not mean:

> The feature is trivial.

It means:

> The current architecture already leaves a reasonable additive path for implementing it later.

Some `SAFE` capabilities may still require significant engineering when their implementation phase arrives.

For example:

- PWA offline storage still requires careful client security.
- Recommendations may require expensive computation.
- Licensing data may require additional providers.
- Friends / Connections still require authorization and abuse-related product decisions.

Those costs do not currently justify speculative MVP architecture.

---

## 5.7 Capabilities That Must Remain Undefined for Now

Two capabilities currently end in `NEEDS PRODUCT DECISION`.

### Aggregate Product Analytics

The product questions are not specific enough to justify:

- An analytics warehouse.
- Extra analytical event streams.
- Cross-service telemetry.
- Demographic collection.
- New retention policies.

The only architectural rule preserved today is that future Analytics must respect Database-per-Service boundaries and cannot assume direct access to operational databases.

### Per-Work Discussion

Discussion is no longer approved Phase 2 scope.

Its relationship to Tracker-First is unresolved.

Therefore Shiori should not pre-build:

- Comment storage.
- Moderation infrastructure.
- User-generated-content pipelines.
- Social feeds.

A completely new product review is required before architecture work begins.

---

## 5.8 Architectural Pressure by Current Service

### Identity

The strongest Horizon pressures on Identity are:

- Unlisted Profile.
- External Authentication Providers.
- Granular Profile Privacy.
- Extended Localization preferences.
- Future Friends / Connections.
- Optional Demographics.

The key long-term Identity principles emerging from Horizon are:

```text
Canonical Shiori User
    ≠ Credential
    ≠ External Identity

Profile Identity
    ≠ Tracking Data

Profile Visibility
    ≠ One permanent boolean assumption

Core Account Data
    ≠ Optional Demographic Data
```

---

### Catalog

The strongest Horizon pressures on Catalog are:

- Franchise Autopilot.
- Interactive Franchise Tree.
- Curated Franchise Consumption Guides.
- Licensing Availability.
- Ownership-related edition metadata.
- Full Cast Directory.
- Illustrator Gallery.

The key long-term Catalog principles emerging from Horizon are:

```text
Relationship Graph
    ≠ Guaranteed Consumption Order

Provider-Derived Data
    ≠ Shiori-Curated Knowledge

Publication Unit
    ≠ Necessarily a Commercial Edition

Official Availability
    ≠ One global language/region boolean
```

---

### Tracking

Tracking receives the greatest Horizon pressure.

Relevant capabilities include:

- Favorites.
- Annual Wrapped.
- Personal Analytics.
- Full Progress Timeline.
- Granular Scoring.
- Custom Lists.
- Rewatch / Reread.
- Personalized Recommendations.
- List Comparison.
- Ownership Tracking.
- Shared-profile data.

The key long-term Tracking principles emerging from Horizon are:

```text
Library Relationship
    ≠ One Consumption Run

Current State
    ≠ Complete Historical Record

Overall Work Rating
    ≠ Per-Run / Per-Unit Rating

Progress
    ≠ Ownership

Private Tracking Data
    ≠ Public Profile Data
```

---

### Future Capabilities

Some features may eventually justify their own bounded context or deployment unit:

- Notifications.
- Recommendations.
- Aggregate Analytics.

Horizon does not approve those services today.

The rule remains:

> A future service is created only when an implemented product capability justifies an independent ownership, scaling, consistency, or operational boundary.

---

## 5.9 Cross-Cutting Architecture Themes

The matrix reveals several recurring architecture themes that affect more than one feature.

### A. Historical Integrity

Driven by:

- Annual Wrapped.
- Personal Analytics.
- Full Progress Timeline.
- Rewatch / Reread.

The architecture must preserve meaningful history without claiming that recorded tracking proves real-world consumption.

---

### B. Stable Identity

Driven by:

- External Authentication.
- Friends / Connections.
- Shared profiles.
- Cross-service Tracking ownership.

A Shiori user identifier must remain stable independently of login method.

---

### C. Privacy as Policy, Not Presentation

Driven by:

- Shareable Profile.
- Unlisted Profile.
- Granular Profile Privacy.
- List Comparison.
- Friends / Connections.
- Optional country visibility.

Privacy decisions must be enforced by the service that owns the data, not only hidden in the frontend.

---

### D. Semantic Events

Driven primarily by:

- Push Notifications.
- Future analytical consumers.
- Existing Catalog-to-Tracking projection flows.

Events should represent meaningful business facts rather than forcing every future consumer to infer meaning from a generic "entity updated" notification.

This does not mean every possible future event should be created during MVP.

---

### E. Distinct Domain Concepts Must Stay Distinct

Several future migration risks come from collapsing two concepts that happen to look similar during MVP:

```text
Library relationship
≠ Consumption run

UI language
≠ Title language
≠ Release language

Publication unit
≠ Commercial edition

Profile visibility
≠ Visibility of every Tracking field

Credential
≠ User identity
```

Preserving these conceptual separations is more important than pre-building future tables.

---

## 5.10 Consistency Review

The matrix produces several combinations that may initially appear contradictory but are intentional.

### Push Notifications

```text
Architecture Risk: Medium
Prepare Now: Yes
ADR Required: Maybe
```

Preparation concerns semantic event capability.

The decision to create a dedicated Notification Service can wait.

---

### Optional Demographics

```text
Privacy Impact: High
Prepare Now: No
```

This is intentional because the MVP is not collecting demographic data.

The safest current preparation is **not collecting it prematurely**.

A privacy/data-use ADR becomes necessary before collection begins.

---

### Aggregate Product Analytics

```text
Architecture Risk: High
Prepare Now: No
```

This is also intentional.

The capability is too undefined to justify speculative infrastructure.

High uncertainty does not automatically justify building ahead.

---

### Personalized Recommendations

```text
Scale Impact: Medium / High
Prepare Now: No
```

Computation may eventually be expensive, but the feature can be added through future read models or background processing.

No irreversible MVP decision has been identified.

---

### Installable PWA with Offline Read Mode

```text
Privacy Impact: High
Prepare Now: No
```

The current backend API direction already includes incremental synchronization and mobile-friendly contracts.

The remaining high-risk concerns are primarily future client-storage and session-security decisions.

---

## 5.11 Matrix Output

The Architecture Impact Matrix produces three concrete sets for the next stages of Product Horizon.

### Set A — Safe Additive Evolution

```text
Favorites
Search Autocomplete
Franchise Autopilot
Interactive Franchise Tree
Custom Lists
Personalized Recommendations
List Comparison
Friends / Connections
Installable PWA with Read-Only Offline Mode
Home Screen Widget
Licensing Availability
Illustrator Gallery
Full Cast Directory
Optional Demographics
```

These require no special speculative MVP construction.

---

### Set B — Architecture Preparation Required

```text
Unlisted Profile
Annual Wrapped
Deep Statistics / Personal Analytics
Push Notifications
Full Progress Timeline
Granular Scoring
Rewatch & Reread Tracking
Ownership Tracking
Extended Localization
Curated Franchise Consumption Guides
External Authentication Providers
Granular Profile Privacy
```

These will become the primary inputs to the High-Risk Stress Tests and later ADR work.

---

### Set C — Product Decision Required Before Architecture

```text
Aggregate Product Analytics
Per-Work Discussion
```

No speculative architecture should be introduced for these capabilities until their product requirements are sufficiently defined.

---

## 5.12 Section Conclusion

The current Shiori architecture is not broadly blocked by the known product horizon.

Half of the evaluated future capabilities can be added through normal additive evolution without special MVP preparation.

The most important pre-MVP architectural pressure is concentrated in a smaller set of domains:

1. Tracking history and consumption-run identity.
2. Stable Identity independent of authentication credentials.
3. Public-profile privacy and cross-service visibility.
4. Semantic integration events for future consumers.
5. Separation of language concerns.
6. Separation of publication progress from physical/digital edition ownership.
7. Explicit provenance for future Shiori-curated Catalog knowledge.

These are the areas that Section 6 must stress-test before Horizon can conclude that Architecture v1.0 is safe to freeze.

# 6. High-Risk Stress Tests

## 6.0 Purpose

Section 5 identified the capabilities that create the strongest architectural pressure before Shiori's MVP architecture can be frozen.

This section does not design those future features.

Instead, it stress-tests the **current architectural direction** against realistic future scenarios and asks:

> If this capability arrives later, can Shiori evolve additively, or would the MVP architecture force data loss, destructive migration, broken service boundaries, or unsafe behavior?

A stress test is considered successful only when the architecture can preserve the required future capability **without implementing that capability now**.

The tests focus on the architectural pressure identified in Section 5:

1. Tracking history and consumption-run identity.
2. Historical durability for Timeline, Wrapped, and Personal Analytics.
3. Stable Shiori identity independent of authentication credentials.
4. Public-profile privacy and cross-service visibility.
5. Semantic release events for future notifications.
6. Separation of language concerns.
7. Separation of progress units from ownership/edition identity.
8. Provenance for future Shiori-curated Catalog knowledge.

A final composite scenario then tests how these concerns interact across the full system.

---

## 6.1 Stress Test A — Rewatch / Reread Without Destroying the Original History

### Capabilities Under Test

- Rewatch & Reread Tracking.
- Granular Scoring.
- Full Progress Timeline.
- Annual Wrapped.
- Deep Statistics / Personal Analytics.

### Current Architectural Pressure

The accepted Tracking architecture currently centers progress around one active `tracking_entry` per user and catalog item.

That model is sufficient for the MVP's single active consumption flow.

It becomes risky if the same object is expected to represent all of the following permanently:

```text
User's library relationship with a work
+
current progress
+
one specific consumption experience
+
all future repeated consumption experiences
```

Those are not necessarily the same domain concept.

### Future Scenario

A user:

1. Starts an Anime in 2027.
2. Completes it.
3. Rates the overall work.
4. Later starts a second watch in 2029.
5. Gives individual episode ratings during the second watch.
6. Stops halfway through that second watch.
7. Starts a third watch years later.

Shiori must still be able to answer:

```text
When was the first watch completed?

What progress belongs to the second watch?

Which granular scores belong to the second watch?

What happened during the third watch?

What is the user's current relationship with the work?

What should appear in historical statistics for each year?
```

### Failure Mode

The architecture fails this test if supporting the second watch requires:

- Overwriting the first watch's dates.
- Reusing one progress identity for incompatible historical runs.
- Guessing run boundaries from old snapshots.
- Changing the canonical Shiori catalog or user identifiers.
- Reinterpreting every historical `tracking_entry`.
- Treating a second watch as an unrelated duplicate library item.

### Required Architectural Property

Tracking must be capable of distinguishing conceptually between:

```text
Persistent User-to-Work Relationship
                │
                └── Consumption Run(s)
```

The exact implementation remains undecided.

Horizon does **not** require:

- A `cycle_number`.
- A `consumption_runs` table.
- A particular foreign-key structure.
- Multiple active runs.

It requires only that the MVP schema not make those concepts impossible to separate later.

### Pass Criteria

This stress test passes when the pre-MVP Tracking design guarantees that:

- A stable user-to-work relationship can survive multiple future runs.
- Historical runs can have independent identity.
- Starting a new run does not destroy a completed previous run.
- Future per-unit scoring can belong to one specific run.
- Progress history can identify which run a recorded change belongs to.
- MVP behavior still supports one normal active consumption flow without implementing Phase 2 Rewatch.

### Current Horizon Result

**REQUIRES ARCHITECTURE DECISION BEFORE TRACKING SCHEMA FREEZE**

The current direction is evolvable, but the existing one-active-entry rule is not sufficient by itself to prove safe future Rewatch/Reread evolution.

### ADR Input

A later architecture decision must explicitly define the relationship between:

- Library membership.
- Current tracking state.
- Consumption-run identity.
- Progress history.

---

## 6.2 Stress Test B — Historical Data Survives Long Enough to Power Timeline, Wrapped, and Personal Analytics

### Capabilities Under Test

- Full Progress Timeline.
- Annual Wrapped.
- Deep Statistics / Personal Analytics.
- Progress Vault.
- Rewatch & Reread.

### Current Architectural Pressure

The existing architecture preserves immutable progress snapshots in `progress_history`, populated through database triggers.

That is a strong starting point.

However, the future product horizon requires more than simply knowing the latest numerical position.

Future history may need to explain:

- What changed.
- When it changed.
- Which status transition occurred.
- Which client/device produced the update.
- Whether the change originated from ordinary Shiori usage or an import.
- Which future consumption run the change belongs to.

Some of those facts may not be reconstructable from a generic state snapshot alone.

### Future Scenario

During one year, a user:

```text
Planned
→ In Progress
→ Episode 4
→ Episode 5
→ Paused
→ In Progress
→ Episode 6
→ Completed
```

Some changes are made from the PWA.

Another change is made from a future mobile client.

Later, the user imports historical records for unrelated works.

At the end of the year:

- Progress Timeline must show the recorded state transitions.
- Wrapped must count activity recorded by Shiori during that year.
- Imported historical activity must not be mistaken for first-party observed activity.
- Personal Analytics must be able to analyze legitimate historical Tracking facts.

### Failure Mode

The architecture fails if:

- Only the current state survives.
- Status transitions are not historically preserved.
- Import writes are indistinguishable from ordinary recorded activity when the distinction matters.
- Device/client context is required but was never stored.
- A database trigger records state but cannot receive required application context and no alternative mechanism exists.
- Progress Vault undo mutates or deletes the immutable historical record.

### Required Architectural Property

The MVP history design must preserve meaningful **recorded Tracking facts**, not merely enough information to restore one accidental update.

At minimum, the architecture review must explicitly decide the semantics of:

- Recorded timestamp.
- Previous state.
- Resulting state.
- Progress type.
- Library-status transition.
- Mutation source/origin when required.
- Client/device context when required.
- Future consumption-run association.
- Undo behavior relative to immutable history.

### Important Boundary

This test does **not** authorize Shiori to collect arbitrary behavioral telemetry.

The required history is limited to product-defined Tracking state.

Shiori must continue distinguishing:

```text
Recorded tracking activity
≠
Verified real-world consumption
```

### Pass Criteria

The test passes when:

- Every product-supported Tracking mutation leaves sufficient immutable history.
- Required context cannot be silently omitted by one write path.
- Imported historical data can be distinguished from first-party Shiori activity where needed.
- Progress Vault can restore state without erasing the historical fact that an update occurred.
- Full Timeline can be added later without inventing missing historical transitions.
- Wrapped can summarize activity Shiori actually recorded during the target year.
- Personal Analytics can use history without requiring destructive reconstruction of past state.

### Current Horizon Result

**REQUIRES ARCHITECTURE DECISION BEFORE TRACKING HISTORY IMPLEMENTATION**

The existing `progress_history` direction is correct, but its final semantic contract must be stronger than "JSONB snapshots exist."

### ADR Input

A later architecture decision must define the Tracking history/audit contract before Milestone 3 persistence is finalized.

---

## 6.3 Stress Test C — External Login Providers Without Changing the Shiori User Identity

### Capabilities Under Test

- External Authentication Providers.
- Existing local account access.
- Future account linking.
- All downstream Tracking ownership.

### Current Architectural Pressure

Identity uses OpenIddict and separates credentials from the public user profile.

The future requirement adds an important invariant:

```text
Shiori User Identity
≠
Local Credential
≠
External Provider Identity
```

Other services must never care whether the user authenticated with:

- Password.
- Google.
- Apple.
- A future standards-compatible provider.

### Future Scenario A — Local First

A user:

1. Creates a Shiori account with email/password.
2. Tracks 500 works.
3. Later links Google.
4. Signs in through Google.

The existing Tracking data must still belong to the same Shiori user.

### Future Scenario B — External First

Another user:

1. Creates the account through Google.
2. Later adds a local credential.
3. Eventually unlinks Google.

The account must remain the same Shiori identity if another valid authentication method remains.

### Future Scenario C — Provider Changes

A provider:

- Changes an email address.
- Stops returning a previously expected claim.
- Revokes access.

Those provider changes must not silently create a new Shiori user.

### Failure Mode

The architecture fails if:

- `UserId = GoogleId`.
- Email is treated as the immutable cross-service identity.
- Linking a provider creates a second Shiori account by default.
- Tracking records must be migrated when login method changes.
- External-provider identifiers leak into Catalog or Tracking as canonical user identifiers.

### Required Architectural Property

Identity must own one stable Shiori user identity.

Credentials and external identities must authenticate **into** that identity.

They must not become the identity itself.

### Pass Criteria

The test passes when:

- One Shiori user can have multiple future authentication methods.
- Downstream services reference only the stable Shiori user identifier.
- Adding/removing an authentication method does not change Tracking ownership.
- Provider-specific identifiers remain inside Identity.
- Account-link collision and recovery policy can be added later without changing user identifiers.

### Current Horizon Result

**CONDITIONALLY SAFE — REQUIRES IDENTITY MODEL DECISION BEFORE IDENTITY PERSISTENCE FREEZE**

OpenIddict provides the standards foundation, but Horizon requires the canonical-user-versus-credential distinction to become explicit architecture.

### ADR Input

The Identity architecture decision must preserve a canonical Shiori account independent of authentication methods.

---

## 6.4 Stress Test D — Public, Unlisted, and Granular Tracking Privacy Across Service Boundaries

### Capabilities Under Test

- Shareable Profile.
- Unlisted Profile.
- Granular Profile Privacy.
- Public Lists.
- Favorites.
- List Comparison.
- Friends / Connections.

### Current Architectural Pressure

Identity owns profile identity and profile visibility.

Tracking owns:

- Library.
- Progress.
- Favorites.
- Statistics.
- Lists.

A public Shiori profile therefore composes data owned by more than one service.

This creates a dangerous architectural temptation:

```text
Profile is public
→ therefore all associated Tracking data is public
```

That implication is invalid.

### Future Scenario

A user configures:

```text
Profile: Unlisted
Statistics: Visible
Favorites: Visible
Recent Progress: Hidden
Public Lists: Visible
Country: Visible
```

The user shares the normal profile URL with a friend.

Later:

- The friend accepts a mutual connection.
- The two users compare libraries.
- The profile remains excluded from public discovery.
- Recent progress remains private.

### Failure Mode

The architecture fails if:

- `IsPublic = true` automatically exposes all Tracking-owned data.
- Identity directly copies the user's entire library to simplify profile reads.
- Tracking trusts a frontend to hide private fields.
- Being Friends automatically grants access to otherwise private data.
- `Unlisted` is treated as a secret-token authorization mechanism.
- List Comparison bypasses visibility because both users initiated the comparison.
- Country visibility implies other demographics are public.

### Required Architectural Property

Privacy must be enforced according to **data ownership**.

Conceptually:

```text
Identity
└── Profile identity / discoverability policy

Tracking
└── Visibility of Tracking-owned data
```

A composed public profile must expose only the intersection of:

1. What the profile allows.
2. What the owning service allows for the specific data.

### Unlisted Semantics

`Unlisted` means:

```text
Stable normal URL
+
not publicly discoverable
```

It does not mean:

```text
secret bearer link
```

### Pass Criteria

The test passes when:

- Profile discoverability can evolve beyond one binary boolean.
- Tracking does not expose private data merely because a profile is visible.
- Privacy rules are enforced server-side.
- Missing/ambiguous visibility rules fail safely.
- Friends / Connections do not bypass the owner's Tracking privacy.
- List Comparison uses only explicitly visible data.
- Country may be independently public if the user explicitly chooses it.
- Other optional demographics remain non-public under the current product direction.

### Current Horizon Result

**REQUIRES ARCHITECTURE DECISION BEFORE PUBLIC PROFILE CONTRACTS ARE FROZEN**

The current service ownership is correct, but composition and authorization semantics need to be made explicit.

### ADR Input

The public-profile/privacy architecture must define:

- Profile-level discoverability.
- Tracking-owned visibility.
- Cross-service composition.
- Default-deny behavior.
- The future extensibility path for granular privacy.

---

## 6.5 Stress Test E — One New Release, Many Users, No Catalog-to-Notification Coupling

### Capabilities Under Test

- Push Notifications.
- Release Intelligence.
- Existing Catalog publication-unit events.
- Tracking release-track preferences.
- RabbitMQ asynchronous integration.

### Current Architectural Pressure

Catalog owns verified release facts.

Tracking owns:

- Which work the user tracks.
- Which release track the user selected.
- Whether the work is in Manual Track Mode.

A future Notification capability needs information from both domains.

Catalog must not become responsible for user notification preferences.

Tracking must not become responsible for determining external release truth.

### Future Scenario

Catalog verifies:

```text
Work: X
Release Track: Official English
New Unit: Chapter 75
```

Users differ:

```text
User A → Official English
User B → Original Release
User C → Manual Track Mode
User D → Official English, notifications disabled
User E → Official English, notifications enabled
```

A future Notification capability should be able to notify only the appropriate users.

### Failure Mode

The architecture fails if:

- Catalog loops through users.
- Catalog calls Notification synchronously.
- Notification directly reads the Catalog database.
- Notification directly reads the Tracking database.
- Tracking calls Catalog synchronously for every notification decision.
- The only published fact is a generic `CatalogItemUpdated` message with insufficient semantics to identify the release change.
- Manual Track users receive inferred automated-release notifications.

### Required Architectural Property

Future consumers must be able to observe **semantic release facts**.

The event architecture should support facts such as:

```text
A verified publication unit became available
for a specific catalog item / release context
```

without Horizon defining the final event schema.

Tracking remains the source of truth for the user's selected release behavior.

A future Notification capability can then build whatever local projection it requires.

### Pass Criteria

The test passes when:

- Catalog publishes sufficient semantic release information.
- Catalog has no knowledge of Notification consumers.
- Tracking remains the owner of user release-track choice.
- A future Notification component can maintain its own data through supported contracts.
- Fan-out happens asynchronously.
- Manual Track behavior remains safe.
- Notification delivery failure cannot block Catalog ingestion or Tracking progress writes.

### Current Horizon Result

**CONDITIONALLY SAFE — EVENT CONTRACT DESIGN MUST PRESERVE SEMANTIC RELEASE FACTS**

RabbitMQ, Outbox/Inbox, and existing publication-unit events provide the correct foundation.

The remaining risk belongs to the semantics of the contracts defined later.

### ADR / Contract Input

Later event-contract work must verify that future release consumers do not have to infer important business meaning from a generic entity-update event.

---

## 6.6 Stress Test F — Three Different Language Preferences Without One Global `Language`

### Capabilities Under Test

- Extended Localization.
- Preferred Title Language.
- Preferred Release Language.
- Existing English/Spanish UI.
- Release Intelligence.

### Current Architectural Pressure

"Language" appears in several Shiori domains but does not mean the same thing in each one.

The product explicitly requires these concerns to remain separable:

```text
UI Language
Preferred Title Language
Preferred Release Language
```

### Future Scenario

A user configures:

```text
UI Language: Spanish
Preferred Title Language: Romaji
Preferred Release Language: English
```

Then the user changes only the UI language to English.

Expected result:

```text
UI Language: English
Preferred Title Language: Romaji
Preferred Release Language: English
```

The change must not alter:

- Title preference.
- Existing selected release tracks.
- Release Intelligence comparisons.

### Failure Mode

The architecture fails if:

- Identity stores one generic `language` value used everywhere.
- Changing UI language changes release-track semantics.
- Catalog assumes interface language determines title selection.
- Tracking assumes title language determines release language.
- APIs use one ambiguous language field for multiple domain meanings.

### Required Architectural Property

Language preferences must be owned by the domain that gives them meaning.

Conceptually:

```text
Experience / Identity Preferences
└── UI Language

Catalog presentation preference
└── Preferred Title Language

Tracking / Release preference
└── Preferred Release Language or per-work track choice
```

The exact persistence model is deferred.

### Pass Criteria

The test passes when:

- Each language dimension can change independently.
- Existing release-track selections remain stable when UI language changes.
- Catalog title presentation does not mutate Tracking state.
- Future languages can be added without redefining the meaning of one generic field.

### Current Horizon Result

**REQUIRES SEMANTIC OWNERSHIP TO BE DECIDED BEFORE USER-PREFERENCE CONTRACTS ARE FROZEN**

No extra localization implementation is required now.

### ADR Input

Identity/Tracking architecture must explicitly separate language concerns by ownership and meaning.

---

## 6.7 Stress Test G — Owning a Spanish Physical Volume While Reading a Different Release Track

### Capabilities Under Test

- Ownership Tracking.
- Licensing Availability.
- Existing Release Tracks.
- Publication Units.

### Current Architectural Pressure

The existing Catalog model has publication units and release tracks designed primarily around **consumption and availability**.

Future ownership is more precise.

A user may own a commercial edition that is not identical to the unit identity used to track reading progress.

### Future Scenario

A user:

- Follows the Japanese original release for Release Intelligence.
- Reads an official English digital edition.
- Physically owns Spanish volumes 1–8.
- Has not read every owned Spanish volume.

The system must represent all of those facts without contradiction.

### Failure Mode

The architecture fails if it permanently assumes:

```text
Progress Unit
=
Release Track Unit
=
Commercial Edition
=
Owned Item
```

Those concepts may overlap, but they are not guaranteed to be identical.

The architecture also fails if a speculative MVP field such as:

```text
owns = true
```

becomes the canonical ownership model and later prevents volume-, language-, edition-, or format-specific ownership.

### Required Architectural Property

Progress and ownership must remain distinct concepts.

Catalog must preserve the option to introduce future edition identity when reliable product requirements and metadata exist.

Tracking may later reference that identity for user ownership.

### Pass Criteria

The test passes when:

- Progress does not imply ownership.
- Ownership does not imply progress.
- A work can have multiple edition/language representations later.
- No existing Shiori work identifier must be replaced to add edition identity.
- Tracking does not invent commercial-edition metadata independently of Catalog.
- No speculative ownership subsystem is required in the MVP.

### Current Horizon Result

**SAFE IF THE MVP AVOIDS COLLAPSING OWNERSHIP INTO A WORK-LEVEL BOOLEAN**

The current architecture does not yet create that problem.

This is primarily a guardrail for later model design.

### ADR Input

A future ownership/edition ADR may be required before Phase 2 implementation, but no full edition model is required for MVP.

---

## 6.8 Stress Test H — Shiori-Curated Franchise Guidance Without Corrupting Provider Provenance

### Capabilities Under Test

- Curated Franchise Consumption Guides.
- Franchise Autopilot.
- Interactive Franchise Tree.
- Existing AniList/MangaDex Anti-Corruption Layer.

### Current Architectural Pressure

Catalog currently normalizes and derives much of its data from external providers.

Curated Consumption Guides introduce a different kind of data:

```text
Shiori-authored knowledge
```

That knowledge must not be confused with:

```text
AniList fact
MangaDex fact
provider-derived relationship
```

### Future Scenario

AniList provides:

```text
A → sequel → B
A → adaptation/source relationship → C
```

Shiori later curates two optional guides:

```text
Recommended Order
A → B → C

Anime-Only Route
A → B
```

The structured provider graph remains unchanged.

The guide represents Shiori's own editorial interpretation.

### Failure Mode

The architecture fails if:

- Curated order is written back into provider-cache fields as if it came from AniList.
- One guide overwrites the underlying relationship graph.
- Catalog is prohibited from owning any first-party knowledge because it was interpreted only as an external-provider cache.
- Clients cannot distinguish provider-derived facts from Shiori-curated guidance.
- Tracking becomes the owner of franchise editorial knowledge merely because guides may display user progress.

### Required Architectural Property

Catalog must be allowed to own multiple provenance classes:

```text
Provider-derived data
Shiori-derived canonical data
Shiori-curated guidance
```

They must remain distinguishable.

### Pass Criteria

The test passes when:

- Provider facts retain provenance.
- Curated guidance can be added without changing the raw relationship graph.
- Multiple guide types can coexist.
- Guide revisions can be introduced later without changing catalog-item identity.
- Tracking overlays remain optional read composition rather than Catalog ownership leakage.

### Current Horizon Result

**REQUIRES AN EXPLICIT CATALOG OWNERSHIP / PROVENANCE DECISION**

No guide collection or editorial workflow needs to exist in the MVP.

### ADR Input

Catalog architecture should explicitly permit clearly identified first-party Shiori knowledge while preserving provider provenance.

---

## 6.9 Composite Stress Test — A Future Shiori User Without a Core Rewrite

### Purpose

The previous tests isolate one architectural pressure at a time.

This scenario combines them to test whether the product horizon still fits the three-service architecture without turning Identity, Catalog, or Tracking into catch-all services.

### Future Scenario

A future user:

1. Owns one stable Shiori account.
2. Initially signs in with a local credential.
3. Later links Google.
4. Uses the interface in Spanish.
5. Prefers Romaji titles.
6. Follows English release tracks.
7. Has an Unlisted profile.
8. Shows statistics and favorites publicly.
9. Hides recent progress.
10. Has several mutual Friends / Connections.
11. Uses a read-only offline PWA while traveling.
12. Has completed the same Anime twice.
13. Is currently on a third watch.
14. Rated individual episodes differently during the second watch.
15. Owns a Spanish physical edition while following an English digital release.
16. Receives notifications only for new units on the selected supported release track.
17. Views an Annual Wrapped generated only from activity actually recorded by Shiori during that year.
18. Opens a curated franchise guide whose provenance is clearly Shiori-authored rather than AniList-authored.

### Required Service Ownership

The scenario should still decompose cleanly:

```text
Identity
├── Stable Shiori user
├── Authentication methods
├── Profile identity
├── Profile discoverability
└── Identity-owned preferences

Catalog
├── Canonical works
├── Franchise relationships
├── Publication / release metadata
├── Licensing metadata
├── Future edition metadata
└── Curated franchise knowledge with provenance

Tracking
├── User library relationship
├── Consumption runs
├── Current progress
├── Immutable history
├── Ratings
├── Favorites
├── Public/private Tracking views
└── Ownership state referencing Catalog identities

Future Capability
├── Notifications
├── Recommendations
└── Aggregate Analytics
```

No future capability is allowed to justify:

```text
Identity reading Tracking tables

Tracking reading MongoDB directly

Catalog reading Identity PostgreSQL

Notification reading operational service databases

Gateway becoming a business workflow orchestrator
```

### Failure Mode

The architecture fails the composite test if future growth requires:

- Replacing canonical Shiori identifiers.
- Sharing databases.
- Moving the user's library into Identity for profile convenience.
- Turning Catalog into a user-preference service.
- Turning Tracking into an external-provider integration layer.
- Making the Gateway compose business transactions.
- Rewriting historical progress because repeat consumption was not anticipated.
- Weakening privacy because profile data spans multiple services.
- Introducing synchronous cross-service calls into critical Tracking writes.

### Pass Criteria

The composite test passes when all of the following remain possible through additive evolution:

- New Identity credential/link models.
- New Tracking run/history models.
- New privacy policies.
- New Catalog metadata collections.
- New event consumers.
- New client capabilities.
- New future bounded contexts when justified.

while preserving:

- Database-per-Service.
- Stable Shiori identifiers.
- Tracker-First product boundaries.
- Local ownership of business rules.
- Eventual consistency where already accepted.
- No synchronous Catalog dependency in Tracking's progress write path.
- No speculative future microservices.

### Current Horizon Result

**CONDITIONAL PASS**

The three-service macro-architecture remains compatible with the known product horizon.

The stress test does **not** reveal a need to:

- Add another MVP microservice.
- Replace PostgreSQL or MongoDB.
- Replace RabbitMQ.
- Abandon YARP.
- Merge service databases.

However, the current architecture cannot yet be frozen because several internal domain and contract decisions remain unresolved.

---

## 6.10 Stress-Test Findings

The tests identify four categories of architectural findings.

### Category A — Architecture Freeze Blockers

These must be resolved before the affected MVP component is considered architecturally frozen.

#### A1. Tracking Relationship vs. Consumption Run

The architecture must explicitly separate or make separable:

```text
User-to-Work Library Relationship
≠
Consumption Run
```

This is required by:

- Rewatch / Reread.
- Granular Scoring.
- Full Progress Timeline.
- Historical integrity.

---

#### A2. Tracking History Contract

`progress_history` must have an explicit semantic contract capable of supporting:

- Progress transitions.
- Status transitions.
- Timestamp semantics.
- Required mutation source/client context.
- Future run association.
- Progress Vault without historical erasure.

The exact persistence implementation remains open.

---

#### A3. Canonical Identity vs. Authentication Method

Identity must guarantee:

```text
Shiori User
≠
Credential
≠
External Identity
```

before account persistence becomes difficult to change.

---

#### A4. Public Profile / Tracking Privacy Composition

The architecture must define how:

```text
Identity-owned profile visibility
```

and:

```text
Tracking-owned data visibility
```

compose safely.

A single global `IsPublic` assumption is not sufficient as a permanent architecture.

---

### Category B — Pre-Freeze Contract Guardrails

These require explicit constraints during upcoming architecture work but do not require the future feature itself.

#### B1. Semantic Release Events

Event-contract design must preserve meaningful release facts for future consumers such as Notifications.

---

#### B2. Independent Language Semantics

UI, title, and release language must not collapse into one generic cross-domain preference.

---

#### B3. Ownership Is Not Progress

The MVP must not introduce a permanent shortcut equating ownership with:

- Library membership.
- Reading progress.
- One work-level boolean.

---

#### B4. Catalog Provenance Classes

Catalog must preserve the ability to distinguish:

- Provider-derived facts.
- Shiori-derived canonical data.
- Future Shiori-curated guidance.

---

### Category C — Safe Additive Future Evolution

The stress tests confirm that the current architecture does not require special MVP construction for:

- Friends / Connections.
- List Comparison.
- Personalized Recommendations.
- Read-only offline PWA.
- Home Screen Widget.
- Full Cast.
- Illustrator Gallery.
- Custom Lists.
- Search Autocomplete.
- Licensing Availability.

Their eventual implementation may still be substantial.

The important result is that no irreversible MVP dependency has been identified.

---

### Category D — Intentionally Unresolved

The following must remain unresolved rather than guessed:

- Aggregate Product Analytics.
- Per-Work Discussion.

Their product definitions are not mature enough to justify architecture.

---

## 6.11 Architecture Changes Explicitly Not Required by Horizon

The stress tests do **not** justify any of the following before MVP:

```text
Notification Service
Recommendation Service
Analytics Service
Social Service
Engagement Service
Edition Service
Discussion Service
```

They also do not justify:

- A shared database.
- Kafka.
- Event sourcing.
- A graph database.
- A second API Gateway.
- A BFF solely for hypothetical future screens.
- Pre-creating demographic tables.
- Pre-creating custom-list tables.
- Pre-creating consumption-guide collections.
- Pre-creating Google/Apple integrations.

If any of those become justified later, they require their own product and architecture decision.

---

## 6.12 Section Conclusion

The High-Risk Stress Tests support the current Shiori macro-architecture.

No known future capability forces a replacement of the current three-service structure.

The main risk is not the number of services.

The main risk is **prematurely collapsing distinct domain concepts inside those services**.

The architecture remains healthy if the next design stage explicitly protects these boundaries:

```text
Library Relationship
≠
Consumption Run

Current Tracking State
≠
Immutable History

Shiori User
≠
Authentication Method

Profile Discoverability
≠
Visibility of All Tracking Data

UI Language
≠
Title Language
≠
Release Language

Progress Unit
≠
Commercial Edition / Ownership

Provider Fact
≠
Shiori-Curated Knowledge
```

These findings become direct inputs to the remaining Product Horizon sections and, after Horizon is approved, to the architecture decisions that precede Architecture Freeze v1.0.

# 7. Horizon Conclusions

## 7.0 Purpose

The Product Horizon exercise is now complete enough to answer its original question:

> **Does the known future product direction require Shiori to redesign the MVP architecture before implementation begins?**

The answer is:

> **No macro-architecture replacement is required.**
>
> The accepted Identity + Catalog + Tracking service structure remains compatible with the known product horizon.
>
> However, several internal domain boundaries and contract semantics must be resolved before Architecture v1.0 can be frozen.

The main value of Horizon is therefore not the discovery of new services.

It is the discovery of **concepts that must not be collapsed together too early**.

---

## 7.1 Macro-Architecture Conclusion

The current three-service architecture remains valid:

```text
Identity
Catalog
Tracking
```

No known approved or credible future capability currently requires Shiori to:

- Merge the services.
- Add a fourth MVP microservice.
- Replace PostgreSQL.
- Replace MongoDB.
- Replace RabbitMQ.
- Replace YARP.
- Introduce Kafka.
- Introduce Event Sourcing.
- Introduce a graph database.
- Share databases between services.
- Move business logic into the API Gateway.

The macro-architecture therefore receives a:

```text
HORIZON RESULT:
PASS — WITH INTERNAL ARCHITECTURE CONDITIONS
```

Those conditions are listed below.

---

## 7.2 SAFE — No Special MVP Preparation Required

The following capabilities fit the current architecture through normal additive evolution.

They do not require speculative MVP implementation.

### MVP Candidates

- Favorites.
- Search Autocomplete.

### Phase 2 Approved

- Franchise Autopilot.
- Interactive Franchise Tree.
- Custom Lists.
- Personalized Recommendations.
- List Comparison.
- Friends / Connections.
- Installable PWA with Read-Only Offline Mode.
- Home Screen Widget.
- Licensing Availability.
- Illustrator Gallery.
- Full Cast Directory.

### Future Candidates

- Optional Demographics for Aggregate Analytics.

`SAFE` means:

> No irreversible architectural obstacle has been identified.

It does **not** mean:

> The feature will be trivial to implement.

Some of these capabilities may still require:

- New tables.
- New collections.
- New read models.
- New client storage.
- New background processing.
- New external providers.
- New authorization rules.

Those costs can safely be paid when the feature becomes real.

---

## 7.3 PREPARE NOW — Architecture Must Preserve a Future Path

The following capabilities do not belong in MVP implementation, but Horizon found that the architecture must preserve or decide something before the affected component is frozen.

### Unlisted Profile

The profile model must not permanently assume only:

```text
Public
Private
```

Discoverability must remain extensible.

---

### Annual Wrapped

Tracking must preserve first-party historical activity sufficiently to support a future year-in-review without pretending imported history was observed by Shiori at the time.

---

### Deep Statistics / Personal Analytics

Tracking history must preserve meaningful temporal facts instead of relying only on current state.

---

### Push Notifications

Future consumers must be able to observe semantic release facts without requiring Catalog to know who the consumers are.

---

### Full Progress Timeline

Tracking history must preserve enough context to reconstruct:

- Progress transitions.
- Status transitions.
- Timestamps.
- Required source/client context.
- Future consumption-run association.

---

### Granular Scoring

Future per-episode and per-chapter ratings must be able to belong to a specific consumption run.

---

### Rewatch & Reread Tracking

Tracking must not permanently collapse:

```text
User-to-Work Relationship
=
One Consumption Run
```

These concepts must remain separable.

---

### Ownership Tracking

The architecture must not permanently equate:

```text
Work
=
Publication Unit
=
Commercial Edition
=
Owned Item
```

Ownership must remain conceptually independent from progress.

---

### Extended Localization

Shiori must keep these concerns distinct:

```text
UI Language
Preferred Title Language
Preferred Release Language
```

---

### Curated Franchise Consumption Guides

Catalog must preserve the ability to distinguish:

```text
Provider-Derived Data
Shiori-Derived Canonical Data
Shiori-Curated Guidance
```

---

### External Authentication Providers

Identity must preserve:

```text
Shiori User
≠
Credential
≠
External Provider Identity
```

---

### Granular Profile Privacy

Public-profile architecture must not rely permanently on one global visibility flag.

Identity-owned profile visibility and Tracking-owned data visibility must compose safely.

---

## 7.4 NEEDS PRODUCT DECISION — Do Not Guess the Architecture

Two capabilities remain intentionally unresolved.

### Aggregate Product Analytics

The product questions are not defined enough to justify:

- An analytical warehouse.
- Extra event pipelines.
- Additional telemetry.
- Demographic collection.
- Cross-service analytical projections.

The correct architectural action today is:

```text
DO NOT PRE-BUILD
```

A future analytics architecture begins only after concrete analytical questions, privacy requirements, retention requirements, and aggregation rules exist.

---

### Per-Work Discussion

The feature no longer fits cleanly inside the clarified Tracker-First direction.

It remains:

```text
NEEDS PRODUCT REVIEW
```

Shiori should not build:

- Comment storage.
- Moderation infrastructure.
- Reporting systems.
- Discussion feeds.
- User-generated-content services.

unless the product direction explicitly re-approves the capability.

---

## 7.5 Rejected / Not Planned Capabilities Require No Architectural Preparation

The architecture must not spend complexity preparing for:

- Streaks.
- XP.
- Levels.
- Invite-only registration.
- Global activity feed.
- Chat / messaging.
- General-purpose posts.
- Likes on user activity.
- Influencer / follower mechanics.
- A gamification-focused Engagement subsystem.

These remain:

```text
Rejected / Not Planned Under Current Product Direction
```

Reconsideration requires a new product decision.

---

## 7.6 Core Domain Separations Discovered by Horizon

The most important output of Product Horizon is the following set of conceptual separations.

These are now considered architecture guardrails.

### Tracking

```text
Library Relationship
≠
Consumption Run

Current State
≠
Immutable Historical Record

Overall Work Rating
≠
Per-Run / Per-Unit Rating

Progress
≠
Ownership

Recorded Tracking Activity
≠
Verified Real-World Consumption
```

---

### Identity

```text
Shiori User
≠
Credential
≠
External Identity

Profile Identity
≠
Tracking Data

Profile Discoverability
≠
Visibility of Every Tracking Field

Core Account Data
≠
Optional Demographic Data
```

---

### Catalog

```text
Relationship Graph
≠
Guaranteed Consumption Order

Provider-Derived Fact
≠
Shiori-Curated Knowledge

Publication Progress Unit
≠
Commercial Edition

Official Availability
≠
One Global Language / Region Boolean
```

---

### Cross-Cutting

```text
UI Language
≠
Preferred Title Language
≠
Preferred Release Language

Public Profile
≠
Permission to Read All User Data

Future Capability
≠
Reason to Pre-Build a New Service
```

These separations are more important than guessing future table structures.

---

## 7.7 Architecture Freeze Readiness

Product Horizon does **not** yet authorize Architecture Freeze v1.0 by itself.

It confirms that the macro-architecture is viable, but identifies specific architecture decisions that must be resolved first.

The next architecture stage must close the decisions listed in Section 8.

Once those decisions are accepted and the remaining architecture documents are completed, Product Horizon no longer blocks implementation.

---

# 8. Inputs to ADR / Step 2

## 8.0 Purpose

This section converts Horizon findings into a bounded list of architecture work.

It prevents Step 2 from becoming an open-ended redesign exercise.

Only decisions supported by the Product Horizon analysis should enter the immediate ADR/design stage.

A future feature being interesting is not enough.

The decision must protect MVP architecture from a credible future dead end.

---

## 8.1 Required Before Architecture Freeze

The following decisions are direct outputs of Horizon and must be resolved before the affected MVP design is considered stable.

---

## 8.1.1 Internal Microservice Architecture

### Why It Is Required

The macro service boundaries are already accepted, but each service still needs a consistent internal architecture that prevents infrastructure concerns from leaking into domain logic.

### Required Decision

Define the internal architecture of Identity, Catalog, and Tracking.

Expected direction:

```text
API
↓
Application
↓
Domain

Infrastructure
→ implements inward-facing contracts
```

Application may organize use cases through Vertical Slices.

### Horizon Capabilities Protected

All future capabilities benefit from this, especially:

- Rewatch/Reread.
- Privacy evolution.
- Authentication evolution.
- Catalog curation.
- Personal Analytics.

### Expected ADR

```text
ADR-012 — Internal Microservice Architecture
```

---

## 8.1.2 Tracking Relationship and Consumption-Run Model

### Why It Is Required

Rewatch/Reread showed that the current concept of one active `tracking_entry` per user/catalog item may eventually mix:

- Library membership.
- Current state.
- One consumption experience.

### Required Decision

Define how the Tracking domain distinguishes or keeps separable:

```text
Persistent User-to-Work Relationship
Current Tracking State
Consumption Run
```

The ADR/design must decide whether the MVP physically separates these concepts immediately or preserves an additive migration path without ambiguity.

### Horizon Capabilities Protected

- Rewatch & Reread.
- Granular Scoring.
- Full Progress Timeline.
- Annual Wrapped.
- Personal Analytics.

### Important Constraint

Horizon does not mandate:

- `cycle_number`.
- A `consumption_runs` table.
- Multiple active runs.

The architecture stage must choose the simplest model that preserves future safety.

### Expected ADR

This may belong inside:

```text
ADR-013 — Tracking Lifecycle, Consumption Runs & History
```

rather than creating many tiny ADRs.

---

## 8.1.3 Tracking History / Audit Contract

### Why It Is Required

`progress_history` cannot be treated only as an implementation detail for Undo.

It is also the historical foundation for future:

- Full Timeline.
- Wrapped.
- Personal Analytics.
- Rewatch history.

### Required Decision

Define:

- What mutations create historical records.
- Previous-state semantics.
- Resulting-state semantics.
- Status-transition capture.
- Timestamp semantics.
- Mutation-source semantics.
- Client/device context where required.
- Import-origin distinction where required.
- Relationship to future consumption runs.
- Relationship between immutable history and Progress Vault undo.
- Retention expectations.

### Technical Question That Must Be Resolved

The current trigger-based design must be evaluated against application-level context.

If a database trigger cannot safely receive all required context, the architecture must define a mechanism that retains the guarantee that no supported write path can bypass history.

### Expected ADR

Prefer combining this with consumption-run design:

```text
ADR-013 — Tracking Lifecycle, Consumption Runs & History
```

---

## 8.1.4 Identity Model and Authentication Extensibility

### Why It Is Required

Future external authentication must not force changes to canonical user identity after Tracking already references users.

### Required Decision

Guarantee:

```text
Canonical Shiori User
≠
Credential
≠
External Identity
```

The design should support future linked authentication methods without implementing Google or Apple now.

### Horizon Capabilities Protected

- External Authentication Providers.
- Account recovery.
- Future client evolution.
- All downstream user-owned data.

### Expected ADR

This can be part of a broader authentication/client architecture decision:

```text
ADR-014 — Identity, Client Authentication & External Login Extensibility
```

---

## 8.1.5 Web / PWA / Future Mobile Authentication Model

### Why It Is Required

Authentication must remain secure across:

- Web.
- Future installable PWA.
- Future native mobile clients.

The architecture needs a concrete policy before Identity implementation begins.

### Required Decision

Define at architecture level:

- Authorization Code + PKCE usage.
- Public/confidential client assumptions.
- Refresh-token model.
- Revocation.
- Token storage expectations by client class.
- Cookie usage if applicable.
- CSRF implications if cookies are introduced.
- CORS policy.
- Logout semantics.
- Account/session revocation expectations.

### Important Boundary

This ADR prepares secure client evolution.

It does not implement Phase 2 offline PWA behavior.

### Expected ADR

Can be combined with Identity extensibility:

```text
ADR-014 — Identity, Client Authentication & External Login Extensibility
```

---

## 8.1.6 Public Profile and Privacy Composition

### Why It Is Required

Identity owns profile identity.

Tracking owns the data users may choose to share.

A public-profile architecture must therefore cross service boundaries safely.

### Required Decision

Define architecture for:

- Profile discoverability.
- Public / future Unlisted semantics.
- Public-list exposure.
- Tracking-owned visibility.
- Safe composition.
- Default-deny behavior.
- Future granular privacy extension.
- Whether profile composition occurs through Gateway/BFF/read model/service calls.

### Horizon Capabilities Protected

- Shareable Profile.
- Unlisted Profile.
- Granular Profile Privacy.
- Favorites.
- Friends / Connections.
- List Comparison.
- Optional country visibility.

### Expected ADR

```text
ADR-015 — Public Profile Composition & Privacy Boundaries
```

The final numbering may change depending on ADR organization.

---

## 8.1.7 Event Semantics and Compatibility

### Why It Is Required

RabbitMQ is already accepted.

The remaining Horizon concern is not the broker.

It is whether events preserve meaningful business facts for future consumers.

### Required Decision

Define:

- Versioned event envelope.
- Compatibility policy.
- Producer/consumer evolution rules.
- Semantic event naming.
- When an event represents an entity mutation versus a business fact.
- Required metadata.
- Idempotency expectations.
- Ordering/version expectations.

### Horizon Capability Protected

Push Notifications is the clearest future consumer.

Existing Catalog → Tracking projections also depend on this discipline.

### Important Constraint

Do not invent events for every future feature.

Only current business facts should be published.

The contracts must merely remain semantically strong enough to support additional consumers.

### Architecture Document

This may be better represented in:

```text
EVENT_CONTRACTS.md
```

plus an ADR if a new architectural decision is required.

---

## 8.1.8 Language Preference Ownership

### Why It Is Required

Shiori currently uses "language" in multiple domains.

Those meanings must not collapse into one global setting.

### Required Decision

Define ownership for:

```text
UI Language
Preferred Title Language
Preferred Release Language
Per-Work Release Track
```

The architecture should state which are:

- User-level defaults.
- Per-work overrides.
- Catalog presentation concerns.
- Tracking concerns.

### Horizon Capability Protected

Extended Localization.

### ADR Requirement

This may fit inside an Identity/preferences or API-contract ADR rather than requiring a standalone ADR.

---

## 8.1.9 Catalog Provenance Model

### Why It Is Required

Future Curated Consumption Guides introduce Shiori-authored knowledge.

Catalog must remain able to distinguish that from provider-derived facts.

### Required Decision

At architecture level, define the conceptual provenance classes:

```text
Provider-Derived
Shiori-Derived
Shiori-Curated
```

The implementation does not need guide collections now.

### Horizon Capabilities Protected

- Curated Franchise Consumption Guides.
- Franchise Autopilot.
- Provider reconciliation.
- Future catalog editorial data.

### ADR Requirement

Can be included in an architecture-evolution ADR or Catalog-specific design decision.

---

## 8.1.10 Ownership / Edition Guardrail

### Why It Is Required

Ownership may eventually refer to:

- Physical/digital form.
- Edition.
- Language.
- Specific volumes.

The MVP should not introduce an oversimplified ownership field that later becomes permanent.

### Required Decision

No full ownership model is needed now.

The architecture must simply record:

```text
Progress Unit
≠
Commercial Edition
```

and avoid treating a work-level `owns` boolean as a canonical future model.

### Horizon Capability Protected

Ownership Tracking.

### ADR Requirement

No standalone ADR is required immediately unless current MVP design unexpectedly introduces edition/ownership concepts.

This remains a design guardrail.

---

## 8.2 Decisions That Do NOT Need to Be Made Now

The following should remain deferred.

### Notification Service

Do not decide:

- Service topology.
- Push provider.
- Device-token table.
- Delivery workers.

until Push Notifications enter implementation.

---

### Recommendation Architecture

Do not decide:

- Recommendation algorithm.
- ML stack.
- Dedicated Recommendation Service.
- Vector database.

until Phase 2 requirements exist.

---

### Aggregate Analytics Architecture

Do not decide:

- Warehouse.
- OLAP database.
- Stream-processing platform.
- Demographic event model.

until concrete product questions exist.

---

### Discussion Architecture

Do not decide anything while the feature remains `Needs Product Review`.

---

### Full Ownership / Edition Schema

Do not build a complete commercial-edition model before Ownership Tracking becomes real.

---

### Curated Guide Storage

Do not create guide collections or editorial tooling now.

Only provenance boundaries need to remain possible.

---

## 8.3 Proposed Immediate ADR / Architecture Workset

Horizon recommends that Step 2 remain intentionally small.

A reasonable initial architecture workset is:

```text
ADR-012 — Internal Microservice Architecture

ADR-013 — Tracking Lifecycle, Consumption Runs & History

ADR-014 — Identity, Client Authentication & External Login Extensibility

ADR-015 — Public Profile Composition & Privacy Boundaries
```

Additional cross-cutting architecture should be documented through:

```text
SYSTEM_DESIGN.md
API_CONVENTIONS.md
EVENT_CONTRACTS.md
```

rather than creating an ADR for every field or future feature.

Catalog provenance and language ownership should be incorporated into the most appropriate ADR/system-design section unless deeper analysis proves they deserve standalone ADRs.

The objective is:

> Record consequential decisions, not maximize ADR count.

---

## 8.4 Architecture Work That Follows Step 2

After the immediate ADR decisions are accepted, the remaining pre-implementation architecture flow should continue with:

```text
SYSTEM_DESIGN.md
↓
API_CONVENTIONS.md
↓
EVENT_CONTRACTS.md
↓
Non-Functional Requirements
↓
Backend-oriented Web UX constraints
↓
Architecture Freeze v1.0
```

Product Horizon does not perform those tasks.

It only defines what they must protect.

---

## 8.5 Step 2 Entry Criteria

Step 2 may begin when:

```text
[x] Known Phase 2 capabilities have been inventoried
[x] New Horizon candidates have been classified
[x] Rejected capabilities are documented
[x] Architecture risk has been evaluated
[x] Historical-data risk has been evaluated
[x] Migration risk has been evaluated
[x] High-risk scenarios have been stress-tested
[x] Architecture preparation requirements are identified
[x] Product-undefined capabilities are isolated from architecture work
```

At that point, ADR work can proceed without reopening the entire product horizon.

---

# 9. Step 1 Definition of Done

## 9.0 Purpose

Step 1 is complete only when `PRODUCT_HORIZON.md` provides enough evidence to begin architecture decisions without silently expanding MVP scope.

The document does not need to predict every future feature Shiori will ever have.

It must cover the future capabilities currently known well enough to identify credible architecture risks.

---

## 9.1 Product Horizon Completion Checklist

### Document Scope

```text
[x] Purpose of PRODUCT_HORIZON.md defined
[x] Boundaries against FEATURES.md defined
[x] Boundaries against ROADMAP.md defined
[x] Boundaries against ADR.md defined
[x] Tracker-First philosophy defined
[x] Data-minimization principle defined
[x] No-prebuilding rule defined
[x] Additive-evolution principle defined
```

---

### Product Classification

```text
[x] Current MVP baseline identified
[x] MVP Candidates identified
[x] Phase 2 Approved capabilities inventoried
[x] Future Candidates inventoried
[x] Needs Product Review capabilities identified
[x] Rejected / Not Planned capabilities documented
```

---

### Architecture Analysis

```text
[x] Likely owner identified for each Horizon capability
[x] Historical Data Dependency evaluated
[x] Backfill Capability evaluated
[x] Cross-service impact evaluated
[x] Event / integration impact evaluated
[x] Privacy / security impact evaluated
[x] Scale / performance impact evaluated
[x] Architecture Risk evaluated
[x] Migration Risk If Ignored evaluated
[x] Prepare Now decision recorded
[x] ADR Required decision recorded
[x] Horizon conclusion recorded
```

---

### Stress Testing

```text
[x] Rewatch / Reread stress-tested
[x] Tracking history stress-tested
[x] External authentication stress-tested
[x] Public-profile privacy stress-tested
[x] Push-notification event flow stress-tested
[x] Language ownership stress-tested
[x] Ownership / edition separation stress-tested
[x] Catalog curation / provenance stress-tested
[x] Composite future-user scenario stress-tested
```

---

### Architecture Outputs

```text
[x] SAFE capabilities identified
[x] PREPARE NOW capabilities identified
[x] NEEDS PRODUCT DECISION capabilities identified
[x] Architecture Freeze blockers identified
[x] Contract guardrails identified
[x] Immediate ADR inputs identified
[x] Architecture work explicitly not required identified
```

---

## 9.2 Remaining Synchronization Before Final Approval

Before `PRODUCT_HORIZON.md` changes from Draft to Approved, the assembled final document must receive one consistency pass.

That pass must verify:

### Section 3 Synchronization

`Aggregate Product Analytics` is now explicitly included in Section 3.5 as a distinct Future Candidate.

The Product Classification section is internally synchronized with its dedicated Horizon fiche and the Architecture Impact Matrix.

---

### Phase 2 Synchronization

The final document must reflect the product decisions made during Horizon review:

```text
Friends / Connections
→ Phase 2 Approved

Installable PWA with Read-Only Offline Mode
→ Phase 2 Approved

Per-Work Discussion
→ Needs Product Review
```

If `FEATURES.md` still contains older classifications, that mismatch must be resolved explicitly after Horizon approval.

Horizon must not silently pretend the authoritative product specification has already changed.

---

### MVP Candidate Synchronization

The following remain candidates, not approved MVP scope:

```text
Favorites
Search Autocomplete
Unlisted Profile
```

They must not enter the implementation roadmap unless `FEATURES.md` is explicitly updated.

---

### Terminology Consistency

The assembled document consistently distinguishes:

```text
Rewatch & Reread
Consumption Run
Deep Statistics / Personal Analytics
Aggregate Product Analytics
Unlisted Profile
Granular Profile Privacy
Installable PWA with Read-Only Offline Mode
Curated Franchise Consumption Guides
```

without silently collapsing distinct capabilities.

---

## 9.3 Final Product Horizon Status

After the synchronization pass above, Step 1 can be marked:

```text
STEP 1 — PRODUCT HORIZON

[x] Scope defined
[x] MVP separated from future horizon
[x] Phase 2 inventoried
[x] New candidates inventoried
[x] Rejected capabilities recorded
[x] Product classifications assigned
[x] Likely owners identified
[x] Architecture risk evaluated
[x] Migration risk evaluated
[x] Historical-data risk evaluated
[x] Prepare Now decisions assigned
[x] Implement Now boundaries preserved
[x] High-risk capabilities stress-tested
[x] Architecture Impact Matrix completed
[x] Architecture Freeze blockers identified
[x] ADR inputs identified
[x] Undefined product capabilities isolated
[x] Final assembled-document consistency pass
[ ] PRODUCT_HORIZON.md approved
```

The assembled-document consistency pass is complete.

Final approval remains open until the authoritative product documents are synchronized with the Horizon decisions and the final review is accepted.

---

## 9.4 Exit Criteria for Step 1

Step 1 is ready to close when the final assembled `PRODUCT_HORIZON.md` demonstrates all of the following:

1. No known future capability has been allowed to expand MVP scope silently.
2. No significant known future capability remains unanalyzed.
3. Future capabilities that can be added safely later are not being pre-built.
4. Future capabilities that create irreversible architectural risk have explicit preparation requirements.
5. Product-undefined capabilities do not generate speculative architecture.
6. The current three-service macro-architecture survives the known future horizon.
7. The exact architecture decisions required before Architecture Freeze are bounded and listed.
8. The project can move into ADR work without reopening broad product discovery.

When those conditions are met:

```text
PRODUCT HORIZON
        ↓
APPROVED
        ↓
STEP 2 — ARCHITECTURE DECISIONS
```

---

## 9.5 Final Step 1 Conclusion

The Product Horizon exercise does not indicate that Shiori needs a larger MVP architecture.

It indicates that Shiori needs a **more precise internal architecture**.

The known future product direction can remain compatible with the current system as long as the design preserves the distinctions identified throughout this document.

The most important preparation is therefore not additional infrastructure.

It is protecting the meaning of the domain:

```text
Do not merge concepts today
that the product is likely to need separately tomorrow.
```

With that principle enforced through the ADR and system-design work that follows, Shiori can move toward Architecture Freeze v1.0 with a substantially lower risk of destructive redesign.
