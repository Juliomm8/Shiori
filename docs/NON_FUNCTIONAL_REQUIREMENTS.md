# Shiori — Non-Functional Requirements

**Status:** Accepted — STEP 8 Complete  
**Last updated:** 2026-08-09  
**Scope:** Performance, availability, resilience, durability, operational limits, messaging health, observability, retention, scalability, and launch verification.

---

## Project context

I am using these NFRs as **engineering targets**, not as commercial promises.

The point is to make performance and reliability measurable while I build Shiori instead of relying on vague statements such as “the API should be fast” or “the system should be resilient.”

Some numbers in this document are deliberately strict. They give me something concrete to design and test against in staging.

If real measurements later show that a target is unrealistic for the deployment resources available, the right response is to review the target explicitly and document the reason. The target should not be quietly weakened just because the first implementation misses it.

Shiori does not currently define a commercial SLA.

---

# 1. Measurement model

## 1.1 SLI, SLO, and SLA

Shiori uses the terms as follows:

```text
SLI = what is measured
SLO = the internal target for that measurement
SLA = an external contractual promise
```

For the MVP:

```text
SLIs: yes
SLOs: yes
Commercial SLA: no
```

Examples of SLIs include:

- HTTP latency
- successful-request ratio
- `5xx` error rate
- provider failures
- Queue Lag
- Outbox Age
- projection freshness

Latency is measured primarily through:

```text
p50
p95
p99
```

rather than averages alone, because averages can hide a small but important group of very slow requests.

---

## 1.2 HTTP latency measurement

Unless an endpoint defines something more specific, latency is measured server-side across the public Shiori path:

```text
YARP receives request
    -> backend processing
    -> YARP completes response
```

This includes synchronous Shiori-owned work such as:

- YARP middleware/routing
- authentication and authorization
- service Application/Domain processing
- required PostgreSQL/MongoDB work
- Profile BFF dependency calls for profile endpoints

It does not include:

- user Internet latency
- browser rendering
- client-side JavaScript
- asynchronous RabbitMQ work after the HTTP response
- provider synchronization that intentionally runs outside the request path

For uploads, raw network transfer time is not used as the server processing target.

---

## 1.3 Which requests count

Latency and availability objectives apply to valid requests under expected operating conditions.

Expected client-side outcomes such as:

```text
400
401
403
404
409
412
```

are not service-availability failures when they are the documented result of the request.

Unexpected `5xx`, dependency timeouts, and server failures do count.

---

# 2. API performance targets

Shiori does not use one latency target for every operation.

The backend has three different request shapes:

```text
fast local reads
transactional writes
asynchronous job acceptance
```

A Catalog search and a 10,000-item import should not be judged by the same budget.

---

## 2.1 Class A — Fast Local Reads

Typical operations:

- Catalog Search
- Catalog Item read
- Franchise read
- local library reads

These should be served from Shiori-owned state rather than live AniList/MangaDex calls.

### Initial target

| Percentile | Budget |
|---|---:|
| `p50` | `<= 100 ms` |
| `p95` | `<= 250 ms` |
| `p99` | `<= 500 ms` |

These are server-side end-to-end budgets through YARP.

A normal Class A read should not suddenly become a provider call just because local data is missing. Explicit hydration is a different workflow.

---

## 2.2 Class B — Transactional Writes

Typical operations:

- progress update
- status update
- Progress Vault undo
- similar Tracking mutations

A successful Tracking write may include, inside one local PostgreSQL transaction:

```text
current state
+ immutable history
+ revision / optimistic concurrency
+ idempotency state when required
+ Outbox record when required
```

### Initial target

| Percentile | Budget |
|---|---:|
| `p50` | `<= 150 ms` |
| `p95` | `<= 400 ms` |
| `p99` | `<= 800 ms` |

A normal progress write does not synchronously call Catalog.

RabbitMQ publication happens after the local commit through the Outbox publisher, so broker latency is not part of the synchronous write budget.

Correctness wins over raw speed. A fast partial write is not a successful write.

Performance work must not weaken:

- immutable history
- optimistic concurrency
- required idempotency
- required Outbox persistence

---

## 2.3 Class C — Asynchronous Acceptance

The main example is Smart Staging Import.

The request should:

```text
validate bounded request input
-> create durable job
-> return accepted response
```

and then let background work continue.

### Acceptance target

| Percentile | Budget |
|---|---:|
| `p50` | `<= 200 ms` |
| `p95` | `<= 500 ms` |
| `p99` | `<= 1,000 ms` |

This target begins once the server has the payload required for basic validation.

The full Import workflow does not have one universal end-to-end latency target because duration depends on:

- number of records
- Catalog misses
- provider rate limits
- queue backlog
- commit batching

The important product behavior is that the original HTTP request does not stay open for the full job.

---

## 2.4 Performance summary

| Class | Example | `p50` | `p95` | `p99` |
|---|---|---:|---:|---:|
| A — Fast Local Read | Catalog Search | `<= 100 ms` | `<= 250 ms` | `<= 500 ms` |
| B — Transactional Write | Progress Update | `<= 150 ms` | `<= 400 ms` | `<= 800 ms` |
| C — Async Acceptance | Import Job Creation | `<= 200 ms` | `<= 500 ms` | `<= 1,000 ms` |

These are initial MVP engineering targets and must be tested with representative data and concurrency before launch.

---

# 3. Availability and degraded behavior

Shiori is designed around capability-level failure rather than one global “up/down” state.

For example:

```text
AniList unavailable
!=
Tracking unavailable

RabbitMQ unavailable
!=
local Tracking write unavailable

Tracking unavailable
!=
Identity unavailable
```

A capability may be:

```text
available
degraded
unavailable
```

without forcing the whole platform into the same state.

---

## 3.1 Core API availability target

The initial MVP target is:

```text
99.9% successful availability per calendar month
```

measured independently for major capability families such as:

- Identity account/token operations
- Catalog local/canonical reads
- Tracking reads
- Tracking progress writes
- shareable profile behavior

The point of measuring capability families separately is that an AniList outage should not make a successful Tracking write look unavailable.

A documented degraded-success response counts as available when it is the intended contract, but degraded responses must still be measured separately.

---

## 3.2 Provider outages

AniList and MangaDex are outside Shiori's operational control.

Shiori does not define an availability SLO for them.

Instead, it defines how Shiori behaves when they fail.

### AniList unavailable

What should still work:

- existing Catalog Search/detail from MongoDB
- Tracking reads/writes using local projections
- unrelated Identity flows

What degrades:

- Catalog synchronization
- first-time or explicit provider-backed hydration

Shiori preserves the last valid Catalog state instead of replacing it with guessed or empty data.

Tracking never calls AniList directly.

---

## 3.3 Identity unavailable

Identity owns:

- registration
- login
- refresh
- recovery
- revocation
- profile-level visibility

So those capabilities may become unavailable when Identity is down.

Protected Catalog/Tracking calls with an already-valid token may still work when the service can safely validate that token using cached signing/discovery material.

The public-profile path is stricter.

If Identity cannot safely establish profile visibility:

```text
Profile BFF
    -> FAIL CLOSED
    -> no Tracking profile data exposed
```

If Identity already confirmed `Public` and Tracking then fails:

```text
200
+ Identity profile data
+ Tracking sections omitted
```

is the approved degraded response.

Privacy has priority over partial availability.

---

## 3.4 RabbitMQ unavailable

RabbitMQ is intentionally outside the local business transaction.

The normal pattern is:

```text
business state
+ Outbox
-> commit

later:
Outbox publisher
-> RabbitMQ
```

Therefore, if the local Tracking transaction succeeds while RabbitMQ is unavailable:

```text
HTTP mutation: successful
Outbox: pending
messaging: degraded
remote projection/consumer: temporarily stale
```

Shiori does not:

- return failure solely because publishing is delayed
- pretend remote propagation already completed
- switch to best-effort in-memory publication

When RabbitMQ recovers, Outbox publishers and consumers resume from durable state.

---

## 3.5 Degraded-mode summary

| Failure | Main effect | Expected behavior |
|---|---|---|
| AniList unavailable | Catalog ingestion | Existing local Catalog reads continue; sync degrades |
| MangaDex unavailable | Manga/Manhwa enrichment | Other Catalog/Tracking behavior continues |
| Identity unavailable | Auth/token lifecycle | Affected Identity operations unavailable |
| Identity unavailable | Public profile | Fail closed; no Tracking exposure |
| Tracking unavailable after Identity confirms Public | Public profile | Identity-only degraded `200` |
| RabbitMQ unavailable | Integration propagation | Local Outbox-backed writes continue; messaging degrades |
| RabbitMQ unavailable | Tracking projection freshness | Projection may become stale until recovery |

---

# 4. Provider resilience

Only Catalog calls AniList and MangaDex.

Normal Catalog reads and Tracking writes do not depend on provider availability.

The provider policy has four goals:

1. never wait forever
2. retry only failures that may recover
3. avoid retry storms
4. stop hammering a provider that is clearly unhealthy

---

## 4.1 Timeout

Each provider HTTP attempt has a maximum duration of:

```text
3 seconds
```

A timeout means that synchronization attempt failed.

It does not mean the last valid Catalog data should be deleted.

---

## 4.2 Retry policy

Retryable examples:

```text
connection failure
timeout
408
429
500
502
503
504
```

Normally non-retryable examples:

```text
400
401
403
404
deterministic contract/schema failure
```

For one logical provider operation:

```text
1 initial attempt
+ max 2 retries
= max 3 immediate attempts
```

Retry delays:

| Retry | Base delay | Jitter |
|---|---:|---:|
| 1 | `500 ms` | `0–250 ms` |
| 2 | `1,000 ms` | `0–250 ms` |

There is no fourth immediate attempt.

---

## 4.3 `Retry-After`

If the provider returns a valid `Retry-After`, Shiori respects it.

If that requested delay is greater than:

```text
5 seconds
```

the preferred behavior is to persist/defer the work and free the Worker rather than holding it asleep.

Provider-specific official limits take precedence when stricter.

---

## 4.4 Retry ownership

Immediate HTTP retries belong to the Catalog provider integration layer.

Shiori should not accidentally multiply retries across:

```text
Gateway
x Application
x Worker
x HTTP client
```

Higher-level durable workflow retries may happen later, after the immediate provider policy has completed.

---

## 4.5 Circuit Breakers

AniList and MangaDex use independent breakers.

One provider failing must not open the other's breaker.

Baseline:

```text
open after:
5 consecutive logical provider operations fail
after their immediate retry policy is exhausted

open duration:
30 seconds

half-open:
1 probe at a time

close after:
2 consecutive successful probes
```

A retryable failure in Half-Open reopens the breaker.

---

## 4.6 Resilience baseline

| Policy | Target |
|---|---:|
| Per-attempt timeout | `3 s` |
| Immediate retries | `2` |
| Total immediate attempts | `3` |
| Retry delay 1 | `500 ms + jitter` |
| Retry delay 2 | `1,000 ms + jitter` |
| Long `Retry-After` | `> 5 s` -> durable defer |
| Breaker threshold | `5` exhausted logical failures |
| Open duration | `30 s` |
| Half-open concurrency | `1` |
| Recovery | `2` successful probes |

These values should be verified through controlled resilience tests.

---

# 5. Durability, RPO, and RTO

Shiori distinguishes canonical state from rebuildable state.

Canonical stores:

```text
Identity PostgreSQL
Catalog MongoDB
Tracking PostgreSQL
```

Derived examples:

```text
Tracking local Catalog projection
Catalog derived summaries
in-memory caches
future rebuildable read models
```

A derived store can only be treated as rebuildable if the rebuild path actually exists and has been tested.

---

## 5.1 Canonical-data priority

### Identity PostgreSQL

High-durability canonical data:

- Shiori user identity
- account/credential state
- profile state
- profile visibility
- OpenIddict persistence

### Tracking PostgreSQL

High-durability canonical data:

- library relationships
- current progress
- immutable progress history
- ratings/dates
- privacy/list state
- active durable import workflow state
- correctness-related Inbox/Outbox/idempotency state

User progress is central to Shiori's product promise, so losing it is treated as a serious data-integrity failure.

### Catalog MongoDB

Canonical but partially rehydratable.

Provider metadata may be recoverable, but Shiori also owns:

- canonical Shiori IDs
- normalized Catalog state
- franchise grouping
- relationship decisions
- provenance/sync state

So Catalog is not a disposable cache.

---

## 5.2 Recovery targets

### RPO

Maximum acceptable committed-data loss after a catastrophic datastore loss:

| Store | RPO |
|---|---:|
| Identity PostgreSQL | `<= 5 min` |
| Tracking PostgreSQL | `<= 5 min` |
| Catalog MongoDB | `<= 15 min` |

### RTO

Maximum target time to return the affected capability to a verified usable state:

| Store | RTO |
|---|---:|
| Identity PostgreSQL | `<= 60 min` |
| Tracking PostgreSQL | `<= 60 min` |
| Catalog MongoDB | `<= 120 min` |

These are design and recovery-exercise targets, not commercial promises.

They do not select a specific cloud, backup vendor, orchestrator, or HA topology.

---

## 5.3 Backup and restore

Every canonical datastore needs a documented backup mechanism before production launch.

A backup is not considered trustworthy merely because a job says “success.”

Shiori must prove restore.

At least once per calendar month:

```text
Identity PostgreSQL
Catalog MongoDB
Tracking PostgreSQL
```

must each be restored into an isolated environment.

A new restore exercise is also required after a material change to backup/restore behavior.

Before MVP launch, all three stores need at least one successful production-like restore.

---

## 5.4 Restore evidence

A restore exercise should record:

- selected backup/recovery point
- procedure used
- restore result
- service connection/smoke test
- representative reads
- controlled write where appropriate
- achieved RPO
- achieved RTO
- PASS/FAIL

If a restore works but exceeds the target, that is useful evidence but still an NFR failure to review.

---

## 5.5 RabbitMQ is not canonical durability

Business facts are protected by:

```text
canonical state
+ required Outbox
-> same local commit
```

RabbitMQ is transport.

It is not:

- the source of truth for user progress
- the source of truth for Identity
- the historical event store
- the only durable copy of an unpublished fact

---

# 6. Public request and import limits

These limits protect Shiori from oversized input, accidental memory pressure, and abuse.

They are defensive ceilings, not normal payload expectations.

---

## 6.1 Ordinary JSON

Default maximum body size:

```text
256 KiB
262,144 bytes
```

Endpoints that genuinely need more require an explicit bounded contract.

---

## 6.2 Import upload

Maximum import-file content:

```text
5 MiB
5,242,880 bytes
```

Maximum total HTTP request envelope:

```text
6 MiB
6,291,456 bytes
```

Maximum parsed entries per import:

```text
10,000
```

The already-planned ~4,000-title scenario remains inside the supported ceiling.

---

## 6.3 Enforcement

YARP should reject known oversized requests as early as practical.

Oversized public bodies use:

```text
413 Content Too Large
```

No `Content-Length` does not mean unlimited input. Streaming/chunked requests still count bytes and are aborted after the limit.

Tracking validates limits again itself.

The Gateway is defense-in-depth, not the only place where the rule exists.

---

## 6.4 Memory behavior

The import architecture should remain:

```text
bounded upload
-> secure temporary file
-> durable import job
-> background parse
-> bounded staging
```

not:

```text
upload
-> load entire file
-> build entire object graph
-> parse everything in request memory
-> finally return
```

Heavy import work belongs to the background workflow.

---

## 6.5 Parser safety

Size limits do not replace safe XML parsing.

Unsafe DTD/entity behavior remains disabled.

Input remains untrusted even when it is smaller than 5 MiB.

---

## 6.6 Commit batching

A 10,000-item import does not justify one giant transaction.

After user confirmation, commit remains:

```text
bounded idempotent batches
+ durable checkpoints
+ short finalization transaction
```

The exact batch size should come from measurement later.

---

# 7. Messaging health

A healthy RabbitMQ process is not enough.

Shiori needs to know whether work is actually moving from:

```text
Outbox
-> RabbitMQ
-> Consumer
-> Inbox/local effect
```

---

## 7.1 Signals

Minimum useful signals include:

- broker connectivity
- Queue Depth
- oldest queued message age
- consumer success/failure rate
- last successful consumption
- oldest unpublished Outbox age
- publication failures
- Inbox failures
- DLQ count
- projection freshness

---

## 7.2 Queue Lag

Queue Lag is primarily:

> the age of the oldest message waiting for its intended consumer path.

It is more useful than Queue Depth alone because:

```text
1000 messages for 3 seconds
may be healthy

1 message for 10 minutes
may be unhealthy
```

### Initial thresholds

| Oldest queued message | State |
|---|---|
| `< 2 min` | Healthy |
| `>= 2 and < 5 min` | Degraded |
| `>= 5 min` | Alert |
| `>= 15 min` | Critical |

Primary alert threshold:

```text
Queue Lag >= 5 minutes
```

---

## 7.3 Outbox Age

Outbox Age is:

> the age of the oldest committed Outbox record not yet successfully published.

Initial thresholds:

| Oldest unpublished Outbox | State |
|---|---|
| `< 30 sec` | Healthy |
| `>= 30 sec and < 2 min` | Degraded |
| `>= 2 min` | Alert |
| `>= 5 min` | Critical |

Primary alert:

```text
Outbox Age >= 2 minutes
```

This is stricter than Queue Lag because the message has not even entered the broker path yet.

---

## 7.4 DLQ

Expected steady state:

```text
0
```

Any newly dead-lettered message triggers operational attention.

DLQ is not:

- normal backlog
- long-term event history
- an infinite retry mechanism

Replay is never blind.

Before replay, the root cause should be understood or the transient dependency should be known to have recovered.

Replaying the same logical message preserves its identity/idempotency semantics.

---

## 7.5 Messaging summary

| Signal | Healthy | Alert | Critical |
|---|---:|---:|---:|
| Queue Lag | `< 2 min` | `>= 5 min` | `>= 15 min` |
| Outbox Age | `< 30 sec` | `>= 2 min` | `>= 5 min` |
| New DLQ messages | `0` | `>= 1` | sustained/increasing |
| Queue Depth | workload-specific | trend-based | workload-specific |

Eventual consistency still has to converge.

“Eventually consistent” is not an excuse for a projection that remains stale indefinitely.

---

# 8. Observability

Observability should let me answer:

- which request failed?
- where did it fail?
- which distributed flow did it belong to?
- did a dependency fail?
- was work retried?
- did it reach the DLQ?
- how long did each stage take?

without turning logs into another database of private user information.

No observability vendor is selected by this document.

---

## 8.1 Structured logs

Executable components emit structured logs:

- YARP
- Identity
- Catalog
- Tracking
- Profile BFF
- Workers/consumers
- Outbox publishers

Useful context includes:

```text
correlationId
traceId
spanId
service/component
environment
UTC timestamp
severity
operation/event name
```

For HTTP completion logs, where safe:

- method
- route template
- status
- server duration

Route templates are preferred to raw user-controlled URLs/query strings.

---

## 8.2 Trace and correlation

Shiori keeps both:

```text
W3C trace context
correlationId
```

Trace context represents distributed trace structure.

`correlationId` is the broader human-friendly operational flow identifier.

Both continue through supported synchronous boundaries.

When a flow becomes asynchronous, correlation/causation metadata continues through the integration message.

---

## 8.3 Sensitive data never belongs in normal logs

Normal logs/traces must not contain:

- passwords
- password hashes
- access tokens
- refresh tokens
- Authorization headers
- auth cookies
- recovery secrets
- client secrets
- signing keys
- provider credentials
- raw email addresses
- raw import files
- import contents
- biography/private profile content
- private lists
- full private request/response bodies

Email is personal account data and should not be logged.

Use safe identifiers such as:

- `correlationId`
- `traceId`
- opaque Shiori `UserId` only when operationally justified
- machine-readable error codes

---

## 8.4 Request bodies

Full body logging is disabled by default.

Safe bounded metadata may still be useful, such as:

- payload byte count
- item count
- media type
- parser outcome
- stable error code

Import contents are never copied into logs.

---

## 8.5 Liveness vs readiness

### Liveness

Answers:

> Is the process alive?

It should be shallow.

A remote outage should not automatically make a healthy process look dead.

For example, RabbitMQ being down should not fail Tracking API liveness.

### Readiness

Answers:

> Can this component safely serve the workload it owns right now?

A service's own canonical database is normally a readiness dependency:

```text
Identity -> Identity PostgreSQL
Catalog  -> Catalog MongoDB
Tracking -> Tracking PostgreSQL
```

If the database is down:

```text
process may be live
service not ready
```

---

## 8.6 Readiness respects degraded design

RabbitMQ alone is not a Tracking API readiness dependency when the Outbox path can preserve publication intent.

AniList/MangaDex are not normal Catalog-read readiness dependencies.

Gateway readiness should not simply aggregate every downstream service. Otherwise one Catalog outage could make Identity and Tracking unreachable through the edge.

The Profile BFF must preserve Identity-first fail-closed privacy regardless of how health checks are implemented.

---

# 9. Retention

Retention is different for canonical product data, temporary workflow data, infrastructure correctness records, diagnostics, and backups.

The basic rules are:

1. canonical product data is not deleted just to keep tables small
2. temporary/infrastructure data gets bounded retention
3. unresolved correctness failures are not silently deleted because a TTL expired

---

## 9.1 Retention table

| Data | Retention |
|---|---:|
| Successfully processed RabbitMQ Inbox | `7 days` |
| Successfully published Outbox | `7 days` |
| Unpublished Outbox | No age-based deletion |
| Completed HTTP idempotency result | `24 hours` |
| Temporary Import file | `<= 24 hours` after no longer needed |
| Terminal Import staging | `<= 24 hours` |
| Terminal Import job metadata | `30 days` |
| DLQ message | `14 days` normal maximum |
| Normal structured logs | `30 days` |
| Detailed distributed traces | `7 days` |
| Identity/security audit events | `90 days` |
| Canonical datastore recovery chain | `>= 35 days` |
| Canonical product data | Product/account lifecycle rules |

---

## 9.2 Inbox and Outbox

Successful Inbox rows:

```text
7 days
```

Successful published Outbox rows:

```text
7 days
```

An unpublished Outbox row is never deleted just because it is old.

An old unpublished row is an incident signal, not garbage.

---

## 9.3 HTTP idempotency

Completed idempotency records are retained for:

```text
24 hours
```

After that, the server no longer promises to remember the old result for that key.

An Idempotency Key is not a permanent business identifier.

---

## 9.4 Import temporary data

Once the raw upload is safely parsed into durable staging and no longer needed for recovery:

```text
delete as soon as practical
and no later than 24 hours
```

Failed/cancelled/rejected uploads follow the same maximum cleanup window after terminal outcome.

Terminal staging rows are also removed within 24 hours once they are no longer needed for correctness.

Terminal job metadata may remain for 30 days.

The raw file is not retained for debugging.

---

## 9.5 DLQ

DLQ messages have a normal maximum operational window of:

```text
14 days
```

That does not mean “ignore them for 14 days.”

They need immediate attention and must be replayed, explicitly resolved, or preserved through an incident process before expiry.

---

## 9.6 Logs, traces, and audit events

```text
application logs: 30 days
distributed traces: 7 days
security audit events: 90 days
```

Retention never relaxes privacy rules.

A 7-day trace retention still does not justify logging tokens or emails.

---

## 9.7 Backup retention

Canonical recovery data remains available for at least:

```text
35 days
```

This supports monthly restore exercises.

The mechanism still has to satisfy the much tighter RPO windows; 35-day retention alone does not provide a 5-minute RPO.

---

## 9.8 Cleanup behavior

Cleanup runs inside the owning bounded context.

It should be:

- bounded
- idempotent
- retry-safe
- batched
- observable

No global cleanup microservice is needed.

---

# 10. Scalability and capacity

Shiori's services can scale independently, but that does not mean “add replicas everywhere.”

The decision process should be:

```text
measure
-> identify SLO/resource pressure
-> find real bottleneck
-> optimize or scale the owner
```

not:

```text
traffic increased
-> scale everything
```

No fixed production replica count is defined here.

---

## 10.1 Catalog reads

Profile:

```text
read-heavy
latency-sensitive
index-sensitive
MongoDB-sensitive
```

Useful signals:

- request concurrency
- p50/p95/p99
- API CPU/memory
- Mongo query latency
- connection-pool pressure
- slow queries
- `5xx`

An unindexed query should not be “fixed” only by adding more API instances.

---

## 10.2 Tracking writes

Profile:

```text
transactional
latency-sensitive
PostgreSQL-sensitive
history/idempotency/outbox-sensitive
```

Useful signals:

- write p50/p95/p99
- transaction latency
- connection-pool saturation
- lock contention
- deadlocks
- DB CPU/I/O
- Outbox health
- `5xx`

More API replicas do not repair a saturated PostgreSQL write path.

---

## 10.3 Imports

Profile:

```text
bursty
CPU-heavy during parse
database-heavy during staging/commit
queue-dependent
batch-oriented
```

Useful signals:

- queued/running jobs
- oldest job age
- processing duration
- worker CPU/memory
- PostgreSQL pressure
- hydration backlog
- Queue Lag
- Outbox Age
- retry/failure rate

Worker concurrency remains bounded.

The purpose of background work is isolation, not unlimited parallelism.

---

## 10.4 Provider synchronization

Profile:

```text
network-bound
provider-rate-limited
Circuit-Breaker controlled
```

Useful signals:

- provider latency
- timeouts
- retries
- `429`
- breaker state
- last successful sync

Provider throttling is usually a reason to slow down, not to add more outbound workers.

---

## 10.5 Profile BFF

Profile:

```text
stateless
read-only
fan-out
dependency-latency sensitive
privacy-sensitive
```

Useful signals:

- concurrency
- end-to-end p95/p99
- Identity latency/failure
- Tracking latency/failure
- full vs degraded response ratio
- fail-closed rate

Scaling the BFF cannot repair an unavailable Identity or Tracking dependency.

---

## 10.6 Identity

Profile:

```text
security-critical
PostgreSQL-backed
token/cryptography cost
latency-sensitive
```

Useful signals:

- request concurrency
- endpoint latency
- CPU/memory
- Identity DB latency
- connection-pool pressure
- `5xx`
- rate-limit trends
- signing/discovery errors

Security controls are never disabled merely to improve throughput.

---

## 10.7 YARP

Profile:

```text
all-public-request fan-in
network/I/O heavy
stateless
```

Useful signals:

- request rate
- concurrency
- CPU/memory
- socket pressure
- Gateway-added latency
- request-policy rejections
- downstream timeout/error distribution

YARP should not become a hidden workflow engine or bottleneck.

---

## 10.8 Scaling from evidence

No single metric is enough.

Good scaling decisions combine several kinds of evidence:

```text
SLO pressure
resource saturation
backlog pressure
dependency pressure
```

For workers, Queue Depth should be considered together with:

```text
Queue Lag
consumer throughput
downstream capacity
```

A temporary spike is not automatically a scaling event.

---

## 10.9 Pre-launch capacity baseline

Before launch, production-like staging should establish a repeatable baseline for each critical workload.

Record at least:

- workload definition
- concurrency
- throughput
- p50/p95/p99 where relevant
- error rate
- CPU/memory
- DB latency
- connection-pool pressure
- queue depth/lag where relevant
- first observed bottleneck

The purpose is to know:

> At this resource profile, how much load can Shiori sustain while staying inside its NFRs, and what fails first as load grows?

No launch throughput number is invented here because final production resources and expected traffic are not fixed yet.

---

# 11. Dashboards and alerts

An NFR that exists only in Markdown is not operationally useful.

Launch-critical SLIs should be visible in staging/production and connected to alerts when someone actually needs to respond.

No monitoring vendor is mandated.

---

## 11.1 HTTP dashboard

At minimum:

- request volume
- success ratio
- `5xx` rate
- availability
- p50/p95/p99
- full vs degraded responses

Views should be separable by service/capability.

Catalog Search, Progress Update, and Import Acceptance should not be averaged into one meaningless latency chart.

---

## 11.2 Messaging dashboard

At minimum:

- Queue Depth
- Queue Lag
- Outbox Age
- publication failures
- consumer success/failure/retries
- Inbox failures
- DLQ count
- projection freshness

Existing thresholds remain:

```text
Queue Lag >= 5 min   -> Alert
Queue Lag >= 15 min  -> Critical

Outbox Age >= 2 min  -> Alert
Outbox Age >= 5 min  -> Critical

new DLQ message      -> Alert
```

---

## 11.3 Datastores

For each canonical store, monitor enough to detect whether it can serve the owning workload:

- connectivity
- query/operation latency
- error rate
- connection-pool pressure
- resource pressure
- backup status
- restore evidence status

---

## 11.4 Providers

AniList and MangaDex should be visible separately:

- latency
- success/failure
- timeouts
- retries
- `429`
- breaker state
- last successful sync

A provider incident should not look like a generic Catalog outage.

---

## 11.5 Imports

Dashboard at minimum:

- jobs by state
- oldest pending/running job
- duration
- failure/retry rate
- stuck-job indicator
- hydration backlog

A healthy `202 Accepted` rate should not hide a background subsystem that stopped making progress.

---

## 11.6 Latency alerts

The main p95 budgets are:

```text
Fast Local Read      <= 250 ms
Transactional Write  <= 400 ms
Async Acceptance     <= 500 ms
```

p99 remains visible.

Alerts should use sustained evaluation rather than paging on one slow request.

Exact query syntax/window is an implementation detail.

---

## 11.7 `5xx` alerts

Unexpected `5xx` should be visible by capability.

Alerting should detect sustained behavior that puts the 99.9% monthly capability target at risk.

The exact short-window percentage is intentionally left for implementation because low-traffic and high-traffic routes need different statistical treatment.

---

## 11.8 Alert quality

Alerts should answer:

- what failed?
- which capability?
- which threshold?
- current value?
- user-facing or background-only impact?
- likely dependency?
- first safe diagnostic step?
- relevant runbook?

An alert that says only:

```text
Something is wrong
```

is not useful.

Routine noise is also a failure.

If an alert is permanently ignored, it should be fixed, removed, or reclassified.

---

# 12. Milestone 5B verification

These NFRs become meaningful only when the implementation proves them.

Milestone 5B is the final MVP verification gate in production-like staging.

The environment should exercise the real architectural path, including where applicable:

- YARP
- Identity
- Catalog
- Tracking
- Profile BFF
- PostgreSQL
- MongoDB
- RabbitMQ
- approved Workers

---

## 12.1 Load tests

At minimum:

```text
Catalog reads/search
Tracking progress writes
Concurrent imports
```

### Catalog

Evidence should include:

- workload and concurrency
- p50/p95/p99
- `5xx`
- API CPU/memory
- Mongo latency/pressure
- confirmation that normal reads do not call providers live

### Tracking

Evidence should include:

- write concurrency
- p50/p95/p99
- `5xx`
- PostgreSQL latency
- pool/lock pressure
- correctness of state + history + Outbox/idempotency

### Imports

Evidence should include:

- concurrent jobs
- representative file sizes/item counts
- acceptance latency
- processing duration/throughput
- CPU/memory
- PostgreSQL pressure
- Queue Depth/Lag
- retry/failure behavior
- proof that ordinary API traffic remains usable

---

## 12.2 Boundary tests

Verify at minimum:

```text
ordinary JSON <= 256 KiB -> normal handling
ordinary JSON > limit    -> safe rejection

Import <= 5 MiB          -> accepted if otherwise valid
Import > 5 MiB           -> 413

Import <= 10,000 entries -> supported
Import > 10,000 entries  -> rejected without partial live mutation
```

---

## 12.3 Fault-injection tests

Controlled staging tests should cover:

- AniList down
- MangaDex down
- provider timeout
- provider `429`
- repeated provider `5xx`
- RabbitMQ down
- consumer restart
- redelivery
- stale/out-of-order event
- poison message
- Identity unavailable
- Tracking unavailable during public profile composition
- canonical DB unavailable

This is controlled fault injection in staging, not deliberate production chaos.

---

## 12.4 Provider resilience verification

Tests should prove:

- 3-second timeout
- bounded retries
- backoff + jitter
- provider-specific breaker behavior
- preservation of last valid Catalog state
- Tracking independence
- clean recovery without retry storm

---

## 12.5 RabbitMQ outage verification

A broker outage should prove:

```text
local Tracking write
-> current state + history + required Outbox commit
-> HTTP succeeds according to endpoint contract

messaging
-> degraded

after recovery
-> Outbox publishes
-> consumers converge
```

Also verify:

- no unpublished Outbox disappears
- duplicate redelivery does not duplicate effects
- Queue Lag returns to normal
- Tracking API readiness does not fail solely because RabbitMQ is unavailable

---

## 12.6 Profile privacy verification

Required cases:

```text
Identity Private
-> no Tracking exposure

Identity unavailable/unknown
-> fail closed
-> no Tracking exposure

Identity Public + Tracking healthy
-> full authorized profile

Identity Public + Tracking unavailable
-> Identity-only degraded profile
```

No resilience optimization is allowed to weaken this privacy behavior.

---

## 12.7 Health-check verification

Examples:

```text
Tracking alive + Tracking PostgreSQL down
-> live
-> not ready

Tracking PostgreSQL healthy + RabbitMQ down
-> live
-> may remain ready
-> messaging degraded

Catalog MongoDB healthy + AniList down
-> Catalog API may remain ready
-> provider sync degraded
```

---

## 12.8 Restore verification

Before launch:

```text
Identity PostgreSQL
Tracking PostgreSQL
Catalog MongoDB
```

each need a successful full restore exercise with measured RPO/RTO and smoke/integrity evidence.

A backup that has never been restored does not satisfy the launch gate.

---

## 12.9 Observability verification

Test that intentionally created conditions are actually visible:

- latency breach
- controlled `5xx`
- Queue Lag threshold
- Outbox Age threshold
- new DLQ message
- provider breaker open
- stuck/failed import
- canonical DB readiness failure

The correct alert should fire and provide enough context to begin diagnosis.

---

## 12.10 Retention verification

Representative cleanup tests should prove:

```text
Inbox success -> eligible after 7 days
Outbox published -> eligible after 7 days
Outbox unpublished -> never age-purged
HTTP idempotency -> 24 hours
temporary import file -> <= 24 hours after safe terminal point
terminal staging -> <= 24 hours
terminal import metadata -> 30 days
DLQ -> explicit operational resolution within normal 14-day window
```

Cleanup should be bounded and idempotent.

---

## 12.11 Contract and migration verification

Before launch:

- OpenAPI compatibility checks pass
- integration contract tests pass
- migrations/bootstrap succeed from clean state
- deployment migration checks pass
- Gateway smoke tests pass
- Architecture Tests stay green

NFR compliance does not replace contract or architecture compliance.

---

# 13. Evidence matrix

The launch checklist should point to actual evidence rather than a vague “tested” checkbox.

| Area | Evidence |
|---|---|
| API latency | Load-test report with workload and p50/p95/p99 |
| Availability / `5xx` | Capability dashboard + controlled-failure evidence |
| Provider resilience | Timeout/retry/backoff/breaker test |
| RabbitMQ degradation | Broker outage + Outbox + recovery test |
| Queue/Outbox health | Alert tests crossing documented thresholds |
| DLQ | Poison-message isolation + alert + safe handling |
| Import limits | 5 MiB / request / 10,000-entry boundary tests |
| Capacity | Production-like baseline + first bottleneck |
| Liveness/readiness | Dependency-failure matrix |
| Durability | Restore evidence with achieved RPO/RTO |
| Retention | Cleanup lifecycle tests |
| Log privacy | Review/test proving sensitive fields are excluded |
| Profile privacy | Fail-closed/degraded-profile tests |
| Contracts | OpenAPI + integration compatibility results |
| Deployment | Clean migrations + smoke tests |

---

# 14. Launch-blocking failures

Milestone 5B does not pass while a critical requirement is known to fail.

Examples:

- security/privacy failure
- canonical data-integrity failure
- canonical datastore cannot be restored
- RPO/RTO materially outside target
- core API cannot meet agreed launch latency/availability at launch load
- broker outage loses required Outbox-backed facts
- broker redelivery creates duplicate business effects
- Identity failure leaks Tracking profile data
- oversized Import bypasses limits
- critical incidents are not observable/actionable

If a target genuinely needs to change, update the requirement explicitly.

Do not turn a failure into a PASS by silently lowering the number.

---

# 15. STEP 8 status

STEP 8 is complete because Shiori now has measurable requirements for:

- latency
- availability
- degraded behavior
- provider resilience
- durability/recovery
- request/import limits
- messaging health
- observability/privacy
- retention
- scalability
- dashboards/alerts
- launch verification

The implementation relationship is:

```text
STEP 8
define targets
    |
    v
Implementation
build and measure against them
    |
    v
Milestone 5B
prove them with repeatable evidence
```

**STEP 8 — Non-Functional Requirements — Complete.**
