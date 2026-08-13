# Shiori Test Organization

This directory contains Shiori's automated test projects.

The testing model follows the architecture defined by ADR-012 and the Milestone 1 testing backlog.

The structure described below is the **approved target organization**, not a requirement to create every project immediately.

> **A test project is created only when the first real test belonging to that category exists.**

Do not create empty `.csproj` files, empty directories, `.gitkeep` files, or trivial tests only to make the repository look symmetrical.

---

## Target Structure

The approved testing structure is:

```text
tests/
├── Services/
│   ├── Identity/
│   │   ├── Shiori.Identity.UnitTests/
│   │   ├── Shiori.Identity.IntegrationTests/
│   │   └── Shiori.Identity.ContractTests/
│   ├── Catalog/
│   │   ├── Shiori.Catalog.UnitTests/
│   │   ├── Shiori.Catalog.IntegrationTests/
│   │   └── Shiori.Catalog.ContractTests/
│   └── Tracking/
│       ├── Shiori.Tracking.UnitTests/
│       ├── Shiori.Tracking.IntegrationTests/
│       └── Shiori.Tracking.ContractTests/
├── Gateway/
│   └── Shiori.Gateway.IntegrationTests/
├── Architecture/
│   └── Shiori.ArchitectureTests/
└── EndToEnd/
    └── Shiori.EndToEnd.Tests/
```

This tree represents where future tests belong when those responsibilities become active.

Directories and projects that do not yet contain a real test responsibility must not be created in advance.

---

## Unit Tests

Unit Tests verify isolated Domain and Application behavior.

They should cover focused business rules, value objects, domain behavior, Application use cases, validation, and other logic that does not require production infrastructure.

Unit Tests must not require:

* PostgreSQL.
* MongoDB.
* RabbitMQ.
* Docker.
* AniList.
* MangaDex.
* Other live external providers.
* Production infrastructure.

Infrastructure-specific behavior does not belong in Unit Tests.

The expected projects are:

```text
Shiori.Identity.UnitTests
Shiori.Catalog.UnitTests
Shiori.Tracking.UnitTests
```

Each project is created only when its bounded context has its first real Unit Test.

---

## Integration Tests

Integration Tests verify behavior at real infrastructure boundaries.

When production behavior depends on a specific database or broker technology, Integration Tests must exercise that actual technology rather than an easier substitute with different behavior.

Examples include:

```text
Identity  -> PostgreSQL
Catalog   -> MongoDB
Tracking  -> PostgreSQL
Messaging -> RabbitMQ
```

PostgreSQL integration behavior must not be replaced by:

```text
EF Core InMemory
SQLite
```

when PostgreSQL-specific behavior is what the test is intended to verify.

MongoDB and RabbitMQ tests must likewise use their real technologies when those capabilities become active.

Containerized dependencies may be used when a real Integration Test requires them, but container infrastructure must not be introduced speculatively before such a test exists.

The expected projects are:

```text
Shiori.Identity.IntegrationTests
Shiori.Catalog.IntegrationTests
Shiori.Tracking.IntegrationTests
Shiori.Gateway.IntegrationTests
```

Gateway Integration Tests verify Gateway-owned integration behavior. They do not turn Gateway into a business service.

---

## Contract Tests

Contract Tests protect compatibility at boundaries exposed to other components or clients.

Public HTTP Contract Tests may protect behavior such as:

* Public routes.
* HTTP methods.
* Status codes.
* Request DTOs.
* Response DTOs.
* JSON contracts.
* RFC 9457 Problem Details.
* Required headers.
* OpenAPI output.
* Backward compatibility.

Integration-contract tests may protect versioned asynchronous contracts when those contracts become active.

Contract Tests do not replace Unit Tests, Integration Tests, End-to-End Tests, or Architecture Tests.

The expected projects are:

```text
Shiori.Identity.ContractTests
Shiori.Catalog.ContractTests
Shiori.Tracking.ContractTests
```

A Contract Test project is created only when the bounded context exposes its first real contract that requires automated compatibility protection.

---

## End-to-End Tests

End-to-End Tests verify Shiori as a black box through its public entry point.

The expected project is:

```text
Shiori.EndToEnd.Tests
```

E2E tests enter the system through YARP rather than directly invoking internal service implementations.

By default, the E2E project must not use `ProjectReference` dependencies on production implementation projects.

Its perspective should remain equivalent to that of an external Shiori client:

```text
Client
  -> YARP
  -> Shiori backend
  -> public response
```

Internal implementation details must not become part of the E2E test contract merely for convenience.

---

## Architecture Tests

Architecture Tests are separate from Unit, Integration, Contract, and End-to-End testing.

Shiori uses one global architecture-governance project:

```text
tests/Architecture/Shiori.ArchitectureTests
```

Its responsibility is to protect frozen architectural rules such as project boundaries, dependency direction, production-project registration, and prohibited technology leakage.

`Shiori.ArchitectureTests` belongs to M1-002.

M1-003 does not move, duplicate, replace, or mix Architecture Tests with other test categories.

---

## External Provider Testing

Deterministic CI must not depend on live AniList or MangaDex availability.

When provider adapters are implemented in later work, their automated tests should use controlled inputs such as:

* Recorded fixtures.
* Deterministic response samples.
* Controlled HTTP stubs.
* Other isolated provider-boundary mechanisms.

Live provider availability, rate limits, latency, or remote data changes must not determine whether the normal automated test suite passes.

Provider test infrastructure must not be created before a real provider adapter and test responsibility exist.

---

## Test Project Creation Rule

Before adding a new test project, all of the following must be true:

1. A real test exists that belongs to that test category.
2. The project name matches the approved naming model.
3. The test responsibility cannot be placed more correctly in an existing category.
4. Required dependencies match the responsibility of that category.
5. No production infrastructure leaks into Unit Tests.
6. Integration Tests use the real production technology where its behavior matters.
7. E2E tests remain black-box through YARP by default.

The presence of another bounded context's test project is not sufficient justification to create a matching project for symmetry.

For example:

```text
Shiori.Identity.IntegrationTests exists
```

does **not** imply that these must also be created immediately:

```text
Shiori.Catalog.IntegrationTests
Shiori.Tracking.IntegrationTests
```

They appear when Catalog or Tracking has a real Integration Test responsibility.

---

## Test Tooling

The testing responsibilities and boundaries are architectural decisions.

Specific helper libraries are not.

Do not introduce a mandatory repository-wide choice of:

* Assertion library.
* Mocking framework.
* Container helper library.
* HTTP stubbing library.
* Test fixture framework.

until real test requirements justify that decision.

Tooling may evolve while the responsibilities defined in this document remain stable.

---

## Current Baseline

At the completion of M1-002, the only active test project is:

```text
tests/
└── Architecture/
    └── Shiori.ArchitectureTests/
```

That is intentional.

Future Unit, Integration, Contract, Gateway, and End-to-End projects will be introduced incrementally as their first real tests appear.
