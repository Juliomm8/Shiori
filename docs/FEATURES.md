# Shiori — Product Feature Specification

**Status:** Approved  
**Last updated:** July 2026  
**Scope:** Official product feature specification for Shiori. This document describes what the product does. For the underlying system architecture, see `ADR.md`.

---

## 1. Executive Summary

Shiori is a unified tracking platform for anime, manga, light novels, and manhwa.

Today, a single franchise can be scattered across a streaming app, a manga reader, a spreadsheet, and multiple outdated trackers that were not designed to handle different entertainment formats together. Shiori replaces that fragmentation with one precise, living record of what a person has watched, read, and may want to continue next.

Shiori's promise is simple: **never lose your progress, always know what is available now, and understand how each franchise connects.**

---

## 2. Phase 1: Minimum Viable Product (MVP)

Phase 1 is the complete set of features required to launch Shiori as a standalone product.

Every feature below supports one of Shiori's core product goals:

- Preserve progress accurately.
- Make connected works easier to understand.
- Help users continue without pressure.
- Give users control over their own data.

Nothing in this phase is decorative.

### 2.1 Content Catalog & Discovery

- **Catalog Item Pages.** Every anime, manga, light novel, or manhwa entry has a dedicated page with cover art, synopsis, original title, and available title alternatives. Users can choose their preferred title language, with Romaji used by default.

- **Franchise Relationships.** Each catalog item shows its verified related works — including adaptations, sources, sequels, prequels, and side stories — so users can navigate the wider franchise without leaving Shiori. Phase 1 presents these relationships as a list; it does not guarantee a single recommended consumption order.

- **Official Consumption Links.** When verified links are available, Shiori shows official platforms where a work can be legally watched or read, such as Crunchyroll, Netflix, or MANGA Plus. Availability may vary by region, and the absence of a link does not mean that no official release exists.

- **Trailers.** Anime entries can display embedded trailers when a verified trailer is available.

- **Character Preview.** Each catalog item shows a bounded preview of up to 10 main characters. Full cast browsing and language-specific voice acting credits are reserved for Phase 2.

- **Trending & Seasonal Discovery.** The home experience includes surfaces for content that is currently popular and content that is airing, publishing, or releasing during the current season.

- **Work-Focused Search.** Global search is designed to find entertainment content, not people. Shiori is a tracking product first and does not search for or surface other users.

- **Appearance & Language.** Shiori supports dark, light, and system themes. The interface launches in English and Spanish, with English used as the default language.

### 2.2 Library & Personalization

- **Account Access.** Users can create an account, sign in, sign out, and recover access to their account. Authentication works consistently across web and mobile clients.

- **List Privacy.** Every list is private by default. Users can make individual lists public at any time.

- **Shareable Profile.** A read-only profile link centered on consumption history, not followers or a social graph. A public profile only exposes entries from lists the user has explicitly made public.

- **Watchlists & Read-lists.** Shiori provides dedicated lists for content a user plans to watch or read next.

- **Library Status.** Every tracked entry has one user-controlled library status:
  - Planned.
  - In Progress.
  - Paused.
  - Completed.
  - Dropped.

- **Consumption Dates.** Users can record start, finish, and pause dates for tracked works.

- **Scoring.** Users can assign an overall rating from 1 to 5 stars to a tracked work.

- **Core Statistics.** Shiori provides aggregate totals across the library, including estimated hours watched and recorded pages read. Shiori only presents estimates when enough duration or page-level data is available and does not invent missing values.

### 2.3 Smart Staging Import

New users are never asked to rebuild years of history by hand.

Shiori imports supported list-export files from MyAnimeList and AniList-compatible sources through three user-visible stages:

1. Upload.
2. Preview.
3. Confirm.

Uploading a file creates a staging job and returns immediately. Shiori validates, parses, and matches the file asynchronously, without keeping the original API request or API Gateway connection open.

When the staging job is ready, Preview shows:

- Matched titles.
- Unmatched entries.
- Ambiguous matches.
- Invalid or unsupported progress values.
- Proposed conflict resolutions.
- Entries that are still being resolved.

No item is added to the user's library during Upload or Preview.

Users can resolve entries individually or apply a bulk rule to compatible unresolved entries. For example, a user may choose to keep the highest valid progress value across matching records.

Closing the preview or cancelling the import before confirmation leaves the account unchanged.

Confirm starts an idempotent background commit of the staged result. The user can see the import status, and retries never create duplicate library entries.

Imports with ten or four thousand titles use the same workflow and safety guarantees, although processing time may differ.

### 2.4 Quick Start Onboarding

A user with no list to import is never forced into an empty dashboard.

Quick Start lets the user choose up to five familiar titles across different formats and assign each one a simple state:

- Planned.
- In Progress.
- Completed.

Only titles marked In Progress appear in the Continue dashboard.

Titles marked Planned appear in the appropriate watchlist or read-list.

Quick Start can be skipped.

### 2.5 Polymorphic Tracking

Shiori tracks progress using the units that match how each type of content is consumed.

- **Anime:** episode number and playback position.
- **Reading — Manga, Manhwa, and Light Novels:** volume, chapter, and page.

Reading progress supports irregular chapter numbering as a normal case, not an exception.

Supported chapter labels include:

- Whole numbers.
- Decimal chapters such as `10.5`.
- Chapter zero.
- Extras.
- One-shots.
- Specials.
- Named interludes.

These values are treated as first-class progress positions rather than workarounds.

Each tracking entry also has a user-controlled library status:

- Planned.
- In Progress.
- Paused.
- Completed.
- Dropped.

For an ongoing work on an automated release track, Shiori may also derive the release-relative state **Up to Date** when the user's progress matches the latest verified release on their selected track.

Up to Date is not a user-selected library status, and it never means that the work itself is finished.

Shiori keeps **Completed** and **Up to Date** separate:

- A finished work can be Completed.
- An ongoing work can be Up to Date.
- A completed finished work does not need an active release-relative state.
- Manual Track Mode does not calculate Up to Date.

### 2.6 Release Intelligence

Shiori tells users where they stand only when it has verified, structured release data.

Precision is more important than coverage. When Shiori cannot support a release track with confidence, it does not estimate.

For each supported work, users can select one active automated track:

- **Original Release:** the Japanese publication or broadcast.
- **Official English Release:** the verified English-language publication or availability track.

Shiori calculates content availability and the Up to Date state against the selected track.

Shiori may display both tracks for reference, but only the selected track controls the user's release-relative state.

New content is always framed as an opportunity, never as a debt.

For example, Shiori says:

> Chapter 74 is available whenever you're ready.

It does not say:

> You are behind.

Release Intelligence can be disabled per work for users who prefer to save episodes or chapters before continuing.

When Release Intelligence is disabled:

- Progress tracking continues normally.
- No Up to Date state is calculated.
- No release comparison is displayed.
- No pressure-based language is shown.

### 2.7 Manual Track Mode

Manual Track Mode is a manual release track, not manual progress tracking.

For a Spanish edition, or any other local or regional release that Shiori cannot track with verified automated data, the user's progress is still recorded in full.

Shiori continues to store:

- Episode and playback position for Anime.
- Volume, chapter, and page for reading formats.
- Library status.
- Consumption dates.
- Ratings.
- Progress history.

In Manual Track Mode, Shiori does not:

- Calculate Up to Date.
- Estimate release availability.
- Display a false comparison against another edition.
- Display any "behind" language.

A user can switch to a compatible automated release track later when one becomes available.

Existing progress is preserved and is never silently renumbered, converted, or discarded.

If the automated track uses incompatible numbering, Shiori requires an explicit user confirmation or progress adjustment instead of guessing.

### 2.8 Dashboard "Continue"

The home screen's primary surface is a single row of all works currently marked In Progress.

Items with newly available content on the user's selected, verified release track appear first.

Remaining items are ordered by recent activity.

Each entry supports a context-aware quick update without opening the full work page:

- **Anime:** `+1` advances to the next episode and resets the playback position.
- **Reading:** `+1` advances to the next known chapter and resets the page position.

When Shiori does not know the next valid unit, it opens the detailed progress editor instead of guessing.

Works in Manual Track Mode remain visible in Continue, but they are ordered by recent activity rather than automated release availability.

### 2.9 Progress Vault (Undo)

Mistakes happen — a wrong episode number, an accidental double update, or a chapter advanced too early.

Shiori lets a user undo the most recent progress update for a specific work.

Undo restores the exact position stored before that update without changing any earlier history.

For example, undo can restore:

- The previous episode number.
- The previous playback position.
- The previous volume.
- The previous chapter.
- The previous page.

Phase 1 only exposes the latest undoable update for each work.

Browsing and restoring older history is reserved for the Full Progress Timeline in Phase 2.

### 2.10 Data Portability

Users can export their library from the first day they use Shiori, without opening a support request.

Shiori provides two export types:

- **MyAnimeList-compatible export:** a portable current-state export limited to the fields supported by that format.

- **Shiori archive:** a complete, high-fidelity export containing the full library, detailed progress, relevant dates, ratings, and progress history.

The MyAnimeList-compatible export may omit information that the target format cannot represent, such as:

- Page-level reading progress.
- Playback position.
- Irregular chapter labels.
- Complete progress history.
- Release track preferences.
- Shiori-specific metadata.

The Shiori archive preserves those details.

The principle is direct: users can bring their history to Shiori, but they are never trapped in Shiori.

---

## 3. Phase 2: Future Scope

The following features extend Shiori beyond its MVP.

They are not required to launch and are listed here to preserve the product vision without expanding Phase 1's scope.

- **Franchise Autopilot.** Proactive "watch this next" guidance built on franchise relationships, offered only when the correct next entry is unambiguous.

- **Interactive Franchise Tree.** A visual, explorable map of a franchise's adaptations, replacing Phase 1's simple relationship list.

- **Annual Wrapped.** A shareable, personalized year-in-review of a user's watching and reading habits.

- **Deep Statistics.** Per-work breakdowns and richer visualizations beyond Phase 1's aggregate totals.

- **Push Notifications.** Proactive alerts for new episodes and chapters, beyond the in-app Continue dashboard.

- **Full Progress Timeline.** An expanded, navigable history behind Progress Vault, including which device made each change.

- **Granular Scoring.** Per-episode and per-chapter ratings, in addition to the overall work score.

- **Custom Lists.** User-created, freeform lists beyond Watchlist and Read-list.

- **Rewatch & Reread Tracking.** Logging repeat viewings or readings without losing the original completion date.

- **Personalized Recommendations.** Suggestions based on a user's own rating and completion history.

- **List Comparison.** A way to compare libraries with another user through a shared link, without a persistent following relationship.

- **Home Screen Widget.** Native quick-update access from a device's home screen.

- **Ownership Tracking.** A separate "own it physically" flag for print collectors, distinct from reading progress.

- **Licensing Availability.** Per-language information about whether a work has an official licensed release.

- **Illustrator Gallery.** Cover art and illustrator credits for light novel volumes.

- **Extended Localization.** Interface languages beyond English and Spanish.

- **Full Cast Directory.** A complete cast and voice acting view, filterable by language, beyond the Phase 1 character preview.

- **Per-Work Discussion.** Community commentary scoped to a work rather than individual episodes, introduced only after moderation tooling exists.

---

This document reflects the architecture accepted in `ADR.md`.

Any feature that requires a new data model, service boundary, external dependency, or consistency guarantee must be reflected in the architecture documentation before implementation begins.