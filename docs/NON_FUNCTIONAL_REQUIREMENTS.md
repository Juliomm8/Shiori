# Shiori — Non-Functional Requirements

**Status:** Accepted — STEP 8 Complete  
**Last updated:** 2026-08-09  
**Scope:** Measurable backend quality requirements for performance, availability, resilience, durability, operational limits, messaging health, observability, retention, scalability, alerting, and MVP launch verification.

## Related Documents

This document defines measurable quality targets for the architecture and product behavior already accepted in:

- `ADR.md`
- `SYSTEM_DESIGN.md`
- `API_CONVENTIONS.md`
- `EVENT_CONTRACTS.md`
- `FEATURES.md`
- `ROADMAP.md`
- `PRODUCT_HORIZON.md`
- `FUTURE_STRESS_TEST.md`

This document does not introduce new product features, bounded contexts, databases, providers, brokers, monitoring vendors, orchestration products, or speculative infrastructure.

The governing principle is:

> **An architectural quality claim is not accepted because it sounds desirable. Shiori must define measurable targets, deterministic degraded-mode behavior, and repeatable verification evidence.**

## Document Map

1. Purpose & Terminology
2. Performance — API Latency Classes
3. Availability & Degradation
4. Resilience — Timeouts, Retries & Circuit Breakers
5. Data Durability & Recovery — RPO / RTO
6. Operational Limits — Imports & Public Requests
7. Messaging Health — RabbitMQ, Outbox, Inbox & DLQ
8. Observability — Structured Logs, Distributed Traces & Health Checks
9. Data Retention Policy
10. Scalability & Capacity Expectations
11. Dashboards & Alerting
12. Verification — Milestone 5B Launch Gate
13. STEP 8 Completion Gate

---
# 1. Purpose & Terminology

## 1.1 Purpose

Shiori already defines what the platform does and how its major components interact.

The purpose of `NON_FUNCTIONAL_REQUIREMENTS.md` is different.

It defines the measurable quality properties that the implementation must satisfy while delivering the approved architecture and product behavior.

Examples include:

- How quickly a normal Catalog read should complete.
- How quickly a Tracking progress mutation should commit.
- What availability Shiori expects from core public API capabilities.
- What remains operational when AniList, Identity, or RabbitMQ is unavailable.
- Which degraded responses are considered valid product behavior rather than silent failure.

This document therefore converts broad statements such as:

```text
"Shiori must be fast."
"Shiori must be reliable."
"Shiori must remain available during partial failures."
```

into requirements that can later be verified through:

- Metrics.
- Load tests.
- Resilience tests.
- Integration tests.
- Staging observations.
- Production monitoring.

The values in this document are **internal engineering objectives for Shiori**.

They are not commercial promises.

---

## 1.2 Service Level Indicator — SLI

A **Service Level Indicator (SLI)** is a measured value that describes the behavior of a system or capability.

An SLI answers:

> **What are we measuring?**

Examples used by Shiori include:

```text
HTTP request latency
HTTP successful-request ratio
HTTP server-error ratio
Dependency failure ratio
Outbox age
Projection lag
```

For API latency, Shiori uses percentile-based indicators rather than only an arithmetic average.

The primary percentiles are:

```text
p50
p95
p99
```

Their meaning is:

- `p50` — 50% of measured requests complete at or below this latency.
- `p95` — 95% of measured requests complete at or below this latency.
- `p99` — 99% of measured requests complete at or below this latency.

Percentiles are preferred because an average can hide a small but important population of very slow requests.

---

## 1.3 Service Level Objective — SLO

A **Service Level Objective (SLO)** is the internal target applied to an SLI.

An SLO answers:

> **What level do we expect the measured indicator to achieve?**

Conceptually:

```text
SLI
Catalog Search p95 server latency

SLO
Catalog Search p95 <= defined latency budget
```

SLOs are engineering targets used to:

- Detect regressions.
- Define load-test pass/fail criteria.
- Drive alerts and dashboards later in STEP 8.
- Prevent performance or availability from becoming subjective.
- Make architecture tradeoffs explicit.

Failure to meet an SLO is an engineering signal that requires investigation.

It does not automatically mean Shiori has violated a customer contract.

---

## 1.4 Service Level Agreement — SLA

A **Service Level Agreement (SLA)** is an external or contractual commitment made to a customer or other party.

An SLA may include consequences when the promised level is not achieved.

Shiori does **not** define a commercial SLA at the current product stage.

Therefore:

```text
SLI = measurement
SLO = internal engineering target
SLA = external contractual promise
```

For the MVP architecture process:

```text
SLIs: YES
SLOs: YES
Commercial SLA: NO
```

The absence of an SLA does not weaken the internal SLOs.

It only means Shiori is not converting those internal engineering objectives into contractual uptime or latency guarantees.

---

## 1.5 Measurement Scope

Unless an endpoint-specific requirement says otherwise, public HTTP latency is measured **server-side across the Shiori public backend path**:

```text
YARP Gateway receives the valid request
            |
            v
required Shiori backend processing
            |
            v
Gateway completes the HTTP response
```

The latency SLI therefore includes relevant Shiori-owned synchronous work such as:

- YARP routing and edge middleware.
- Authentication/authorization processing in the called backend.
- Service Application/Domain processing.
- Required PostgreSQL or MongoDB operations.
- Profile BFF synchronous dependency calls when that endpoint is being measured.

The latency SLI does **not** include:

- Internet latency between the end-user device and the Shiori edge.
- Client rendering time.
- Client-side JavaScript execution time.
- Time spent by asynchronous RabbitMQ consumers after the HTTP request has already completed.
- External-provider synchronization that is intentionally outside the normal read/write request path.

For file uploads, raw client upload-transfer time is not used as the server processing latency target because it depends heavily on the user's connection and the final approved upload-size limit.

The asynchronous-job acceptance budget begins once the request payload required for basic validation is available to the server.

---

## 1.6 Measurement Validity

Latency SLO evaluation applies to valid requests under supported operating conditions and expected load.

Expected client-caused outcomes such as these are not treated as service availability failures:

```text
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
other explicitly documented client/precondition failures
```

Server-side dependency failures, timeouts, and unexpected `5xx` responses are availability/reliability signals and must not be hidden by excluding them from measurement.

Cold-start, deployment, and recovery behavior may be measured separately from steady-state latency, but deployment strategy must not use that distinction to hide persistent production latency regressions.

---

# 2. Performance — API Latency Classes

## 2.1 Performance Principle

Shiori does not use one universal latency target for every operation.

The architecture intentionally contains different workload classes:

```text
Fast local reads
Transactional writes
Long-running asynchronous workflows
```

A cached Catalog read and a four-thousand-entry import are not equivalent operations and must not be judged by the same latency budget.

The synchronous request path should remain short wherever the user's current interaction depends on an immediate result.

Long-running work must be converted into durable asynchronous state instead of keeping the original HTTP request open.

---

## 2.2 Latency Class A — Fast Local Reads

### Scope

Class A applies to read operations expected to complete from Shiori-owned local state without live external-provider calls.

Representative operations include:

```text
Catalog Search
Catalog Item read
Franchise read
Library read paths backed by local service-owned state
```

Normal Catalog search/detail requests are expected to use Shiori's MongoDB state rather than calling AniList or MangaDex synchronously.

### Initial MVP SLO

| Percentile | Latency budget |
|---|---:|
| `p50` | `<= 100 ms` |
| `p95` | `<= 250 ms` |
| `p99` | `<= 500 ms` |

These are **server-side end-to-end Shiori latency budgets through YARP**, excluding end-user network latency.

### Requirement

A Class A endpoint MUST NOT introduce a live AniList or MangaDex request merely to satisfy an ordinary cached/canonical read.

If an operation requires explicit provider hydration because the required data does not exist locally, that operation is not allowed to silently consume the Class A latency budget by blocking indefinitely on the provider.

The final provider-hydration behavior remains governed by its explicit API/workflow contract.

### Performance intent

Class A is the main "feels immediate" read class.

A regression where p50 remains fast while p99 grows beyond the budget is considered a performance problem even if the arithmetic average still appears acceptable.

---

## 2.3 Latency Class B — Transactional Writes

### Scope

Class B applies to user-facing mutations whose success depends on one bounded context making a durable local decision.

Representative operations include:

```text
Tracking progress update
Tracking status update
Progress Vault undo
other bounded Tracking mutations with similar transaction shape
```

The canonical Tracking mutation path may include, inside one local PostgreSQL transaction:

```text
current authoritative state
+
immutable progress history
+
revision / optimistic-concurrency state
+
idempotency state when required
+
Tracking Outbox record when required
```

It does not wait for RabbitMQ consumers to process the resulting message.

### Initial MVP SLO

| Percentile | Latency budget |
|---|---:|
| `p50` | `<= 150 ms` |
| `p95` | `<= 400 ms` |
| `p99` | `<= 800 ms` |

These targets intentionally give a transactional write more budget than a simple local read while still keeping the mutation interactive.

### Requirement

A normal progress mutation MUST NOT synchronously call Catalog.

Required Catalog facts for the write path are read from Tracking's local projection.

RabbitMQ publication MUST occur after the local durable commit through the Outbox publisher.

Therefore, broker publication time and downstream consumer processing time are not part of the synchronous progress-write latency budget.

### Atomicity over raw speed

A write is not considered successful merely because it is fast.

If any state required by the accepted local transaction cannot commit consistently, the mutation must fail and roll back rather than return a fast partial success.

Performance optimization MUST NOT weaken:

- Progress-history capture.
- Optimistic concurrency.
- Required idempotency.
- Required Outbox persistence.

---

## 2.4 Latency Class C — Heavy Asynchronous Work

### Scope

Class C applies to workflows whose total execution time is intentionally decoupled from the original HTTP request.

The canonical example is Smart Staging Import.

The user-visible pattern is:

```text
Client
  |
  | POST / import workflow
  v
YARP
  |
  v
Tracking
  |
  | create durable job / staging state
  v
202 Accepted

HTTP request ends

        later
          |
          v
RabbitMQ / Workers / staging / hydration / commit
```

### HTTP acceptance SLO

For the server-side job-registration/acceptance phase, after the payload required for basic validation is available to Shiori:

| Percentile | Latency budget |
|---|---:|
| `p50` | `<= 200 ms` |
| `p95` | `<= 500 ms` |
| `p99` | `<= 1,000 ms` |

### Total job duration

The complete Import workflow does **not** receive a single fixed end-to-end latency SLO.

Total duration can legitimately vary based on factors that are intentionally outside this first NFR slice, including:

- Number of imported records.
- Number of unresolved Catalog identifiers.
- Required Catalog hydration.
- Provider rate limiting.
- Worker backlog.
- Bounded commit-batch behavior.

The product requirement is instead:

> **The original HTTP request must complete after durable acceptance. The client observes the long-running workflow through durable job state rather than through a long-lived Gateway connection.**

The exact import processing-time expectations belong to the later Import/Capacity/Operational-Limits NFR sections after file-size, batch-size, and concurrency limits are finalized.

---

## 2.5 Performance Budget Summary

| Class | Representative operation | `p50` | `p95` | `p99` | Synchronous completion meaning |
|---|---|---:|---:|---:|---|
| **A — Fast Local Read** | Catalog Search / cached Catalog read | `<= 100 ms` | `<= 250 ms` | `<= 500 ms` | Requested representation is returned from Shiori-owned state. |
| **B — Transactional Write** | Progress Update / Undo | `<= 150 ms` | `<= 400 ms` | `<= 800 ms` | Required local business transaction has durably committed. |
| **C — Async Acceptance** | Import job creation / accepted async work | `<= 200 ms` | `<= 500 ms` | `<= 1,000 ms` | Durable asynchronous job has been accepted; background completion is not implied. |

These values are **initial MVP engineering SLOs**, not commercial SLAs.

They MUST be validated in production-like staging with representative data volume and concurrency before MVP launch.

A future evidence-based revision is allowed, but an SLO must not be weakened merely to make a failing implementation appear compliant.

---

# 3. Availability & Degradation

## 3.1 Availability Principle

Shiori is intentionally designed so that one failed dependency does not automatically create a total-platform outage.

The availability objective is capability-based rather than "everything or nothing."

Examples:

```text
AniList unavailable
!=
Tracking unavailable

RabbitMQ unavailable
!=
local Tracking transaction unavailable

Tracking unavailable
!=
Identity unavailable
```

A capability may be:

```text
AVAILABLE
DEGRADED
UNAVAILABLE
```

without forcing every other capability into the same state.

Degradation MUST remain explicit and truthful.

Shiori must not present stale or incomplete information as if it were fresh and complete when the architecture knows otherwise.

---

## 3.2 Core Public API Availability SLO

The initial MVP availability target for **core public API capabilities under Shiori's control** is:

```text
99.9% successful availability per calendar month
```

This target applies independently to the major user-facing capability families, measured through the public YARP path:

```text
Identity core account/token operations
Catalog local/canonical read operations
Tracking library/progress read operations
Tracking progress mutation operations
Shareable Profile endpoint behavior
```

The SLO is evaluated at the capability/endpoint-family level rather than by averaging unrelated services into one platform-wide number.

For example, an AniList outage must not reduce the measured availability of Tracking progress writes when Tracking continues to satisfy its own contract successfully.

### What counts as available

A valid request counts as available when Shiori returns the success behavior defined by that endpoint contract.

A deliberately documented degraded-success response also counts as available when degraded behavior is part of the accepted contract.

Example:

```text
Public profile
Identity succeeds and confirms Public
Tracking unavailable
        |
        v
200 degraded Identity-only profile
Tracking sections omitted
```

This response is considered **available but degraded**, because that exact failure behavior is part of the accepted Profile BFF architecture.

The degraded condition MUST still be measured separately so that a high degraded-response rate cannot be hidden inside the top-level availability percentage.

### What does not count as available

Unexpected server failures, dependency timeouts that prevent the endpoint from satisfying its safe contract, and unexpected `5xx` outcomes count against the availability SLI.

Expected client-caused `4xx` outcomes do not count as service unavailability.

No commercial uptime SLA is created by this SLO.

---

## 3.3 External Provider Availability Is Not Core API Availability

AniList and MangaDex are external systems outside Shiori's operational control.

Shiori does not define an availability SLO for those providers.

Instead, Shiori defines how its own capabilities behave when a provider becomes unavailable.

The architectural objective is:

> **Provider outage = provider-ingestion degradation, not automatic platform outage.**

Provider availability and provider synchronization freshness must later be monitored as separate dependency SLIs.

---

## 3.4 Degradation Matrix

| Failure | Capability | Required behavior | Availability state |
|---|---|---|---|
| **AniList unavailable** | Catalog background ingestion/synchronization | Stop treating live provider access as available; use bounded resilience policy later defined in STEP 8; preserve last valid canonical Catalog state. | **Degraded** |
| **AniList unavailable** | Existing Catalog Search / Catalog detail backed by local MongoDB state | Continue serving existing canonical/cached Shiori data without requiring a live AniList call. | **Available** |
| **AniList unavailable** | Tracking progress reads/writes | Continue using Tracking-owned state and local Catalog projections. No direct AniList dependency is allowed. | **Available** |
| **AniList unavailable** | Data that is not local and requires explicit hydration | Do not invent metadata or silently claim freshness. Exact hydration failure contract remains defined by its owning API/workflow policy. | **Degraded / capability-specific** |
| **Identity unavailable** | New login, refresh, recovery, token-lifecycle operations that require Identity | These operations may be unavailable because Identity owns them. | **Unavailable for affected Identity capability** |
| **Identity unavailable** | Protected Catalog/Tracking request with an already valid access token | Downstream services may continue local JWT validation when required signing/discovery material remains safely available through normal caching. No synchronous Identity call is required per protected request. | **Potentially Available** |
| **Identity unavailable / timeout / unsafe policy result** | Profile BFF public-profile composition | **Fail Closed.** The BFF must not query or expose Tracking public-profile data when Identity cannot safely establish profile eligibility. | **Unavailable for composed public profile; privacy preserved** |
| **Identity confirms Public; Tracking unavailable** | Profile BFF public-profile composition | Return the already accepted degraded `200` Identity-only public profile and omit Tracking sections. | **Available but Degraded** |
| **RabbitMQ unavailable** | Local Catalog mutation whose required canonical data + Outbox can commit locally | Commit local authoritative state and durable Outbox state. Do not make broker availability part of the local transaction. | **Available for local mutation** |
| **RabbitMQ unavailable** | Local Tracking mutation whose required state + history + Outbox can commit locally | Commit the local Tracking transaction. A normal Progress Update returns its normal successful HTTP result (for example `200 OK` when that endpoint contract returns the updated representation). | **Available for local mutation** |
| **RabbitMQ unavailable** | Integration-event propagation | Publication pauses; pending Outbox records remain durable and retryable. | **Degraded** |
| **RabbitMQ unavailable** | Tracking Catalog projection freshness | Projection may become stale because new Catalog events cannot arrive. Existing projected state remains usable only according to its accepted stale/degraded semantics. | **Degraded** |
| **RabbitMQ unavailable** | Import hydration and other broker-dependent background workflows | Workflow may pause or remain pending until broker delivery resumes. Durable job state must not be falsely marked complete. | **Degraded / Paused** |
| **RabbitMQ recovers** | Outbox publication and consumers | Resume publication/consumption from durable state and converge without reconstructing business facts from process memory. | **Recovering → Available** |

---

## 3.5 AniList Failure Rules

AniList is part of Catalog ingestion, not the normal user-facing Tracking write path.

Therefore the following rules are normative:

1. Tracking MUST NOT call AniList directly.
2. Normal Catalog reads SHOULD use Shiori-owned local canonical/cached state.
3. An AniList outage MUST NOT automatically make ordinary Tracking progress mutations unavailable.
4. Catalog synchronization may become stale while AniList is unavailable.
5. Shiori MUST NOT fabricate provider data that was not successfully obtained and normalized.
6. If local Catalog data exists, the outage should be represented primarily as a freshness/synchronization degradation rather than as total Catalog read failure.
7. Behavior for data requiring first-time or explicit hydration remains capability-specific and must not be invented implicitly by the NFR layer.

---

## 3.6 Identity Failure Rules

Identity is security-critical because it owns:

- Registration.
- Login.
- Refresh-token lifecycle.
- Revocation.
- Recovery.
- Public-profile eligibility/visibility.

The architecture also deliberately avoids a synchronous Identity request for every protected Catalog or Tracking API call.

Therefore:

1. Existing valid access tokens may continue to be validated locally by protected services when safe signing/discovery key material remains available through the configured validation cache.
2. New authentication/token lifecycle operations that require Identity may be unavailable while Identity is down.
3. The Profile BFF MUST evaluate Identity first.
4. If Identity is unavailable, times out, returns an unsupported visibility value, or otherwise cannot provide a safe authorization result, the Profile BFF MUST **fail closed**.
5. Fail-closed behavior means **no Tracking public-profile data is fetched or exposed** as a fallback.
6. A client-supplied claim that a profile is public MUST NOT override the Identity failure.
7. If Identity has safely confirmed `Public` but Tracking later fails, the BFF MAY return the already accepted degraded Identity-only `200` profile with Tracking sections omitted.

This distinction is intentional:

```text
Identity unknown
        -> privacy authority unavailable
        -> FAIL CLOSED

Identity says Public
Tracking unavailable
        -> privacy decision is known
        -> safe degraded read is possible
```

---

## 3.7 RabbitMQ Failure Rules

RabbitMQ is not part of the local database transaction for Catalog or Tracking business state.

The accepted model is:

```text
business state
+
required Outbox record
        |
        | same local durable decision
        v
COMMIT
        |
        v
HTTP success

later
        |
        v
Outbox Publisher
        |
        v
RabbitMQ
```

Therefore:

1. A RabbitMQ outage MUST NOT cause an already valid local Catalog or Tracking mutation to fail solely because the broker cannot accept the message after the Outbox record has been durably committed.
2. The HTTP request MUST report the result of the local business transaction, not the state of downstream consumers.
3. For a progress-update endpoint whose successful contract returns `200 OK`, a successful local transaction remains `200 OK` even while RabbitMQ publication is temporarily unavailable.
4. Pending integration work MUST remain represented by durable Outbox state.
5. The implementation MUST NOT switch to best-effort in-memory publication as a fallback.
6. Integration propagation, projection freshness, and broker-dependent background workflows are explicitly **degraded** while RabbitMQ is unavailable.
7. Release Intelligence MUST NOT claim that Tracking has received a newer Catalog release fact when that event has not yet reached the local projection.
8. Once RabbitMQ recovers, Outbox publishers and consumers must resume and converge through the existing idempotent/versioned message-processing model.

RabbitMQ degradation therefore preserves local durability while sacrificing asynchronous freshness and throughput temporarily.

It does **not** mean "everything is healthy."

---

## 3.8 Availability Measurement During Degraded Modes

Degraded behavior must be observable separately from binary availability.

Conceptually, the later observability section must be able to distinguish:

```text
successful_full
successful_degraded
failed
```

Examples:

```text
Profile BFF full public profile
    -> successful_full

Profile BFF Identity-only public profile
    -> successful_degraded

Profile BFF cannot safely evaluate Identity
    -> failed / fail-closed

Tracking progress update while RabbitMQ is down,
local transaction succeeds
    -> successful_full for the Tracking HTTP mutation
       + messaging subsystem degraded
```

This distinction prevents a healthy HTTP success rate from hiding a prolonged messaging or dependency incident.

---

---

# 4. Resilience — Timeouts, Retries & Circuit Breakers

## 4.1 Scope

This section defines the baseline resilience policy for outbound Catalog integration with:

```text
AniList
MangaDex
```

Only the Catalog bounded context may call these providers directly.

The policy applies to provider-backed background work such as:

- Scheduled Catalog synchronization.
- Catalog hydration initiated by an approved asynchronous workflow.
- Manga/MangaDex publication-unit enrichment.
- Provider refresh operations.

It does **not** place AniList or MangaDex inside normal user-facing Catalog reads or Tracking progress writes.

Normal Catalog reads remain local to Shiori-owned MongoDB state, and normal Tracking writes remain local to Tracking-owned state/projections.

---

## 4.2 Resilience Objectives

Provider resilience has four objectives:

1. **Bound waiting time.** A Worker must never wait indefinitely for a provider.
2. **Retry only failures that may actually recover.** Invalid requests must not be retried as if they were transient outages.
3. **Avoid retry storms.** Multiple Shiori layers must not independently retry the same provider operation.
4. **Stop hammering an unhealthy provider.** Repeated transient failures must open a Circuit Breaker and allow the provider time to recover.

The baseline policy is intentionally conservative for an MVP and may be tightened later from production evidence.

It must not be loosened merely to hide an unhealthy provider or slow integration path.

---

## 4.3 Provider Request Timeout

Every individual AniList or MangaDex network attempt has a strict timeout of:

```text
3 seconds per attempt
```

The timeout applies to the provider HTTP operation performed by the Catalog provider adapter.

If the provider does not produce a successful response within the allowed attempt window, the attempt is treated as a transient timeout failure.

### Normative rule

```text
Provider attempt timeout = 3 seconds
```

No Catalog provider call may use an unbounded/default infinite timeout.

A provider timeout does not delete or replace the last valid canonical Catalog data.

A timeout means:

```text
This synchronization attempt did not succeed.
```

It does not mean:

```text
The previously stored Shiori Catalog fact is now false.
```

---

## 4.4 Retry Eligibility

Retries are allowed only for failures that are reasonably transient.

### Retryable failures

The Catalog provider adapter MAY retry:

```text
network connection failure
request timeout
HTTP 408 Request Timeout
HTTP 429 Too Many Requests
HTTP 500 Internal Server Error
HTTP 502 Bad Gateway
HTTP 503 Service Unavailable
HTTP 504 Gateway Timeout
```

### Non-retryable failures

The adapter MUST NOT automatically retry ordinary deterministic client/contract failures such as:

```text
HTTP 400 Bad Request
HTTP 401 Unauthorized
HTTP 403 Forbidden
HTTP 404 Not Found
other non-transient 4xx responses
provider payload/schema validation failure caused by a deterministic incompatible response
```

A deterministic mapping or contract problem must become visible through logs/metrics and follow its normal failure workflow instead of being converted into repeated provider traffic.

---

## 4.5 Maximum Retry Count

For one logical provider operation, Shiori allows:

```text
Initial attempt: 1
Retries:         maximum 2
Total attempts:  maximum 3
```

Therefore:

> **A single logical provider operation may perform no more than three immediate network attempts before yielding failure/degraded state to the owning background workflow.**

This limit prevents one failing provider request from occupying a Worker indefinitely.

A later scheduled execution or durable workflow retry is a separate operation and does not justify an unbounded immediate retry loop.

---

## 4.6 Exponential Backoff with Jitter

Immediate retries use exponential backoff.

Baseline schedule:

```text
Attempt 1
    |
    | failure
    v
wait ~500 ms + jitter
    |
    v
Attempt 2
    |
    | failure
    v
wait ~1,000 ms + jitter
    |
    v
Attempt 3
```

Jitter MUST be applied so multiple Workers do not synchronize into the same retry pattern.

The baseline jitter window is:

```text
0–250 ms
```

Therefore the nominal immediate retry delays are:

| Retry | Base delay | Jitter |
|---|---:|---:|
| First retry | `500 ms` | `0–250 ms` |
| Second retry | `1,000 ms` | `0–250 ms` |

No fourth immediate attempt is allowed by the baseline policy.

---

## 4.7 `Retry-After` and Provider Rate Limits

When a provider returns `429 Too Many Requests` or another response with a valid `Retry-After` instruction, Shiori MUST respect the provider's requested delay when doing so is compatible with the durable background workflow.

The Worker MUST NOT busy-wait or sleep for a long provider-directed delay while holding scarce processing capacity.

If the requested wait is greater than:

```text
5 seconds
```

then the preferred behavior is:

```text
record provider throttling
        |
        v
persist/reschedule durable work
        |
        v
release the current Worker execution
        |
        v
retry later
```

rather than keeping the Worker blocked for the entire provider cooldown.

A provider-specific official policy that is stricter than this baseline takes precedence.

---

## 4.8 Retry Ownership

Provider retries are owned by the **Catalog provider integration layer**.

They MUST NOT be independently multiplied by:

```text
YARP retry
    x
Catalog Application retry
    x
Worker retry
    x
HTTP client retry
```

The same network operation must have one clearly defined immediate-retry owner.

Higher-level durable workflow retries are allowed only after the immediate provider retry policy has completed and control has returned to the owning workflow.

This distinction prevents retry amplification.

---

## 4.9 Circuit Breaker — Isolation per Provider

AniList and MangaDex use independent Circuit Breakers.

A failure in MangaDex must not open AniList's breaker, and an AniList outage must not suppress unrelated MangaDex work.

Baseline state model:

```text
CLOSED
  |
  | repeated transient failures
  v
OPEN
  |
  | cooldown expires
  v
HALF-OPEN
  |
  +-- successful probes --> CLOSED
  |
  +-- transient failure --> OPEN
```

---

## 4.10 Circuit Breaker Threshold

The baseline breaker opens after:

```text
5 consecutive retryable provider-operation failures
```

for the same provider.

A logical provider operation counts as one failure **after** its allowed immediate retry policy has been exhausted.

For example:

```text
Logical operation A
  attempt 1 -> fail
  attempt 2 -> fail
  attempt 3 -> fail
  => breaker failure #1
```

It does **not** count as three separate breaker failures.

Non-retryable business/contract failures do not increment the transient-failure breaker unless they indicate a provider-wide transport/service failure.

---

## 4.11 Circuit Open Duration

When the breaker opens, outbound calls to that provider are rejected/deferred for:

```text
30 seconds
```

During the open interval:

- Background provider work remains degraded/pending.
- Last valid canonical Catalog data is preserved.
- User-facing normal Catalog reads continue from local state when that state exists.
- Tracking remains independent from the provider outage.
- Shiori does not fabricate provider data.

After 30 seconds, the breaker enters Half-Open.

---

## 4.12 Half-Open Recovery

Half-Open permits only one provider probe at a time.

The provider must produce:

```text
2 consecutive successful probe operations
```

before the breaker returns fully to Closed.

Any retryable failure during Half-Open immediately reopens the breaker for another 30-second cooldown.

This avoids restoring full outbound concurrency after a single lucky response from an unstable provider.

---

## 4.13 Provider Resilience Baseline Summary

| Policy | Baseline |
|---|---:|
| Per-attempt timeout | `3 s` |
| Maximum immediate retries | `2` |
| Maximum total attempts | `3` |
| First retry delay | `500 ms + 0–250 ms jitter` |
| Second retry delay | `1,000 ms + 0–250 ms jitter` |
| Long `Retry-After` threshold | `> 5 s` → durable defer/reschedule |
| Circuit threshold | `5 consecutive exhausted logical failures` |
| Circuit Open duration | `30 s` |
| Half-Open concurrency | `1 probe at a time` |
| Circuit recovery | `2 consecutive successful probes` |

These values are initial MVP operational requirements.

They MUST be verified in resilience tests that simulate:

- Provider timeout.
- `429` throttling.
- `500` / `502` / `503` / `504` responses.
- Recovery after Circuit Open.
- Multiple Workers encountering the same provider outage.

---

## 4.14 Resilience Guardrails

The following rules are normative:

1. Only Catalog calls AniList or MangaDex directly.
2. Normal Catalog reads do not synchronously depend on provider availability.
3. Tracking never calls AniList or MangaDex.
4. Every provider attempt has a finite timeout.
5. Retries are bounded.
6. Exponential backoff includes jitter.
7. Deterministic client/contract failures are not retried blindly.
8. `Retry-After` is respected without needlessly blocking Worker capacity.
9. Circuit Breakers are isolated per provider.
10. Circuit Open preserves last valid canonical data; it does not replace it with empty/guessed state.
11. Provider failure is observable through metrics/logs/traces.
12. A later implementation may use a standard .NET resilience library, but the library choice does not change these behavioral requirements.

---

# 5. Data Durability & Recovery — RPO / RTO

## 5.1 Durability Principle

Shiori distinguishes **canonical business data** from **derived/rebuildable state**.

The following service-owned databases are canonical:

```text
Identity PostgreSQL
Catalog MongoDB
Tracking PostgreSQL
```

They are not disposable caches.

A successful write to one of these canonical stores represents a Shiori-owned business fact that must be protected according to the recovery targets in this section.

---

## 5.2 Canonical Data Classification

### Identity PostgreSQL — Canonical

Identity PostgreSQL contains authoritative Identity-owned state such as:

- Stable Shiori user identity.
- Account and credential state.
- Public profile state.
- Profile visibility.
- OpenIddict persistence/token lifecycle state where applicable.

Loss of Identity data may prevent users from accessing their accounts or may destroy identity/security state.

Therefore Identity data is classified as:

```text
CANONICAL — HIGH DURABILITY REQUIREMENT
```

### Tracking PostgreSQL — Canonical

Tracking PostgreSQL contains authoritative Tracking-owned state such as:

- User library relationships.
- Current progress.
- Immutable progress history.
- Ratings and consumption dates.
- List/privacy state owned by Tracking.
- Import job/staging state while it is part of an active durable workflow.
- Tracking Outbox/Inbox/idempotency state required for correctness.

User progress is one of Shiori's central product promises.

Therefore Tracking data is classified as:

```text
CANONICAL — HIGH DURABILITY REQUIREMENT
```

### Catalog MongoDB — Canonical

Catalog MongoDB contains Shiori's canonical normalized entertainment model, including Shiori-owned identifiers and normalized provider-derived state.

Although portions of provider-backed metadata may be re-hydratable from AniList or MangaDex, Catalog MongoDB MUST NOT be treated as a disposable cache because Shiori also owns:

- Canonical Shiori identifiers.
- Normalized Catalog state.
- Franchise grouping/relationships represented by Shiori.
- Synchronization/provenance state.
- Other Catalog-owned decisions that may not be safely reproduced by simply calling a provider again.

Therefore Catalog data is classified as:

```text
CANONICAL — DURABLE, PARTIALLY REHYDRATABLE BUT NOT DISPOSABLE
```

---

## 5.3 Derived / Rebuildable Data

Examples of state that may be rebuilt/reconciled from canonical owners include:

```text
Tracking local Catalog projections
Catalog derived summaries
future explicitly rebuildable read models
in-memory caches
```

Derived state does not receive the same recovery priority as canonical business data when it can be reconstructed safely.

However, rebuildability must be real and tested.

A table or document is not "rebuildable" merely because the team hopes it can be recreated later.

---

## 5.4 Recovery Point Objective — RPO

**RPO** answers:

> **After a catastrophic datastore-loss event, how much already committed canonical data may Shiori lose at most?**

The initial MVP recovery targets are:

| Canonical store | RPO |
|---|---:|
| **Identity PostgreSQL** | `<= 5 minutes` |
| **Tracking PostgreSQL** | `<= 5 minutes` |
| **Catalog MongoDB** | `<= 15 minutes` |

### Rationale

Identity and Tracking receive the strictest target because they contain user identity, security/account state, user progress, and historical tracking information that may be impossible to reconstruct from another source.

Catalog receives a slightly wider target because part of its provider-derived metadata can be rehydrated; however, it remains canonical Shiori data and therefore still requires a low RPO and tested backup/recovery path.

These targets are internal engineering objectives, not commercial SLAs.

The backup/replication implementation selected later MUST be capable of recovering to a point inside these windows under the supported production topology.

---

## 5.5 Recovery Time Objective — RTO

**RTO** answers:

> **After a catastrophic datastore-loss event is declared, how long may Shiori take to restore the affected canonical capability to a verified serviceable state?**

Initial MVP targets:

| Canonical store | RTO |
|---|---:|
| **Identity PostgreSQL** | `<= 60 minutes` |
| **Tracking PostgreSQL** | `<= 60 minutes` |
| **Catalog MongoDB** | `<= 120 minutes` |

A datastore is not considered recovered merely because the database process has started.

Recovery means the owning service can safely use the restored state and pass the required integrity/smoke verification.

---

## 5.6 What RPO/RTO Do Not Mean

The targets above do not require a specific cloud, database vendor, backup product, Kubernetes cluster, or replication topology.

They define the outcome that the eventual deployment strategy must be capable of meeting.

Therefore:

```text
RPO/RTO requirement
        !=
pre-approval of a specific infrastructure product
```

If the selected production infrastructure cannot meet these objectives, the deployment design must be revised or the target must be explicitly re-reviewed before launch.

The target must not be silently changed by implementation convenience.

---

## 5.7 Backup Requirement

Every canonical datastore MUST have a documented backup mechanism before production launch.

The mechanism must support recovery inside the defined RPO.

Backups must be:

- Stored independently enough that loss of the live database does not automatically destroy the only recovery copy.
- Access-controlled using least privilege.
- Monitored for successful completion/failure.
- Retained according to the later data-retention policy.
- Compatible with the database/schema versions they are expected to restore.

The exact retention duration is intentionally deferred to the dedicated retention section of STEP 8.

---

## 5.8 A Backup Is Not Considered Valid Until Restore Is Tested

The existence of a backup file, snapshot, or provider status page is not sufficient evidence of recoverability.

The normative rule is:

> **Shiori must prove that canonical backups can actually be restored.**

A restore test MUST recover the backup into an isolated environment and verify that the owning service can safely use the restored data.

The test must not overwrite or experiment against the live production datastore.

---

## 5.9 Restore Test Frequency

For each canonical datastore:

```text
Identity PostgreSQL
Catalog MongoDB
Tracking PostgreSQL
```

Shiori requires:

```text
At least 1 successful restore test per calendar month
```

In addition, a new restore test is required after a material change to:

- Backup technology/configuration.
- Database topology.
- Restore procedure.
- Encryption/access mechanism that affects restore.
- Major persistence migration strategy when that change could affect recoverability.

Before MVP launch, all three canonical datastores MUST have at least one successful full restore test from production-like backups in a production-like isolated environment.

---

## 5.10 Restore Test Acceptance Criteria

A restore test passes only when all applicable checks succeed:

1. A specific backup/recovery point is selected.
2. Restore starts from documented procedure rather than undocumented operator knowledge.
3. The datastore restores without corruption/fatal restore errors.
4. The owning service can connect using the restored datastore in the isolated environment.
5. Required migrations/schema/validators/indexes are compatible with the restored state.
6. Representative canonical records can be read correctly.
7. A controlled representative write can be committed where safe for the isolated test.
8. Required service health/smoke checks pass.
9. The achieved recovery point is measured against the RPO.
10. The elapsed recovery time is measured against the RTO.
11. The result is recorded as PASS or FAIL with evidence.

A restore test that exceeds the target is still useful evidence, but it is an NFR failure requiring remediation or explicit architecture review.

---

## 5.11 RabbitMQ and Outbox Durability

RabbitMQ is important infrastructure, but it is not the canonical source of Shiori business state.

The canonical durability rule remains:

```text
business mutation
+
required Outbox record
        |
        v
same local atomic decision
```

Therefore, loss or unavailability of RabbitMQ must not erase a canonical business fact that already committed with its required Outbox intent.

RabbitMQ recovery/HA topology will be defined separately, but the broker MUST NOT be treated as the only durable copy of an unpublished business fact.

Likewise, RabbitMQ is not Shiori's historical event store.

---

## 5.12 Durability / Recovery Summary

| Data | Classification | RPO | RTO | Restore test |
|---|---|---:|---:|---|
| Identity PostgreSQL | Canonical / high durability | `<= 5 min` | `<= 60 min` | Monthly + before MVP |
| Tracking PostgreSQL | Canonical / high durability | `<= 5 min` | `<= 60 min` | Monthly + before MVP |
| Catalog MongoDB | Canonical / partially rehydratable, not disposable | `<= 15 min` | `<= 120 min` | Monthly + before MVP |
| Tracking Catalog projection | Derived/rebuildable | No independent canonical RPO | Rebuild/reconcile | Rebuild test defined later |
| Catalog derived summaries | Derived/rebuildable | No independent canonical RPO | Recompute | Recompute test defined by Catalog |
| RabbitMQ messages | Transport/durable messaging infrastructure | Not canonical business RPO | Broker recovery policy later | Covered by resilience/HA tests later |

---

## 5.13 Data Durability Guardrails

1. Identity PostgreSQL, Catalog MongoDB, and Tracking PostgreSQL are canonical stores.
2. Canonical stores MUST NOT be treated as disposable caches.
3. Tracking progress/history loss is a high-severity data-integrity failure.
4. Provider rehydration does not replace Catalog backup/restore requirements.
5. RPO/RTO are measured recovery objectives, not documentation-only statements.
6. Backup success without restore evidence is insufficient.
7. Restore tests occur at least monthly for every canonical datastore.
8. Restore tests execute in isolated environments.
9. Recovery evidence records both achieved RPO and achieved RTO.
10. Derived state may use rebuild/reconciliation instead of canonical backup only when the rebuild path is explicit and verifiable.
11. RabbitMQ is not the source of truth for user progress, identity, or Catalog canonical state.
12. Required Outbox state protects asynchronous publication intent across broker outages.

---

# 6. Operational Limits — Imports & Public Requests

## 6.1 Purpose

Public request limits protect Shiori from:

- Accidental oversized clients.
- Malformed imports.
- Deliberate denial-of-service attempts.
- Excessive Gateway/service buffering.
- Large-object allocations and `OutOfMemoryException` risk.
- One expensive request consuming disproportionate public request capacity.

The limits in this section are **defensive boundaries**, not estimates of normal payload size.

Normal clients should remain well below them.

---

## 6.2 Important Import Contract Clarification

The currently approved MVP import architecture uses **uploaded supported list files**, especially MyAnimeList-compatible XML/import files.

It does not require clients to send the entire imported library as one giant JSON array through the API.

Therefore the primary limit is defined on the **import upload body/file**, not only on JSON serialization.

If a future JSON import format is introduced, the same import-body and item-count limits apply by default until this NFR is explicitly revised.

---

## 6.3 Default JSON Request Body Limit

For ordinary public API JSON requests that do not have an explicitly documented specialized upload contract:

```text
Maximum JSON request body = 256 KiB
```

Equivalent bytes:

```text
262,144 bytes
```

This is intentionally much larger than normal Tracking/Profile mutation payloads while still preventing arbitrary multi-megabyte JSON documents from reaching ordinary endpoints.

An endpoint requiring a larger body must define its own bounded contract rather than silently inheriting an effectively unlimited server default.

---

## 6.4 Import File Content Limit

The maximum accepted import file content is:

```text
5 MiB
```

Equivalent bytes:

```text
5,242,880 bytes
```

This limit applies to the uploaded import file itself.

The complete multipart request envelope, when multipart upload is used, may be slightly larger because of headers/boundary metadata.

Therefore the public import route has a total request-body ceiling of:

```text
6 MiB
```

Equivalent bytes:

```text
6,291,456 bytes
```

The `5 MiB` file limit remains authoritative for actual import content.

---

## 6.5 Maximum Items per Import

A single import job may contain at most:

```text
10,000 parsed list entries
```

This limit intentionally remains above the already approved scenario of imports containing approximately four thousand titles, while preventing one file from becoming an unbounded data-ingestion workload.

The item count is evaluated by the hardened import parser during the `Validating` / `Processing` stage.

If the file exceeds 10,000 parsed entries:

- Parsing stops as soon as the limit violation is known.
- The job does not proceed to normal matching/commit.
- The live library remains unchanged.
- The import job records a deterministic item-limit failure.
- Any partial staging produced before the limit was detected is cleaned according to the import cleanup policy.

The exact stable error code belongs to the Import API/Application contract and is not invented by this NFR section.

---

## 6.6 Gateway Enforcement

YARP MUST enforce public request-size limits as early as practical.

For import uploads, the Gateway must reject a request that is already known to exceed the total route limit before forwarding the complete body downstream.

For known oversized HTTP bodies, the public response follows the established API convention for:

```text
413 Content Too Large
```

The Gateway must not fully buffer a multi-megabyte import file in memory merely to discover later that it exceeds the limit.

The preferred request path remains:

```text
Client upload
     |
     v
YARP bounded/streamed request
     |
     v
Tracking secure temporary storage
     |
     v
202 Accepted after durable job creation
     |
     v
background parsing later
```

---

## 6.7 Chunked / Unknown-Length Requests

The absence of `Content-Length` does not bypass size enforcement.

If a request arrives using streaming/chunked transfer, Shiori must count received bytes and abort the request when the configured maximum is exceeded.

Therefore:

```text
No Content-Length
!=
unlimited body size
```

---

## 6.8 Defense in Depth — Service-Side Revalidation

The Gateway is not the only enforcement point.

Tracking MUST independently validate the import limits before treating a request as accepted work.

This protects the service if:

- Internal routing changes.
- A request reaches the service through a trusted internal test/deployment path.
- Gateway configuration drifts.
- A future topology introduces another approved ingress path.

The service-side limit may be equal to or stricter than the Gateway limit.

It must not be looser than the documented public contract.

---

## 6.9 Memory Safety Rule

Neither YARP nor Tracking may require loading the entire import file plus its fully materialized parsed object graph into public-request memory before returning the accepted job response.

The architecture remains:

```text
bounded upload
      |
      v
secure temporary file storage
      |
      v
durable import job
      |
      v
background streaming/batched parse
      |
      v
bounded staging batches
```

Heavy parsing and Catalog matching remain background work.

This rule is more important than any single file-size number because it prevents request-path memory growth from scaling linearly with the complete import workflow.

---

## 6.10 Parser Safety Remains Mandatory

Request-size limits do not replace XML/parser hardening.

A small malicious document can still be dangerous if the parser permits unsafe XML features or pathological expansion.

Therefore the existing architecture requirement remains:

- Unsafe XML external entity/DTD behavior is disabled according to the hardened parser configuration.
- Input is validated before it affects the live library.
- Parsing occurs in bounded background work.
- Untrusted import content never becomes trusted simply because it is below 5 MiB.

Detailed parser-security configuration belongs to implementation/security hardening, not to this numeric limits section.

---

## 6.11 Import Commit Remains Bounded

The 10,000-item acceptance ceiling does not allow one giant PostgreSQL transaction for the entire import.

After explicit user confirmation, Tracking continues to use:

```text
bounded idempotent local batches
+
durable batch progress/checkpoints
+
short finalization transaction
```

The exact commit batch size is intentionally not fixed in this document because it depends on measured PostgreSQL transaction cost, history generation, indexes, and load-test evidence.

It must be chosen later from performance testing rather than guessed here.

---

## 6.12 Operational Limit Summary

| Boundary | Limit |
|---|---:|
| Default ordinary JSON request | `256 KiB` / `262,144 bytes` |
| Import file content | `5 MiB` / `5,242,880 bytes` |
| Import HTTP request envelope | `6 MiB` / `6,291,456 bytes` |
| Parsed entries per import job | `10,000` |
| Import processing in original HTTP request | **Forbidden** beyond bounded validation/job acceptance |
| Full-body in-memory buffering as import design | **Forbidden** |
| Oversized known request | `413 Content Too Large` |

---

## 6.13 Operational Limit Guardrails

1. Ordinary JSON endpoints have a `256 KiB` default maximum body size.
2. Import file content is limited to `5 MiB`.
3. The import HTTP request envelope is limited to `6 MiB`.
4. One import job is limited to `10,000` parsed entries.
5. The already approved ~4,000-title import scenario remains inside the supported ceiling.
6. The Gateway enforces public request limits early.
7. Tracking revalidates limits independently.
8. Chunked transfer does not bypass byte limits.
9. Oversized requests are rejected before expensive parsing whenever size is knowable at the edge.
10. The Gateway does not parse XML.
11. The public request does not wait for the full import workflow.
12. Heavy import parsing happens in background processing.
13. File size limits do not replace parser hardening.
14. Item-count limits do not authorize one giant database transaction.
15. Exact import commit batch size remains a later measured implementation/NFR decision.

---

---

# 7. Messaging Health — RabbitMQ, Outbox, Inbox & DLQ

## 7.1 Purpose

Shiori uses RabbitMQ for durable asynchronous communication, but RabbitMQ is not the canonical source of business state.

The messaging path exists to move already-durable facts and capability requests between bounded contexts:

```text
Producer local transaction
        |
        +-- canonical state
        +-- Outbox record
        |
        v
Outbox publisher
        |
        v
RabbitMQ
        |
        v
Consumer
        |
        v
Inbox + consumer-owned local effect
```

The architecture assumes:

```text
at-least-once delivery
+
idempotent consumers
+
bounded eventual consistency
```

Therefore messaging health is not judged only by whether the RabbitMQ process answers a TCP connection.

Shiori must also observe whether durable work is actually moving through the complete path.

---

## 7.2 Messaging Health Signals

The minimum messaging-health signals are:

```text
broker connectivity
oldest ready message age
queue depth
consumer presence / processing activity
consumer success/failure/retry rate
oldest unpublished Outbox age
Outbox publication failures
Inbox processing failures
dead-letter count
projection lag where a projection depends on the queue
```

No single signal is sufficient by itself.

For example:

```text
RabbitMQ reachable
+
queue lag = 20 minutes
```

is not a healthy messaging system.

Likewise:

```text
queue depth = 0
+
Outbox contains unpublished records for 10 minutes
```

is not healthy merely because RabbitMQ currently has no queued messages.

---

## 7.3 Queue Lag Definition

For Shiori's durable asynchronous queues, **Queue Lag** is primarily measured as:

> **The age of the oldest message that is waiting to be processed by the intended consumer path.**

This is preferred over using queue depth alone because a queue containing 1,000 messages may be healthy if consumers clear them quickly, while a queue containing only one message may be unhealthy if that message has been waiting for ten minutes.

Conceptually:

```text
oldest queued message timestamp
        |
        v
current time
        |
        v
Queue Lag
```

Queue depth remains an important capacity signal, but there is no single global queue-depth threshold in this NFR because different workloads have different throughput characteristics.

---

## 7.4 Queue Lag Thresholds

The initial MVP messaging-health thresholds are:

| Oldest message age | State | Required interpretation |
|---|---|---|
| `< 2 minutes` | **Healthy** | Normal bounded asynchronous delay. |
| `>= 2 min and < 5 min` | **Degraded** | Investigate consumer slowdown/backlog growth; no incident is assumed yet. |
| `>= 5 minutes` | **Unhealthy / Alert** | Messaging convergence is outside the accepted normal window. Alert and investigate. |
| `>= 15 minutes` | **Critical** | Sustained asynchronous-delivery failure or severe backlog. Immediate operational response required. |

The primary alert threshold is therefore:

```text
Oldest queued message age >= 5 minutes
```

This threshold applies to normal integration/projection queues whose purpose is timely cross-service convergence.

A future workload whose business semantics legitimately require a different queue-lag budget must define that exception explicitly rather than silently weakening this baseline.

---

## 7.5 Projection Lag Is a Correctness Signal

Catalog → Tracking synchronization is eventually consistent by design.

However:

```text
eventual consistency
!=
indefinite divergence
```

If Catalog has committed a fact that Tracking requires in its local projection and the corresponding integration work remains unprocessed beyond the accepted queue-lag threshold, Shiori is operating in a degraded consistency state.

For projection-backed behavior, the system must be able to distinguish at minimum:

```text
healthy convergence
bounded delay
unhealthy lag
repair/reconciliation required
```

A stale projection that never converges is a correctness defect, not merely a performance issue.

---

## 7.6 Outbox Age Definition

**Outbox Age** is measured as:

> **The age of the oldest committed Outbox record that has not yet been successfully published through the intended broker path.**

The Outbox exists specifically so a successful local business transaction does not depend on RabbitMQ being available at commit time.

Therefore an increasing Outbox age means:

```text
business facts are safely committed locally
        |
        v
but asynchronous propagation is delayed
```

The public mutation does not become retroactively unsuccessful because publication is delayed.

The asynchronous subsystem becomes degraded instead.

---

## 7.7 Outbox Age Thresholds

The initial MVP thresholds are:

| Oldest unpublished Outbox age | State | Required action |
|---|---|---|
| `< 30 seconds` | **Healthy** | Normal publisher cadence / transient scheduling delay. |
| `>= 30 sec and < 2 min` | **Degraded** | Observe publisher/broker health and publication-failure rate. |
| `>= 2 minutes` | **Alert** | Outbox publication is outside the accepted normal window. Investigate. |
| `>= 5 minutes` | **Critical** | Sustained broker/publisher failure; asynchronous propagation is materially delayed. |

The maximum age before an operational alert is therefore:

```text
Oldest unpublished Outbox record >= 2 minutes
```

This threshold is intentionally stricter than the queue-lag threshold because an unpublished Outbox record has not yet entered the broker/consumer path at all.

---

## 7.8 RabbitMQ Outage Semantics

If RabbitMQ becomes unavailable while a Tracking mutation performs a valid local transaction:

```text
Tracking PostgreSQL
    current state
    + history
    + required Outbox
        |
        v
local commit succeeds
```

then the HTTP mutation retains its normal successful contract, such as `200 OK`, when all synchronous requirements of that endpoint have succeeded.

The system state becomes:

```text
Tracking API:          AVAILABLE
Canonical write:       COMMITTED
Outbox:                PENDING
RabbitMQ propagation:  DEGRADED
Remote consumers:      STALE UNTIL RECOVERY
```

Shiori must never return a successful HTTP result if the required local transaction itself failed.

Likewise, Shiori must never claim that remote propagation completed merely because the local mutation succeeded.

---

## 7.9 Consumer Health

A queue may be reachable while no useful consumer work is occurring.

Consumer health must therefore expose at least:

```text
messages processed
success count
failure count
retry count
processing duration
in-flight work
last successful processing time
```

A consumer that repeatedly receives and fails the same poison message must not block healthy traffic indefinitely.

Worker concurrency and broker prefetch remain bounded and workload-specific; this section does not invent one global consumer-count or prefetch value.

---

## 7.10 Dead Letter Queue — DLQ

Permanent failures and poison messages must be isolated from healthy traffic through the approved dead-letter mechanism.

The DLQ rule is:

> **A message reaches the DLQ only after the normal bounded retry policy has been exhausted or the failure has been classified as non-recoverable for automatic processing.**

The DLQ is not:

```text
normal backlog
long-term event storage
an automatic infinite retry loop
a substitute for fixing a consumer bug
```

---

## 7.11 DLQ Alerting

The expected steady-state DLQ count is:

```text
0
```

Therefore:

```text
Any newly dead-lettered message
        |
        v
Operational Alert
```

A DLQ message indicates that normal automated processing could not safely converge.

An increasing DLQ count is always unhealthy even if normal API traffic continues to succeed.

---

## 7.12 DLQ Replay Safety

Shiori MUST NOT automatically replay DLQ messages in an uncontrolled loop.

Before replay, the operator or approved operational procedure must establish at least one of the following:

- The root cause has been fixed.
- The transient dependency has recovered and the message is safe to retry.
- The message contract is now supported.
- The poison payload has been corrected through an explicit safe process.

Replay must preserve the existing integration-message identity and idempotency semantics where the replay represents the same logical message.

A replay must not generate a new business fact merely to bypass Inbox duplicate protection.

The exact command/tool used to perform replay is an implementation/operations choice and is intentionally not fixed here.

---

## 7.13 Messaging Health Summary

| Signal | Healthy | Alert | Critical |
|---|---:|---:|---:|
| Oldest queued message age | `< 2 min` | `>= 5 min` | `>= 15 min` |
| Oldest unpublished Outbox age | `< 30 sec` | `>= 2 min` | `>= 5 min` |
| New DLQ messages | `0` | `>= 1 new message` | Sustained/increasing DLQ backlog |
| Queue depth | Workload-specific | Trend/capacity based | Workload-specific |
| Consumer processing | Active / converging | Failures or no progress with backlog | Sustained inability to converge |

---

## 7.14 Messaging Health Guardrails

1. Queue health is measured by age/throughput as well as depth.
2. Normal integration queue lag must remain below 5 minutes.
3. Outbox records older than 2 minutes unpublished trigger an alert.
4. Outbox records older than 5 minutes unpublished are critical.
5. A RabbitMQ outage does not erase canonical business facts already committed with their Outbox intent.
6. RabbitMQ unavailability does not automatically make Tracking progress writes unavailable.
7. Eventual consistency must converge; indefinite projection staleness is a correctness failure.
8. DLQ steady state is zero.
9. Every newly dead-lettered message triggers operational attention.
10. Poison messages are isolated from healthy traffic.
11. DLQ messages are not replayed blindly or infinitely.
12. Queue depth has no universal global threshold; workload-specific capacity testing defines depth-based scaling thresholds later.
13. Messaging alerts do not automatically imply every Shiori API is unavailable; degraded-mode behavior remains capability-specific.

---

# 8. Observability — Structured Logs, Distributed Traces & Health Checks

## 8.1 Observability Principle

Observability exists so an engineer can answer questions such as:

```text
Which request failed?
Which service handled it?
Which distributed flow did it belong to?
Did the failure occur in HTTP, PostgreSQL, MongoDB, RabbitMQ, or a provider?
Was the work retried?
Did it reach a DLQ?
How long did each stage take?
Is the process alive?
Can it safely receive traffic?
```

without requiring:

- Production database guessing.
- Raw secret inspection.
- Logging private request bodies.
- Reproducing every failure manually.

Shiori requires observability behavior, not a specific observability vendor.

---

## 8.2 Structured Logging Is Mandatory

All executable Shiori components MUST emit structured logs rather than relying only on unstructured free-form console strings.

This includes, where applicable:

```text
YARP Gateway
Identity API
Catalog API
Tracking API
Profile BFF / Read Composer
approved Workers
Outbox publishers
RabbitMQ consumers
```

Structured logging means operational fields are emitted as named properties that can be filtered and correlated independently from the human-readable message.

---

## 8.3 Required Log Context

For an HTTP request or distributed operation, structured logs MUST carry the applicable tracing/correlation context.

At minimum:

```text
correlationId
traceparent / equivalent W3C trace context
traceId
spanId
service/component name
environment
log severity
timestamp in UTC
operation/event name
```

For HTTP request-completion logs, include where safe and applicable:

```text
HTTP method
route template
status code
server-side duration
```

Use the route template rather than blindly recording the complete raw URL/query string.

Example:

```text
PATCH /api/v1/tracking-items/{id}
```

is preferable as an operational route identifier to logging arbitrary user-controlled query data.

---

## 8.4 `correlationId` and W3C Trace Context

`traceparent` remains Shiori's primary distributed HTTP tracing contract.

`correlationId` remains the human-friendly operational/request correlation identifier.

They solve different problems:

```text
traceparent / traceId / spanId
        -> distributed trace structure

correlationId
        -> whole-flow/support correlation
```

Both MUST propagate through supported synchronous service boundaries.

When a flow crosses:

```text
HTTP
  -> Outbox
  -> RabbitMQ
  -> Consumer
```

correlation and causal/trace context must continue through the approved message metadata/contracts rather than being lost at the broker boundary.

A background operation that does not originate from an HTTP request must still establish its own trace/correlation context so its logs are not operationally anonymous.

---

## 8.5 Strict Privacy Rule — Secrets and Personal Data

Observability MUST NOT become a secondary database containing private user information.

The following MUST NEVER be written to normal application logs or traces:

```text
passwords
password hashes
access tokens
refresh tokens
Authorization headers
session/authentication cookies
password-reset or account-recovery secrets
client secrets
signing keys
API/provider credentials
raw email addresses
raw imported files
raw Import file contents
full request/response bodies containing private user data
biography text
private profile fields
private list contents
other sensitive profile/account data
```

This is a hard requirement, not a best-effort recommendation.

The fact that a field is useful for debugging does not override this rule.

---

## 8.6 Email Logging Rule

Email addresses are account identifiers and personal data.

Therefore:

```text
Email in logs = FORBIDDEN
```

Authentication/account troubleshooting must rely on safe operational identifiers such as:

- `correlationId`.
- `traceId`.
- An opaque Shiori `UserId` only where operationally necessary and authorized.
- Stable machine-readable error/event codes.

The login request body is never logged merely to make failed-login debugging easier.

---

## 8.7 Request and Response Body Logging

Full request/response body logging is disabled by default for public business endpoints.

An implementation may log approved non-sensitive bounded metadata such as:

```text
payload byte size
item count
media type
document/parser outcome
machine-readable error code
```

when that metadata is useful and does not expose user content.

Import files are never copied into logs or traces.

---

## 8.8 Exception Logging

Unexpected exceptions must be logged with enough technical context to diagnose the failure while still obeying the privacy rules above.

Logging an exception does not authorize Shiori to serialize the complete request object, authentication principal, token, imported file, or database entity into the log record.

Expected client/business outcomes such as documented `400`, `404`, `409`, or `412` responses should not automatically be logged as fatal server failures.

Unexpected `5xx`, dependency failures, repeated consumer failures, and data-integrity failures require appropriately elevated operational severity.

---

## 8.9 Distributed Tracing

Distributed tracing is required for supported cross-process flows.

Representative synchronous flow:

```text
Client
  -> YARP
  -> Profile BFF
  -> Identity
  -> Tracking
```

Representative asynchronous flow:

```text
HTTP request / scheduled operation
  -> local transaction + Outbox
  -> RabbitMQ
  -> Worker / consumer
  -> local database effect
```

Tracing must preserve the relationship between these stages without turning trace attributes into a dump of sensitive payload data.

Trace sampling/storage implementation is intentionally not fixed by this document.

Regardless of sampling strategy, correlation context must remain available for errors and critical distributed workflows.

---

## 8.10 Liveness Check

A **Liveness Check** answers:

> **Is this process alive and capable of continuing execution, rather than crashed, deadlocked, or irrecoverably unhealthy?**

Liveness must be intentionally shallow.

It MUST NOT fail merely because a remote dependency is temporarily unavailable.

For example, the following must not automatically fail Tracking API liveness:

```text
RabbitMQ unavailable
Catalog unavailable
AniList unavailable
MangaDex unavailable
```

Otherwise an infrastructure orchestrator could repeatedly restart a healthy Tracking process because an unrelated dependency is down.

The conceptual rule is:

```text
Process alive?
    YES -> Live
    NO  -> Not Live
```

---

## 8.11 Readiness Check

A **Readiness Check** answers:

> **Can this component currently accept traffic/work for the capability it owns without predictably failing its required synchronous dependencies?**

Readiness is therefore stricter than liveness.

For a database-owning API, readiness normally requires its own canonical datastore to respond sufficiently for the service to operate.

Examples:

```text
Identity API
  requires Identity PostgreSQL

Catalog API
  requires Catalog MongoDB

Tracking API
  requires Tracking PostgreSQL
```

If the owning canonical database is unavailable:

```text
process may remain LIVE
service becomes NOT READY
```

This allows the runtime to stop routing normal traffic to an instance that cannot perform its core local work without treating the process as crashed.

---

## 8.12 Readiness Must Respect Degraded Architecture

Readiness MUST NOT include every dependency indiscriminately.

A dependency belongs in readiness only when its absence means that the component cannot safely perform the capability for which it receives traffic.

Therefore:

### Tracking API

RabbitMQ failure alone does **not** make Tracking API unready because the accepted Outbox architecture allows local mutations to commit and publish later.

```text
Tracking PostgreSQL available
RabbitMQ unavailable
        |
        v
Tracking API may remain READY
Messaging subsystem = DEGRADED
```

### Catalog API

AniList or MangaDex failure alone does **not** make normal Catalog API reads unready because those reads use Shiori-owned local MongoDB state.

```text
Catalog MongoDB available
AniList unavailable
        |
        v
Catalog API may remain READY
Provider synchronization = DEGRADED
```

### Profile BFF

The Profile BFF has no canonical database.

Its health must reflect the fact that Identity is the mandatory profile-level privacy gate.

If the BFF process is alive but Identity cannot establish profile eligibility, public-profile composition fails closed according to ADR-013.

The exact deployment decision about whether that condition marks the entire BFF instance `Not Ready` or leaves it ready to return controlled fail-closed responses may depend on the runtime/orchestrator behavior.

The privacy invariant does not change:

```text
Identity policy unknown
        ->
NO Tracking exposure
```

This NFR does not weaken that rule for health-check convenience.

---

## 8.13 Gateway Health

YARP is infrastructure-only.

Gateway liveness checks the Gateway process.

Gateway readiness must not simply aggregate every downstream business service into one global dependency gate.

Otherwise:

```text
Catalog outage
        ->
Gateway Not Ready
        ->
Identity + Tracking also become unreachable
```

which would destroy the fault-isolation purpose of the architecture.

The Gateway may route to a healthy capability while another route is degraded/unavailable.

Downstream service health is observed independently.

---

## 8.14 Worker Health

Workers distinguish the same two concepts:

```text
Liveness
  = Worker process is alive and can continue.

Readiness
  = Worker can currently perform the workload it owns.
```

A RabbitMQ consumer Worker whose workload requires RabbitMQ connectivity may be `Live` but `Not Ready` while broker connectivity is unavailable.

A Worker must not acknowledge work merely to remain healthy.

Incomplete work remains durable/retryable according to its queue/job semantics.

---

## 8.15 Health Checks Are Operational Contracts

Health checks must be:

- Fast.
- Bounded by short internal timeouts.
- Side-effect free.
- Safe to call repeatedly.
- Free of sensitive data.
- Separate from business APIs.

A health response must not expose:

```text
connection strings
credentials
internal tokens
private database contents
stack traces
sensitive infrastructure details
```

---

## 8.16 Observability Guardrails

1. All executable Shiori components emit structured logs.
2. Distributed operations carry `correlationId` and W3C trace context.
3. HTTP logs include `traceId`, `spanId`, and `correlationId` where applicable.
4. Background/message work establishes or continues trace/correlation context.
5. Passwords, tokens, emails, Authorization headers, auth cookies, secrets, private profile data, and import-file contents are never logged.
6. Full public request/response bodies are not logged by default.
7. Route templates are preferred over raw user-controlled URLs/query strings in request logs.
8. Liveness checks process viability, not remote dependency availability.
9. Readiness checks whether the component can safely perform its owned workload.
10. A service's own canonical database is normally a readiness dependency.
11. RabbitMQ is not a Tracking API readiness dependency when the Outbox path can safely preserve publication intent.
12. AniList/MangaDex are not normal Catalog API readiness dependencies.
13. Gateway readiness must not collapse all downstream service availability into one global gate.
14. Health endpoints are side-effect free and never disclose secrets or private data.
15. No monitoring/logging/tracing vendor is mandated by this architecture.

---

# 9. Data Retention Policy

## 9.1 Purpose

Shiori must retain data long enough to preserve correctness, recoverability, and operational diagnosability without allowing infrastructure/temporary records to grow without bound.

Retention policy follows three principles:

1. **Canonical user/product data is not deleted merely to control infrastructure growth.**
2. **Temporary and idempotency/transport records have explicit finite retention.**
3. **Records required to repair an unresolved failure are not silently deleted just because their normal cleanup age was reached.**

This section does not define user-requested account deletion or product-level history deletion semantics.

Those are separate product/privacy lifecycle concerns.

---

## 9.2 Retention Classes

Shiori distinguishes:

```text
CANONICAL PRODUCT DATA
    Identity / Catalog / Tracking authoritative state

DERIVED DATA
    rebuildable projections / summaries

INFRASTRUCTURE CORRECTNESS DATA
    Inbox / Outbox / HTTP idempotency records

TEMPORARY WORKFLOW DATA
    import files / staging

OPERATIONAL DIAGNOSTIC DATA
    logs / traces / audit events

RECOVERY DATA
    backups / point-in-time recovery chain
```

A finite retention period applied to infrastructure records must never be interpreted as permission to purge the underlying canonical business fact.

---

## 9.3 RabbitMQ Inbox Retention

Successfully processed Inbox/idempotency records for RabbitMQ messages are retained for:

```text
7 days after successful processing
```

Purpose:

- Protect against duplicate/redelivered integration messages during the expected operational replay window.
- Support short-term incident investigation.
- Prevent the Inbox table from growing indefinitely.

After seven days, successfully completed Inbox records may be deleted in bounded cleanup batches.

Inbox records associated with unresolved/failed processing are not treated as successfully completed records and MUST NOT be purged by the normal success-retention job while they remain required for correctness/investigation.

---

## 9.4 Published Outbox Retention

Successfully published Outbox records are retained for:

```text
7 days after confirmed publication
```

This provides a short operational audit/debugging window while preventing permanent Outbox-table growth.

After the retention window, published rows may be deleted in bounded cleanup batches.

### Critical rule

```text
Unpublished Outbox record
        !=
expired infrastructure garbage
```

An unpublished Outbox record MUST NOT be deleted solely because it is old.

Old unpublished records trigger the Messaging Health alerts defined in Section 7 and remain until publication or explicit operational resolution.

---

## 9.5 HTTP Idempotency-Key Retention

Durable HTTP idempotency records for completed retry-safe mutations are retained for:

```text
24 hours after the original operation completes
```

During this window, reuse of the same Idempotency Key for the same logical request must reproduce or reference the previously committed outcome according to the endpoint contract.

Reuse with materially different input remains a conflict according to `API_CONVENTIONS.md`.

After 24 hours, the server is no longer required to preserve the historical idempotency result for that key.

Clients must not rely on an Idempotency Key as a permanent business identifier.

---

## 9.6 Temporary Import File Retention

Uploaded import files contain private library/history information and therefore receive aggressive cleanup.

Once the original file has been successfully parsed into the durable Tracking staging representation and is no longer required for safe resumption:

```text
Delete as soon as practical
and no later than 24 hours afterward.
```

For an import that terminates before successful parsing because it is:

```text
Failed
Cancelled
Rejected as invalid
```

the temporary uploaded file must also be deleted no later than:

```text
24 hours after the terminal outcome
```

The file is not kept merely for debugging.

Logs may record safe metadata such as file size, source type, job ID, or parser error code, but never the file contents.

---

## 9.7 Import Staging Retention

Tracking staging rows must remain available while they are required by an active import workflow, including Preview / `AwaitingConfirmation`.

This NFR does **not** invent a user-facing expiration period for an `AwaitingConfirmation` job because that would change product behavior.

After an Import job reaches a terminal state such as:

```text
Completed
PartiallyCompleted
Failed
Cancelled
```

staging rows that are no longer required for correctness are retained for at most:

```text
24 hours
```

and then removed in bounded cleanup batches.

The durable final Tracking state and required progress history are not deleted with staging.

---

## 9.8 Import Job Metadata Retention

Terminal Import job metadata may be retained for:

```text
30 days after terminal completion
```

The retained metadata should be limited to information needed for:

- User-visible job status where applicable.
- Support/incident diagnosis.
- Processing counts.
- Safe failure codes.

It must not preserve the original uploaded file contents.

The exact product decision about exposing historical Import jobs to the user may evolve independently; this retention period does not create a new product feature.

---

## 9.9 DLQ Retention

Dead-lettered messages are retained for a maximum normal operational window of:

```text
14 days
```

A DLQ item must receive attention immediately when created; the 14-day window is not permission to ignore it for 14 days.

Before expiration, each unresolved DLQ item must be:

```text
safely replayed
or
explicitly resolved/discarded with documented reason
or
preserved through an approved incident/diagnostic procedure if longer evidence retention is required
```

No automatic silent DLQ deletion is considered a valid resolution for a correctness-affecting poison message.

The architecture does not mandate a separate archival service.

---

## 9.10 Application Log Retention

Normal structured application/operational logs are retained for:

```text
30 days
```

This window supports recent incident diagnosis while bounding long-term accumulation of detailed operational data.

The privacy rules in Section 8 apply regardless of retention duration.

Short retention is not a substitute for safe logging.

---

## 9.11 Distributed Trace Retention

Detailed distributed trace data is retained for:

```text
7 days
```

Traces are comparatively high-volume diagnostic data and should not become permanent user-activity history.

Aggregated metrics may use a different retention strategy because they contain different information and volume characteristics; their final retention duration is intentionally not fixed by this document.

---

## 9.12 Security / Identity Audit Event Retention

Security-relevant audit events required by Identity are retained for:

```text
90 days
```

Examples may include safe, structured events representing:

- Authentication success/failure category.
- Token revocation action.
- Credential/security configuration change.
- Recovery workflow outcome.

Audit events MUST still obey the no-secret/no-email logging policy.

They should use opaque Shiori identifiers and event categories rather than copying credentials or private request payloads.

This retention requirement applies to security audit events, not to raw access/refresh tokens themselves.

---

## 9.13 Recovery / Backup Retention

The canonical datastore recovery chain must preserve usable recovery points for at least:

```text
35 days
```

for:

```text
Identity PostgreSQL
Tracking PostgreSQL
Catalog MongoDB
```

This retention window supports the monthly Restore Test requirement from Section 5 while leaving margin to test a recovery point from the preceding month.

The backup mechanism may use snapshots, incremental backup, point-in-time recovery logs, or another production-appropriate method.

The implementation must still meet the RPO/RTO objectives from Section 5.

A 35-day retention window by itself does not satisfy a 5-minute RPO; the recovery mechanism must provide sufficiently granular recovery points inside that retained chain.

---

## 9.14 Cleanup Execution Rules

Retention cleanup is operational maintenance owned by the service that owns the records.

Cleanup MUST be:

- Bounded.
- Idempotent.
- Safe to retry.
- Performed in batches rather than giant unbounded deletes.
- Observable through success/failure metrics/logs.
- Designed so one failed cleanup execution does not block normal business traffic.

Shiori does not introduce a global cross-domain cleanup service.

Examples:

```text
Tracking owns cleanup of:
    Tracking Inbox
    Tracking Outbox
    Tracking idempotency records
    Import staging
    Import temporary-file lifecycle

Catalog owns cleanup of:
    Catalog Inbox/Outbox records where applicable

Identity owns cleanup of:
    Identity-owned operational/idempotency/audit records
```

---

## 9.15 Retention Summary

| Data class | Retention |
|---|---:|
| Successfully processed RabbitMQ Inbox record | `7 days` |
| Successfully published Outbox record | `7 days` |
| Unpublished Outbox record | **No age-based deletion; alert + resolve** |
| Completed HTTP Idempotency-Key result | `24 hours` |
| Temporary Import file after safe parse/terminal failure | `<= 24 hours` |
| Import staging after terminal job state | `<= 24 hours` |
| Terminal Import job metadata | `30 days` |
| DLQ message | `14 days maximum normal operational window` |
| Normal structured application logs | `30 days` |
| Detailed distributed traces | `7 days` |
| Security/Identity audit events | `90 days` |
| Canonical datastore recovery chain | `>= 35 days` |
| Canonical product data | Governed by product/account/data-lifecycle rules, not infrastructure TTL |

---

## 9.16 Retention Guardrails

1. Canonical user/product data is not purged by infrastructure cleanup jobs merely to reduce table size.
2. Successfully processed RabbitMQ Inbox records are retained for 7 days.
3. Successfully published Outbox records are retained for 7 days.
4. Unpublished Outbox records are never silently age-purged.
5. Completed HTTP idempotency results are retained for 24 hours.
6. Import files are removed no later than 24 hours after they are no longer required for safe processing/recovery.
7. Terminal Import staging is removed within 24 hours when no longer required for correctness.
8. Import job metadata may remain for 30 days without retaining the raw file.
9. DLQ messages are operational incidents, not long-term storage; the normal maximum window is 14 days.
10. Normal application logs are retained for 30 days and detailed traces for 7 days.
11. Security audit events are retained for 90 days without recording emails, tokens, passwords, or secrets.
12. Canonical datastore recovery data remains available for at least 35 days while still satisfying the much smaller RPO windows from Section 5.
13. Cleanup runs in bounded, idempotent batches.
14. Each bounded context cleans up only the records it owns.
15. No generic global cleanup microservice is introduced.

---

---

# 10. Scalability & Capacity Expectations

## 10.1 Purpose

Shiori's architecture allows business capabilities to scale independently because Identity, Catalog, Tracking, the YARP Gateway, the approved Profile BFF / Read Composer, and approved Workers have separate runtime responsibilities.

This does not mean every component must be multiplied preemptively.

The NFR rule is:

```text
Measure workload
      |
      v
Identify saturation / SLO pressure
      |
      v
Find the real bottleneck
      |
      v
Scale or optimize the owning component
```

not:

```text
Traffic exists
      |
      v
Add infrastructure everywhere
```

Capacity decisions must be evidence-based.

Shiori does not define a fixed replica count in this document because the final production topology and host sizes are intentionally separate deployment decisions.

---

## 10.2 Workload Profiles

Different Shiori capabilities have materially different resource profiles.

They must not be treated as one generic workload.

### 10.2.1 Catalog Search & Catalog Reads — Read-Heavy

Normal Catalog browsing is served from Shiori-owned Catalog state rather than synchronously calling AniList or MangaDex.

Representative operations include:

```text
Catalog Search
Catalog Item read
Franchise read
Trending / Seasonal read surfaces
```

Primary characteristics:

```text
read-heavy
latency-sensitive
index-sensitive
cache/read-model friendly where already approved
MongoDB query sensitive
high fan-in from public clients
```

Primary capacity signals:

- API request concurrency.
- `p50`, `p95`, and `p99` latency against the Section 2 Fast Local Read budget.
- API CPU utilization.
- API memory / runtime allocation pressure.
- MongoDB query latency.
- MongoDB connection-pool pressure.
- Slow-query frequency.
- Cache hit/miss behavior where an approved cache path exists.
- `5xx` rate.

A latency regression caused by an unindexed or expensive query must not be "fixed" only by adding more API instances.

If the database/query path is the bottleneck, the owning query/index design must be corrected first.

---

## 10.2.2 Tracking Progress Writes — Transactional Write-Heavy

Representative operations include:

```text
Progress Update
Quick +1
Undo
Library Status change
Tracking entry mutation
```

Primary characteristics:

```text
small request payloads
transactional writes
latency-sensitive
PostgreSQL durability-sensitive
history/idempotency/outbox-sensitive
concurrency-sensitive
```

Primary capacity signals:

- Section 2 Transactional Write `p50` / `p95` / `p99` latency.
- Tracking API CPU and memory pressure.
- PostgreSQL transaction latency.
- Database CPU / I/O pressure.
- Connection-pool saturation or wait time.
- Lock contention / blocked transactions.
- Deadlock or transaction-failure rate.
- `5xx` rate.
- Outbox creation/publication health after successful local commits.

Scaling the Tracking API is not a valid substitute for an unhealthy PostgreSQL write path.

The critical progress-write path must continue to avoid synchronous Catalog/provider dependencies.

---

## 10.2.3 Smart Staging Import — Bursty Background Work

Import is intentionally separated from the synchronous request path.

The HTTP request performs bounded acceptance work and returns the durable job response. Parsing, matching, hydration coordination, staging, and confirmed commit continue asynchronously.

Primary characteristics:

```text
bursty
CPU-consuming during parse/validation
PostgreSQL-consuming during staging/commit
RabbitMQ-consuming during asynchronous coordination
batch-oriented
potentially long-running
bounded by the 5 MiB / 10,000-item limits from Section 6
```

Primary capacity signals:

- Number of queued/running Import jobs.
- Import queue depth.
- Oldest Import-related message age.
- Job processing duration.
- Worker CPU utilization.
- Worker memory / allocation pressure.
- Tracking PostgreSQL write latency.
- Tracking PostgreSQL connection-pool pressure.
- Catalog-hydration backlog.
- Import failure/retry rate.
- Queue Lag and Outbox Age from Section 7.

Worker concurrency must remain bounded.

The system must not increase Import parallelism until it overwhelms Tracking PostgreSQL, RabbitMQ, Catalog, or an external provider.

The purpose of asynchronous processing is load isolation, not unlimited concurrency.

---

## 10.2.4 Catalog Provider Synchronization — Provider-Bound Background Work

AniList and MangaDex synchronization is background/provider-backed work.

Primary characteristics:

```text
network-bound
provider-rate-limit constrained
retry/circuit-breaker controlled
not part of normal Catalog read latency
```

Primary capacity signals:

- Provider request latency.
- Provider timeout rate.
- Provider retry rate.
- HTTP `429` / rate-limit responses.
- Circuit Breaker state.
- Last successful provider synchronization.
- Synchronization backlog/freshness.
- Worker CPU/memory only after provider health has been ruled out as the bottleneck.

Important rule:

> **Provider throttling is not a signal to create more outbound pressure.**

If the provider is rate-limiting Shiori or the Circuit Breaker is open, increasing worker concurrency is normally the wrong response.

---

## 10.2.5 Profile BFF / Read Composer — Stateless Fan-Out Read

The Profile BFF is a stateless read composer.

Its synchronous path is:

```text
Client
  |
  v
YARP
  |
  v
Profile BFF
  |
  +--> Identity first
  |
  +--> Tracking only after safe Identity result
```

Primary characteristics:

```text
read-only
stateless
fan-out / dependency-latency sensitive
privacy-sensitive
```

Primary capacity signals:

- Request concurrency.
- BFF CPU and memory pressure.
- End-to-end profile `p95` / `p99` latency.
- Identity dependency latency/failure rate.
- Tracking dependency latency/failure rate.
- Full-response vs approved degraded-response ratio.
- Fail-Closed response rate caused by inability to evaluate Identity safely.

Scaling the BFF cannot repair an unavailable Identity or Tracking dependency.

Dependency failure must continue to follow ADR-013 degradation/privacy semantics rather than being hidden through resource scaling.

---

## 10.2.6 Identity — Security-Critical Transactional Work

Identity handles account access and OAuth2/OIDC behavior.

Representative operations include:

```text
Registration
Login
Refresh
Revocation
Recovery
Profile metadata changes
```

Primary characteristics:

```text
security-critical
PostgreSQL-backed
latency-sensitive
bursty around login/session activity
cryptographic/token-processing cost where applicable
```

Primary capacity signals:

- Request concurrency.
- API CPU and memory pressure.
- Authentication endpoint latency.
- Identity PostgreSQL latency and connection-pool pressure.
- `5xx` rate.
- Rate-limit rejection trends.
- Token/signing/discovery operational errors.

Security controls must never be disabled to gain throughput.

---

## 10.2.7 YARP Gateway — Edge / Routing Workload

YARP is the public entry point and remains infrastructure-focused.

Primary characteristics:

```text
all-public-request fan-in
network / I/O heavy
routing and edge-policy work
stateless
no business database
```

Primary capacity signals:

- Incoming request rate and concurrency.
- Gateway CPU and memory pressure.
- Connection/socket pressure.
- Gateway-added latency.
- Request rejection due to legitimate rate/request-size policy.
- Downstream timeout/error distribution by route.

The Gateway must not become a hidden business-workflow bottleneck.

---

## 10.3 Scaling Signals — No Single Metric Is Sufficient

Shiori must not scale a component from one isolated measurement such as CPU alone.

Scaling decisions should correlate at least two categories of evidence where applicable:

```text
USER / SLO PRESSURE
- p95 / p99 latency approaching or exceeding budget
- 5xx error-rate increase
- availability degradation

RESOURCE SATURATION
- sustained CPU pressure
- sustained memory / allocation / GC pressure
- connection-pool pressure
- database CPU / I/O / lock pressure

BACKLOG PRESSURE
- queue depth increasing
- Queue Lag increasing
- Outbox Age increasing
- Import backlog increasing

DEPENDENCY PRESSURE
- provider 429 / timeout / Circuit Open
- database latency
- downstream service latency/failure
```

A temporary CPU spike with healthy latency and no backlog does not by itself prove that scaling is required.

Likewise, an elevated `p95` caused by an unhealthy database or provider must not automatically trigger more application instances that increase pressure on the failing dependency.

---

## 10.4 Horizontal Scaling Eligibility

Stateless or independently runnable compute components must remain capable of horizontal scaling when real load demonstrates the need.

Examples include, subject to the final deployment topology:

```text
YARP Gateway
Identity API
Catalog API
Tracking API
Profile BFF
approved background consumers / Workers
```

This statement does not pre-approve a replica count.

Stateful infrastructure such as PostgreSQL, MongoDB, and RabbitMQ requires its own capacity/HA design and cannot be treated as interchangeable stateless replicas.

Database scaling decisions must preserve service ownership, transaction semantics, durability, and the RPO/RTO requirements from Section 5.

---

## 10.5 Queue-Based Worker Scaling

For asynchronous consumers, Queue Depth is useful but insufficient by itself.

The primary backlog decision must consider:

```text
Queue Depth
+
Oldest Message Age / Queue Lag
+
Consumer Throughput
+
Downstream Capacity
```

Examples:

```text
Depth rises
but oldest-message age remains low
and consumers are converging
    -> may still be healthy burst absorption

Depth rises
and oldest-message age crosses 5 minutes
    -> unhealthy backlog; investigate / add safe capacity if downstream permits
```

Any worker-scaling action must remain compatible with:

- Inbox idempotency.
- At-least-once delivery.
- Bounded database concurrency.
- Provider rate limits.
- Circuit Breakers.
- Graceful shutdown.

---

## 10.6 Capacity Baseline Must Be Measured

Before MVP launch, Shiori must establish a repeatable capacity baseline in a production-like staging environment.

For each critical workload, the test record must capture at minimum:

```text
workload definition
request/job concurrency
achieved throughput
p50 / p95 / p99 latency where HTTP applies
5xx/error rate
CPU
memory
relevant database latency
connection-pool pressure
queue depth / lag where messaging applies
first observed bottleneck
```

The capacity baseline is evidence, not a promise of infinite growth.

The purpose is to answer:

> **At the tested resource profile, what load can Shiori sustain while remaining inside its NFRs, and what component becomes the first bottleneck as load grows?**

The exact launch throughput target is not invented in this document because expected production traffic and final deployment resources are not yet fixed.

Once those inputs are known, launch capacity must be approved against measured staging evidence rather than intuition.

---

## 10.7 Capacity Guardrails

1. Workload profiles are measured separately; Shiori does not use one generic capacity number.
2. Catalog Search / reads are primarily read-heavy and latency/index sensitive.
3. Tracking mutations are transactional-write and PostgreSQL sensitive.
4. Import is bursty background work and must remain isolated from public request capacity.
5. Provider synchronization is rate-limit/provider constrained and must not scale outbound pressure blindly.
6. Profile BFF scaling does not weaken Identity-first Fail-Closed privacy behavior.
7. Queue Depth is evaluated together with Queue Lag and consumer throughput.
8. Scaling is based on sustained evidence, not isolated spikes.
9. Application scaling must not hide a database/query/design bottleneck.
10. No fixed server/pod/replica count is defined by this NFR.
11. Production-like capacity testing must identify the first real bottleneck before MVP launch.
12. Capacity changes must preserve the latency, availability, durability, privacy, and messaging guarantees already defined in Sections 1–9.

---

# 11. Dashboards & Alerting

## 11.1 Purpose

An SLI that exists only in documentation is not operationally useful.

Every launch-critical SLI defined by this NFR must be visible through operational dashboards and connected to an alerting rule when failure requires human or automated operational action.

The dashboard/alerting layer must remain tool-neutral.

The requirement is about observable behavior, not a vendor.

---

## 11.2 Minimum Launch Dashboard Coverage

At minimum, the production-like staging and production operational view must expose the following categories.

### HTTP / Capability Health

```text
request volume
successful-request ratio
5xx server-error rate
capability availability
p50 latency
p95 latency
p99 latency
full vs degraded response ratio where degradation is supported
```

The latency views must be separable by at least:

- Service/component.
- Public route family or capability.
- HTTP method where useful.
- Response-status class.

A global average that mixes Catalog Search, Progress Update, and Import acceptance is insufficient because those operations use different latency classes.

---

### Messaging Health

The dashboard must expose:

```text
Queue Depth
Queue Lag / oldest queued message age
Outbox Age / oldest unpublished record
Outbox publication failures
consumer processing/failure/retry rate
Inbox failures
DLQ count / newly dead-lettered messages
projection freshness where applicable
```

Existing Section 7 thresholds remain authoritative:

```text
Queue Lag >= 5 min       -> Alert
Queue Lag >= 15 min      -> Critical

Outbox Age >= 2 min      -> Alert
Outbox Age >= 5 min      -> Critical

New DLQ message >= 1     -> Alert
DLQ steady state         -> 0
```

---

### Datastore Health

For Identity PostgreSQL, Tracking PostgreSQL, and Catalog MongoDB, operational views must expose enough information to detect inability to serve the owning workload.

At minimum this includes, where the technology exposes it:

```text
availability/connectivity
operation/query latency
error rate
connection-pool pressure
capacity/resource pressure
backup success/failure
last successful backup/recovery evidence status
```

Database metrics must be interpreted in the context of the owning service rather than as isolated infrastructure charts.

---

### External Provider Health

Catalog provider health must expose separately for AniList and MangaDex:

```text
request latency
success/failure rate
timeout rate
retry rate
rate-limit / 429 observations
Circuit Breaker state
last successful synchronization / freshness
```

A provider incident must remain distinguishable from a Catalog API incident.

---

### Import Health

The operational view must expose:

```text
jobs by state
oldest pending/processing job
processing duration
failure rate
retry rate
stuck-job indicators
Catalog hydration backlog where applicable
```

A high volume of successful public `202 Accepted` responses must not hide a background Import subsystem that is no longer making progress.

---

## 11.3 Latency Alerting

Latency alerts must evaluate the percentile SLOs from Section 2.

At minimum:

```text
Fast Local Read p95 budget      = <= 250 ms
Transactional Write p95 budget  = <= 400 ms
Async Acceptance p95 budget     = <= 500 ms
```

`p99` must remain visible because tail-latency regressions can exist even while `p95` still passes.

An isolated slow request must not automatically page an operator.

Latency alerting must use a sustained evaluation window or equivalent SLO-breach rule that distinguishes persistent degradation from isolated outliers.

The exact monitoring-query syntax and evaluation window are implementation details, but the resulting alert must trigger early enough that persistent SLO degradation is not discovered only after a monthly report.

---

## 11.4 `5xx` Error-Rate Alerting

Unexpected `5xx` responses count against the Section 3 availability/reliability objective.

The dashboard must show the `5xx` rate by capability and service.

Alerting must detect sustained server-error behavior that places the `99.9%` monthly core-capability SLO at risk.

This document does not invent a universal short-window percentage because low-traffic and high-traffic endpoint families behave differently statistically.

Instead, the implementation must configure an SLO-aware rule that:

1. Ignores expected client-caused `4xx` outcomes as service failures.
2. Detects sustained unexpected `5xx` behavior.
3. Evaluates the affected capability rather than only a global aggregate.
4. Produces an actionable alert before the monthly availability objective is irrecoverably consumed.

The short-window threshold/window selected during implementation must be validated against staging traffic and documented with the dashboard rule.

---

## 11.5 Alert Severity

Alerts must distinguish urgency.

Conceptually:

```text
DEGRADED / WARNING
    -> service still operating but an NFR is moving outside normal bounds

ALERT / UNHEALTHY
    -> accepted threshold violated; investigation required

CRITICAL
    -> severe sustained failure, data-integrity risk, or major inability to converge/serve safely
```

Examples already defined:

```text
Outbox Age >= 2 min
    -> Alert

Outbox Age >= 5 min
    -> Critical

Queue Lag >= 5 min
    -> Alert

Queue Lag >= 15 min
    -> Critical

Any new DLQ message
    -> Alert
```

Severity must reflect required response, not merely metric magnitude.

---

## 11.6 Alerts Must Be Actionable

Every production alert must answer, either directly or through its associated operational documentation:

```text
What failed?
Which capability is affected?
Which threshold was crossed?
What is the current observed value?
Is user-facing availability affected or only degraded background work?
What dependency/component is most likely involved?
What is the first safe diagnostic action?
What runbook or recovery procedure applies?
```

An alert that only says:

```text
"Something is wrong"
```

is not sufficient.

---

## 11.7 No Alert Fatigue

The normative rule is:

> **No alert may be allowed to become routine noise that operators learn to ignore.**

Therefore:

1. A condition that requires no action should normally be a metric/log/dashboard signal, not a paging alert.
2. Alerts must represent a threshold or state that has an expected response.
3. Repeated duplicate notifications for one continuing incident should be minimized by the selected tooling/configuration.
4. Flapping conditions must be tuned or corrected rather than accepted indefinitely.
5. A permanently ignored alert is considered a broken alert and must be fixed, removed, or reclassified.
6. Warning and Critical severity must not be used interchangeably.
7. The alert rule must link to or identify an applicable runbook once the runbook exists.
8. A dashboard may contain more signals than the alerting system; visibility does not require paging for every metric.

This rule is part of launch readiness.

A monitoring configuration that technically emits notifications but is routinely ignored does not satisfy this NFR.

---

## 11.8 Dashboard & Alerting Guardrails

1. Every launch-critical SLI must be visible operationally.
2. HTTP latency must show `p50`, `p95`, and `p99`, not average latency alone.
3. The three API latency classes must remain distinguishable.
4. `5xx` server-error rate must be visible by affected capability/service.
5. Core capability availability must be measurable against the `99.9%` monthly SLO.
6. Queue Lag, Queue Depth, Outbox Age, consumer failures, Inbox failures, and DLQ state must be visible.
7. Section 7 Queue Lag / Outbox Age / DLQ alert thresholds remain authoritative.
8. Provider health is monitored independently for AniList and MangaDex.
9. Import background progress/failure must remain visible independently from successful `202 Accepted` responses.
10. Alerts must be actionable and severity-based.
11. No alert may be accepted as permanent routine noise.
12. Tool/vendor selection remains outside this NFR.

---

# 12. Verification — Milestone 5B Launch Gate

## 12.1 Purpose

`NON_FUNCTIONAL_REQUIREMENTS.md` is not a list of aspirations.

Every requirement that affects MVP launch readiness must have objective verification evidence.

The governing rule is:

> **An NFR is not considered satisfied because the code appears correct or because a configuration file contains the desired value. The relevant behavior must be exercised and measured.**

Milestone 5B is the final MVP gate that verifies and operationalizes the quality work introduced throughout the roadmap.

It does not introduce NFR testing for the first time; it proves that the completed system satisfies the agreed requirements in a production-like environment.

---

## 12.2 Verification Environment

Launch-gate verification must run against a production-like staging environment containing the actual architectural paths used by the MVP, including as applicable:

```text
YARP Gateway
Identity
Catalog
Tracking
Profile BFF / Read Composer
PostgreSQL
MongoDB
RabbitMQ
approved Workers/consumers
```

The environment must use production-equivalent behavior for:

- Routing.
- Authentication/authorization.
- Database migrations/schema/indexes.
- RabbitMQ durability and consumer behavior.
- Request-size/rate policies.
- Observability instrumentation.
- Backup/restore procedure.

The exact infrastructure vendor/topology may differ from final production where necessary, but the test environment must be representative enough that the verified behavior is meaningful.

---

## 12.3 Load Testing — API Latency & Capacity

Milestone 5B must include load tests for at least the critical workloads already required by the roadmap:

```text
Catalog reads / search
Tracking progress writes
Concurrent Imports
```

The tests must exercise the public path through YARP where the objective being verified is a public API NFR.

### Catalog Read Test

Pass evidence must include:

- Workload definition and request mix.
- Concurrency/load level.
- `p50`, `p95`, `p99` latency.
- `5xx` rate.
- Catalog API CPU/memory.
- MongoDB query latency/resource pressure.
- Confirmation that normal reads do not silently depend on live AniList/MangaDex calls.

The Fast Local Read latency class must pass its defined budget under the accepted launch-load profile.

### Tracking Write Test

Pass evidence must include:

- Progress mutation request mix.
- Concurrent write level.
- `p50`, `p95`, `p99` latency.
- `5xx` rate.
- PostgreSQL transaction/query latency.
- Connection-pool / lock pressure.
- Verification that required current state + history + Outbox/idempotency semantics remain correct.

The Transactional Write latency class must pass its defined budget under the accepted launch-load profile.

### Concurrent Import Test

Pass evidence must include:

- Concurrent Import-job count used by the test.
- Representative file sizes/item counts including boundary/large cases.
- API acceptance latency.
- Background processing throughput/duration.
- CPU/memory pressure.
- Tracking PostgreSQL pressure.
- Queue Depth and Queue Lag behavior.
- Failure/retry behavior.
- Proof that normal API traffic does not collapse because Imports are active.

The test must include the Section 6 defensive boundaries for supported Import input.

---

## 12.4 Boundary / Resource-Protection Tests

The operational limits from Section 6 must be verified rather than trusted from configuration review alone.

At minimum, automated/integration testing must prove:

```text
normal JSON body <= approved limit
oversized normal JSON -> rejected safely
Import content <= 5 MiB accepted when otherwise valid
Import content > 5 MiB -> 413 Content Too Large
Import <= 10,000 parsed entries accepted when otherwise valid
Import > 10,000 parsed entries -> rejected without partial live-library mutation
```

Tests must also verify that rejected oversized input does not require Shiori to fully materialize or process the entire unsafe payload in memory before applying the defensive boundary where the platform permits earlier rejection.

---

## 12.5 Controlled Fault-Injection / Chaos Testing

Before MVP launch, Shiori must deliberately test the degraded-mode behavior defined in Sections 3–9.

For STEP 8, **Chaos Testing** means controlled fault injection in an isolated production-like environment.

It does not mean intentionally causing uncontrolled production outages.

The purpose is to prove that known dependency failures produce the designed degraded state rather than an unexpected cascading failure.

---

## 12.6 Provider-Outage Verification

Tests must simulate or safely reproduce provider conditions such as:

```text
AniList unavailable
MangaDex unavailable
provider timeout
provider rate limit / 429
repeated transient provider failures
```

Pass criteria include:

1. Provider calls obey the Section 4 `3 second` per-attempt timeout.
2. Immediate retries remain bounded and use the approved exponential-backoff + jitter policy.
3. Circuit Breakers are isolated per provider.
4. The breaker opens after the defined exhausted-failure threshold.
5. Catalog's existing valid local state remains available according to the approved degraded behavior.
6. Normal Tracking progress writes remain independent from provider availability.
7. Provider failure is visible in metrics/dashboard/alerts without being misclassified as universal Shiori downtime.
8. Recovery permits provider work to resume without a retry storm.

---

## 12.7 RabbitMQ-Outage Verification

A controlled RabbitMQ outage must verify the core Outbox guarantee.

Representative flow:

```text
RabbitMQ unavailable
        |
        v
Tracking progress mutation
        |
        v
Tracking PostgreSQL commits:
current state + history + required Outbox
        |
        v
HTTP mutation succeeds according to its normal contract
        |
        v
messaging health becomes degraded
```

Pass criteria include:

1. Eligible local Tracking/Catalog transactions do not require RabbitMQ to commit.
2. Required Outbox records remain durable and unpublished while the broker is unavailable.
3. Outbox Age crosses the documented degraded/alert thresholds visibly.
4. Tracking readiness does not become false solely because RabbitMQ is unavailable when the local Outbox path remains safe.
5. On broker recovery, pending Outbox work is published.
6. Consumers process redeliveries idempotently.
7. Backlog converges without duplicate business effects.
8. Queue Lag returns to the healthy range.
9. No unpublished Outbox record is silently discarded.

---

## 12.8 Consumer Redelivery / Backlog Recovery Verification

Milestone 5B must test RabbitMQ behaviors already assumed by the architecture:

```text
at-least-once redelivery
consumer restart
consumer backlog
out-of-order/stale event arrival where applicable
poison-message isolation
```

Pass criteria include:

- Duplicate Integration Events do not duplicate business effects.
- Inbox/idempotency state prevents repeated effects.
- Older aggregate versions do not regress newer local projection state.
- Consumer restart does not require manual reconstruction of normal in-flight work.
- A poison message can reach the DLQ without permanently blocking healthy traffic.
- DLQ creation triggers the required operational alert.
- Backlog recovery eventually returns Queue Lag below the accepted threshold.

---

## 12.9 Profile BFF Degradation / Privacy Verification

The approved shareable-profile degradation model must be tested explicitly.

Required scenarios include:

```text
Identity says Private
    -> no Tracking exposure

Identity unknown/failure
    -> Fail Closed
    -> no Tracking exposure

Identity says Public + Tracking healthy
    -> full authorized profile

Identity says Public + Tracking unavailable
    -> approved degraded Identity-only profile
```

No load, timeout, retry, cache, or degraded-mode optimization may cause private Tracking data to be exposed when Identity cannot authorize it.

Privacy correctness outranks partial availability.

---

## 12.10 Liveness / Readiness Verification

Health checks from Section 8 must be exercised under real dependency failures.

Examples:

```text
Tracking process alive + Tracking PostgreSQL unavailable
    -> Liveness remains appropriate for process viability
    -> Tracking Readiness = false

Tracking PostgreSQL healthy + RabbitMQ unavailable
    -> Tracking process live
    -> Tracking API may remain ready
    -> Messaging subsystem degraded

Catalog MongoDB healthy + AniList unavailable
    -> Catalog API may remain ready for local reads
    -> Provider synchronization degraded
```

The test must prove that readiness reflects the component's ability to perform its owned synchronous responsibility rather than blindly requiring every dependency to be healthy.

---

## 12.11 Restore Tests — Real Recovery Evidence

The Section 5 recovery requirements remain mandatory launch criteria.

Before MVP launch, each canonical datastore must have at least one successful full restore test from production-like backup material:

```text
Identity PostgreSQL
Tracking PostgreSQL
Catalog MongoDB
```

The recorded evidence must include:

- Backup/recovery point selected.
- Restore procedure used.
- Start/end time.
- Achieved RPO.
- Achieved RTO.
- Integrity/smoke checks.
- PASS/FAIL result.

The target remains:

| Canonical store | RPO | RTO |
|---|---:|---:|
| Identity PostgreSQL | `<= 5 min` | `<= 60 min` |
| Tracking PostgreSQL | `<= 5 min` | `<= 60 min` |
| Catalog MongoDB | `<= 15 min` | `<= 120 min` |

A backup that has never been successfully restored does not satisfy the launch gate.

---

## 12.12 Observability Verification

Before launch, dashboards and alerts themselves must be tested.

Verification must prove that intentionally created test conditions become visible as expected.

At minimum:

```text
latency SLO breach
controlled 5xx generation
Queue Lag >= alert threshold
Outbox Age >= alert threshold
new DLQ message
provider failure / Circuit Open
Import job failure/stall
canonical datastore readiness failure
```

The test is not complete merely because the metric exists.

The correct alert must fire with sufficient context to begin diagnosis, and the alert must clear/resolve appropriately after the test condition is removed according to the selected alerting system's behavior.

Alert tests must not require exposing passwords, tokens, emails, or sensitive profile data in logs or notifications.

---

## 12.13 Data-Retention Verification

Retention policies from Section 9 must have executable cleanup behavior and evidence.

Tests must verify representative lifecycle cases such as:

```text
processed Inbox record -> eligible after 7 days
published Outbox record -> eligible after 7 days
unpublished Outbox record -> NOT age-purged
HTTP idempotency result -> 24-hour retention
Import temporary file -> removed <= 24 hours after no longer required
terminal Import staging -> removed <= 24 hours when safe
terminal Import metadata -> 30-day retention
DLQ item -> operational resolution before normal 14-day maximum
```

Cleanup tests must prove bounded/idempotent behavior and must never delete canonical product data merely because infrastructure retention elapsed.

---

## 12.14 Contract & Migration Verification

Milestone 5B must also preserve the engineering gates already required by the roadmap.

Before launch:

- Approved OpenAPI contracts must pass compatibility verification.
- Integration Event/Command contracts must pass producer/consumer compatibility tests.
- Database migrations/bootstrap must execute successfully from a clean environment.
- Deployment-time migration verification must pass.
- Post-deployment smoke tests must pass through the Gateway.
- Architecture Tests must remain green.

NFR compliance does not override contract or architecture compliance.

All gates must pass together.

---

## 12.15 NFR Evidence Matrix

The release checklist must include named evidence rather than an unchecked statement such as "performance tested."

Minimum evidence mapping:

| NFR area | Required verification evidence |
|---|---|
| API latency | Load-test report with `p50` / `p95` / `p99` and workload definition |
| Availability / `5xx` | Error/availability dashboard plus controlled-failure evidence |
| Provider resilience | Timeout/retry/backoff/Circuit Breaker fault-injection result |
| RabbitMQ degradation | Broker-outage + Outbox preservation + recovery result |
| Queue Lag / Outbox Age | Dashboard/alert test crossing defined thresholds |
| DLQ | Poison-message isolation + alert + controlled handling evidence |
| Import limits | Boundary tests for `5 MiB`, request cap, and `10,000` entries |
| Scalability/capacity | Production-like capacity baseline and identified bottleneck |
| Liveness/Readiness | Dependency-failure health-check matrix result |
| Data durability | Restore-test evidence with achieved RPO/RTO for all canonical stores |
| Retention | Cleanup lifecycle test evidence |
| Privacy observability | Log/trace review showing sensitive-data exclusions |
| Profile privacy degradation | Identity Fail-Closed / Tracking degraded-profile test |
| Contracts | OpenAPI + integration-contract compatibility results |
| Deployment | Clean-environment migration + smoke-test result |

Evidence may be generated by different tools, but the pass/fail requirement is tool-independent.

---

## 12.16 Launch-Gate Failure Rule

Milestone 5B does **not** pass when a critical NFR is known to fail.

The following are launch blockers until resolved or explicitly re-reviewed through the appropriate architecture/product process:

```text
critical security or privacy failure
canonical data-integrity failure
unrestorable canonical datastore
RPO/RTO materially outside approved target
core API incapable of meeting accepted latency/availability target at launch load
RabbitMQ failure causing loss of required Outbox-backed business facts
unbounded duplicate effects under broker redelivery
Identity failure exposing Tracking data through Profile BFF
oversized Import bypassing defensive limits
critical alert condition not observable/actionable
```

A test failure must not be converted into a PASS by lowering the NFR silently.

If a target is genuinely no longer appropriate, it requires explicit review and document update before launch.

---

## 12.17 Milestone 5B Final NFR Gate

The NFR portion of Milestone 5B passes only when:

```text
Architecture implemented
        |
        v
Production-like staging deployed
        |
        v
Load / capacity tests PASS
        |
        v
Fault-injection / resilience tests PASS
        |
        v
Restore tests PASS
        |
        v
Observability + alerts verified
        |
        v
Retention / limits verified
        |
        v
Contract + migration + smoke gates PASS
        |
        v
No unresolved critical security/data-integrity issue
        |
        v
MVP NFR LAUNCH GATE = PASS
```

This is the difference between:

```text
"The architecture should be resilient."
```

and:

```text
"The implemented Shiori system demonstrated the required resilience under controlled verification."
```

---

---

# 13. STEP 8 Completion Gate

`NON_FUNCTIONAL_REQUIREMENTS.md` is the canonical STEP 8 quality contract for Shiori.

The design phase is complete because the following requirements are now accepted:

```text
[x] SLI, SLO, and SLA terminology is defined.
[x] No commercial SLA is introduced.
[x] API latency classes and p50/p95/p99 budgets are defined.
[x] 99.9% monthly availability SLO is defined for core capability families.
[x] Dependency degradation behavior is explicit.
[x] Provider timeout, retry, backoff, jitter, and Circuit Breaker policies are defined.
[x] Canonical datastore RPO/RTO targets are defined.
[x] Monthly Restore Tests are mandatory.
[x] Public request and Import defensive limits are defined.
[x] RabbitMQ Queue Lag, Outbox Age, Inbox, and DLQ health requirements are defined.
[x] Structured logging, tracing, privacy, Liveness, and Readiness requirements are defined.
[x] Infrastructure and temporary-data retention policies are defined.
[x] Workload profiles and scaling signals are defined without speculative replica counts.
[x] Production-like capacity-baseline requirements are defined.
[x] Dashboard coverage and actionable alerting rules are defined.
[x] No-alert-fatigue rule is defined.
[x] Milestone 5B verification and evidence requirements are defined.
[x] Controlled fault-injection / Chaos Testing is required in isolated production-like staging.
[x] Load, degradation, recovery, retention, contract, migration, and Restore Test evidence is required.
[x] No unresolved NFR contradicts the accepted Shiori architecture.
[x] No speculative infrastructure has been introduced.
[x] STEP 8 — NON-FUNCTIONAL REQUIREMENTS — COMPLETE.
```

## 13.1 Change Control

The numeric targets and behavioral requirements in this document are accepted engineering requirements for the MVP architecture.

They may evolve later when production evidence justifies a change, but they MUST NOT be weakened silently to make a failing implementation appear compliant.

A future change that materially affects:

- Security or privacy.
- Canonical-data durability.
- Service ownership.
- Cross-service consistency.
- Public API semantics.
- Integration-message guarantees.
- MVP launch-gate safety.

requires the corresponding architecture or product review before the new requirement becomes authoritative.

## 13.2 Implementation Relationship

STEP 8 defines the target.

Implementation milestones must build toward these targets continuously, and Milestone 5B must provide the final production-like PASS/FAIL evidence.

```text
STEP 8
Define measurable quality requirements
        |
        v
Implementation milestones
Build and observe against those requirements
        |
        v
Milestone 5B
Prove them with repeatable evidence
        |
        v
MVP NFR LAUNCH GATE = PASS
```

---

**End of Shiori — Non-Functional Requirements**
