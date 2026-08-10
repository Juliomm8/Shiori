# Shiori — Product Feature Specification

**Status:** Approved  
**Last updated:** July 2026  
**Scope:** What Shiori is meant to do as a product. Architecture decisions live in `ADR.md`.

---

## 1. Product summary

Shiori is a unified tracker for anime, manga, light novels, manhwa, movies, and related works.

The problem it tries to solve is simple: one person's entertainment history often ends up scattered across streaming apps, manga readers, spreadsheets, old trackers, and memory.

Shiori brings that history into one place and treats progress as something worth preserving carefully.

The product promise is:

> **Never lose your progress, always know what is available now, and understand how each franchise connects.**

That promise shapes the MVP more than the number of features does.

Phase 1 focuses on four things:

- preserving progress accurately
- making connected works easier to understand
- helping users continue without pressure
- keeping the user in control of their own data

---

# 2. Phase 1 — MVP

Phase 1 is the feature set required for Shiori to feel like a complete standalone product rather than a technical demo.

---

## 2.1 Catalog and discovery

### Catalog item pages

Every supported work has its own page.

The page can include:

- cover art
- synopsis
- original title
- alternative titles
- format
- publication/airing information
- other approved Catalog metadata

Users can choose their preferred title language.

Romaji is the default title preference.

---

### Franchise relationships

Each work can show verified relationships such as:

- adaptation
- source
- sequel
- prequel
- side story
- spin-off
- alternative version

The goal is to help someone understand how works belong together without leaving Shiori.

Phase 1 presents these relationships as factual connections.

It does **not** promise that every franchise has one correct watch/read order.

---

### Official consumption links

When Shiori has a verified link, it can show official places where a work is available to watch or read.

Examples may include platforms such as:

- Crunchyroll
- Netflix
- MANGA Plus

Availability can vary by region.

A missing link does not mean no legal release exists. It only means Shiori does not currently have a verified link it can safely show.

---

### Trailers

Anime entries may show an embedded trailer when Shiori has a verified trailer available.

---

### Character preview

A Catalog item shows a bounded preview of up to 10 main characters.

The MVP does not try to expose a complete cast database.

Full cast browsing and language-specific voice acting credits belong to later scope.

---

### Trending and Seasonal

The Home/Discovery experience can surface:

- currently popular works
- works airing, publishing, or releasing in the current season

These are discovery surfaces, not text-search tricks.

---

### Work-focused Search

Global Search finds entertainment works.

It does not search users or profiles.

Shiori is a tracker first, and I do not want user discovery to quietly become the center of the product.

---

### Appearance and interface language

Phase 1 supports:

- dark theme
- light theme
- system theme

Interface languages:

- English
- Spanish

English is the default.

Interface language remains separate from future title/release-language preferences.

---

## 2.2 Account, library, and personalization

### Account access

Users can:

- create an account
- sign in
- sign out
- refresh/revoke sessions
- recover account access

Authentication should behave consistently across web and future mobile clients.

---

### List privacy

Lists are private by default.

A user can choose to make an individual list public.

Public sharing should always be explicit rather than something a user has to opt out of later.

---

### Shareable profile

Shiori supports a read-only shareable profile centered on tracking.

A public profile can show tracking information that the user has deliberately made eligible for public exposure.

The profile is not meant to become the foundation for:

- follower counts
- a global activity feed
- chat
- posts
- social engagement mechanics

The useful part is being able to share selected tracking history, not building another general-purpose social network.

---

### Watchlists and read-lists

Shiori provides dedicated places for works the user plans to watch or read.

---

### Library Status

Every tracked work has one user-controlled status:

```text
Planned
In Progress
Paused
Completed
Dropped
```

These describe the user's relationship with the work.

They stay separate from release-relative states such as `Up to Date`.

---

### Consumption dates

Users can record dates such as:

- started
- completed
- paused

These are user-declared calendar dates, not automatically inferred from system timestamps.

---

### Scoring

Users can give a tracked work one overall rating from 1 to 5 stars.

Phase 1 rating is work-level.

Per-run and per-unit scoring, if added later, remain separate concepts.

---

### Core statistics

Shiori provides useful aggregate totals across the user's library.

Examples:

- estimated hours watched
- recorded pages read

Shiori only calculates these when enough trustworthy data exists.

It does not fill missing duration/page data with invented estimates just so every statistic has a number.

---

# 2.3 Smart Staging Import

A user with years of history should not have to rebuild everything manually.

Shiori supports list imports from:

- MyAnimeList exports
- AniList-compatible sources

The user-visible flow is intentionally simple:

```text
Upload
-> Preview
-> Confirm
```

Behind that simple flow, processing is asynchronous and durable.

---

## Upload

Uploading creates an import job and returns without keeping the original API request open for the entire workflow.

Shiori then:

- validates the file
- parses it
- matches entries
- resolves supported Catalog references
- records staging results

The user's live library is not changed during Upload.

---

## Preview

Preview shows what Shiori understood before anything is committed.

It can include:

- matched works
- unmatched works
- ambiguous matches
- invalid/unsupported progress
- proposed conflict resolutions
- entries still being resolved

The user can review entries individually or apply a bulk rule where that rule is safe for compatible entries.

For example:

> Keep the highest valid progress value across matching records.

Closing Preview or cancelling before confirmation leaves the live library unchanged.

---

## Confirm

Confirm authorizes Shiori to apply the staged result.

The commit runs in the background and is idempotent.

Retries must not create duplicate library entries.

The user can come back later and read the durable import status instead of needing to keep the page open.

The same workflow is used for small and large imports.

A 10-item import and a 4,000-item import have the same safety guarantees even though the processing time will differ.

---

# 2.4 Quick Start

A new user who has nothing to import should not be forced to stare at an empty product.

Quick Start lets them choose up to five familiar works and mark each as:

```text
Planned
In Progress
Completed
```

Behavior:

- `In Progress` -> appears in Continue
- `Planned` -> appears in the relevant watch/read list
- `Completed` -> enters the library as completed

Quick Start can be skipped.

---

# 2.5 Polymorphic tracking

Shiori does not pretend every kind of entertainment has the same progress model.

### Audiovisual

For Anime and other audiovisual works, progress can include:

- episode
- playback position

### Reading

For Manga, Manhwa, and Light Novels, progress can include:

- volume
- chapter
- page

Reading labels are allowed to look like real publication labels rather than being forced into simple integers.

Examples:

```text
0
10.5
Extra
One-shot
Special
named interludes
```

These are normal supported progress positions.

---

## Library Status vs Up to Date

Each tracking item still has one user-controlled Library Status:

```text
Planned
In Progress
Paused
Completed
Dropped
```

For an ongoing work using a supported automated release track, Shiori may also derive:

```text
Up to Date
```

when the user's recorded progress matches the latest verified release for that selected track.

These meanings are different:

```text
Completed
    -> user's library status

Up to Date
    -> derived release-relative state
```

An ongoing work can be:

```text
In Progress + Up to Date
```

without being finished.

Manual Track does not calculate `Up to Date`.

---

# 2.6 Release Intelligence

Release Intelligence answers:

> **Where does my recorded progress stand relative to the release track I chose?**

Shiori only answers that when it has structured, verified release data.

Precision matters more than pretending to support every edition.

---

## Automated release tracks

Current approved automated tracks:

- **Original Release** — Japanese publication/broadcast
- **Official English Release** — verified English publication/availability

A work can show information for more than one track, but only the user's selected track controls their release-relative state.

---

## Product tone

New content is presented as an opportunity rather than a debt.

Good:

> Chapter 74 is available whenever you're ready.

Not part of Shiori's tone:

> You are behind.

The tracker should help people remember and continue, not make entertainment feel like homework.

---

## Disabling Release Intelligence

A user can disable release-relative comparison for an individual work.

When disabled:

- normal progress continues
- no `Up to Date` state is calculated
- no automated release comparison is shown
- no pressure-based language is shown

---

# 2.7 Manual Track Mode

Manual Track is a **manual release track**, not manual progress tracking.

A user following a Spanish edition, regional edition, unusual numbering system, or another unsupported track still gets full Tracking behavior.

Shiori continues to store:

- audiovisual progress
- reading progress
- Library Status
- dates
- rating
- progress history

What Shiori does not do in Manual Track:

- calculate `Up to Date`
- guess release availability
- compare against another edition and pretend it is equivalent
- show “behind” language

If a compatible automated track becomes available later, the user may switch to it.

Existing progress is preserved.

If the new track uses incompatible numbering, Shiori asks for explicit confirmation/adjustment rather than silently renumbering data.

---

# 2.8 Continue

The main Home tracking surface is **Continue**.

It shows works currently marked:

```text
In Progress
```

Ordering:

1. works with verified newly available content on the selected automated release track
2. remaining works by recent activity

Manual Track works still appear, but their order comes from recent activity rather than automated release availability.

---

## Context-aware `+1`

Continue allows a fast update when Shiori knows the next valid unit.

### Audiovisual

`+1` advances to the next known episode and resets playback position.

### Reading

`+1` advances to the next known chapter and resets page position.

The important rule is:

> **Shiori advances to the next known unit; it does not do arithmetic and hope.**

For example, after Chapter 10 the next valid chapter could be:

```text
10.5
Extra
11
```

If Shiori cannot determine the next unit safely, it opens the detailed editor instead.

---

# 2.9 Progress Vault

Mistakes are normal:

- wrong episode
- accidental double update
- chapter advanced too early
- wrong page/position

Progress Vault lets the user undo the most recent progress update for one work.

Undo restores the exact state that existed before that update.

That may include:

- previous episode
- previous playback position
- previous volume
- previous chapter
- previous page

Undo changes the current state.

It does not erase the fact that the original update occurred.

Phase 1 exposes only the latest undoable progress update.

Older navigable history belongs to Full Progress Timeline later.

---

# 2.10 Data portability

Users can export their data without opening a support ticket.

Shiori provides two export types.

---

## MyAnimeList-compatible export

A portable current-state export limited to what the target format can represent.

It may not preserve Shiori-specific details such as:

- page-level progress
- playback position
- irregular labels
- complete progress history
- release-track preferences
- other Shiori metadata

---

## Shiori archive

A high-fidelity export containing the data Shiori can represent natively, including:

- full library
- detailed progress
- relevant dates
- ratings
- progress history

The principle is simple:

> **Users can bring their history to Shiori, but they are never trapped in Shiori.**

---

# 3. Phase 2 — Future scope

These capabilities extend the product beyond the MVP.

Listing them here does not mean Phase 1 should pre-build their infrastructure.

---

## Franchise Autopilot

Proactive “watch/read this next” guidance when the recommendation is unambiguous enough to be useful.

---

## Interactive Franchise Tree

A visual graph of franchise relationships instead of Phase 1's simple relationship list.

---

## Annual Wrapped

A shareable year-in-review based on tracking activity Shiori actually recorded.

---

## Deep Statistics

Richer personal analytics and per-work breakdowns beyond Phase 1's aggregate totals.

---

## Push Notifications

Optional proactive release alerts beyond the in-app Continue experience.

---

## Full Progress Timeline

A navigable view of the historical progress foundation behind Progress Vault.

---

## Granular Scoring

Per-episode and per-chapter ratings alongside the overall work rating.

---

## Custom Lists

User-created lists beyond the default watch/read lists.

---

## Rewatch & Reread Tracking

Support repeated consumption without losing earlier completion history.

---

## Personalized Recommendations

Suggestions based on the user's own library, completion history, ratings, and other approved signals.

---

## List Comparison

Compare compatible public/authorized tracking data with another user through a shared experience.

A comparison request does not create new privacy permission.

---

## Home Screen Widget

Quick access to selected Tracking information/actions from supported devices.

---

## Ownership Tracking

A future way to record what the user owns separately from progress.

The current source document describes this as a simple physical-ownership flag.

Later architecture/product exploration has identified that ownership may eventually need more detail than a work-level boolean if Shiori wants to represent language, edition, format, or individual volumes.

That later design should be synchronized explicitly before implementation rather than silently changing this approved source during editorial cleanup.

---

## Licensing Availability

Structured information about official licensing/availability by supported language or market.

---

## Illustrator Gallery

Extended cover art and illustrator credits, especially for Light Novel volumes.

---

## Extended Localization

Interface languages beyond English and Spanish.

---

## Full Cast Directory

A complete cast/voice-acting surface beyond the bounded Phase 1 character preview.

---

## Per-Work Discussion

The current approved source still lists Per-Work Discussion under Phase 2.

Later Product Horizon work questions whether this fits the tracker-first direction because discussion introduces moderation, abuse/reporting, and community-governance requirements.

That is a product-synchronization issue, not something this humanization should resolve silently.

Until the canonical product documents are synchronized, this section preserves the original approved feature list while making the discrepancy explicit.

---

# 4. Product boundaries worth preserving

Several ideas are intentionally **not** part of Shiori's current direction.

The product does not need:

- streaks
- XP/levels
- invite-only registration
- a global activity feed
- chat/direct messaging
- general-purpose posts
- likes on user activity
- follower/influencer mechanics

This is not because those features are impossible.

They simply do not strengthen Shiori's main job enough to justify the extra product and architecture complexity right now.

---

# 5. What this document owns

`FEATURES.md` describes **what the product should do**.

If a feature needs:

- a new data model
- a service-boundary change
- a new external dependency
- a new consistency guarantee

the architecture documents should be updated before implementation.

Likewise, if later Product Horizon work reclassifies a feature, that product decision should be synchronized here explicitly.

Humanizing this document should improve its voice and clarity; it should not quietly redesign the product.

---

# 6. MVP product promise

If Phase 1 works correctly, a user should be able to:

```text
create an account
-> find works
-> understand how they connect
-> import or build a library
-> track audiovisual or reading progress precisely
-> follow a verified release track when supported
-> use Manual Track when it is not supported
-> continue from where they left off
-> undo the latest mistake
-> share only what they choose
-> export their data again
```

That is the MVP.

The rest can grow later without making the first version feel incomplete.
