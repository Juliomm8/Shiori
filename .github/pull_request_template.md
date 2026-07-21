## Summary

<!-- Explain what this pull request changes. -->

## Motivation

<!-- Explain why this change is needed. Link the related issue when applicable. -->

Closes #

## Affected Components

- [ ] Identity Service
- [ ] Catalog Service
- [ ] Tracking Service
- [ ] YARP API Gateway
- [ ] Shared contracts
- [ ] RabbitMQ messaging
- [ ] PostgreSQL
- [ ] MongoDB
- [ ] Docker or local infrastructure
- [ ] CI/CD
- [ ] Documentation

## Change Type

- [ ] Bug fix
- [ ] New feature
- [ ] Enhancement
- [ ] Refactor
- [ ] Test improvement
- [ ] Documentation
- [ ] Infrastructure or maintenance
- [ ] Breaking change

## Implementation

<!-- Summarize the main technical decisions and implementation details. -->

## Architecture and Compatibility

### Architecture

- [ ] The change follows the accepted service boundaries.
- [ ] No service directly reads or writes another service's database.
- [ ] A new or updated ADR is included when required.
- [ ] No architectural documentation is required.

### API Contracts

- [ ] No public API contract changed.
- [ ] The change is backward-compatible.
- [ ] OpenAPI documentation was updated.
- [ ] RFC 9457 Problem Details behavior was reviewed.
- [ ] This change intentionally introduces a breaking API change.

### Integration Events

- [ ] No integration event changed.
- [ ] New or changed events are versioned.
- [ ] Producer and consumer compatibility was reviewed.
- [ ] Outbox and Inbox behavior was considered.
- [ ] Duplicate and out-of-order delivery was tested where applicable.

### Data

- [ ] No database schema changed.
- [ ] PostgreSQL migrations are included and repeatable.
- [ ] MongoDB indexes, validators, or migration scripts are included.
- [ ] Existing data and rollback behavior were considered.
- [ ] Data ownership remains inside the responsible service.

## Testing

<!-- Describe how the change was verified. -->

- [ ] Unit tests added or updated.
- [ ] Integration tests added or updated.
- [ ] Contract tests added or updated.
- [ ] End-to-end behavior verified through the API Gateway.
- [ ] Concurrency or idempotency behavior tested where applicable.
- [ ] Failure and retry behavior tested where applicable.
- [ ] Existing tests pass locally.

### Test Commands

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

## Operational Readiness

- [ ] Structured logging was reviewed.
- [ ] Metrics were added or updated where needed.
- [ ] Distributed tracing and correlation were reviewed.
- [ ] Health checks were updated where needed.
- [ ] Configuration is environment-specific.
- [ ] No secrets or credentials were committed.
- [ ] Docker images build successfully.
- [ ] The Docker Compose environment starts successfully.
- [ ] A rollback or forward-fix approach is understood.

## Security

- [ ] Authentication and authorization implications were reviewed.
- [ ] User-controlled input is validated.
- [ ] Sensitive data is not logged.
- [ ] File, XML, URL, or provider input is handled safely.
- [ ] Dependency changes were reviewed for vulnerabilities.
- [ ] No additional security review is required.

## Documentation

- [ ] README or contributor documentation updated.
- [ ] API documentation updated.
- [ ] ADR updated.
- [ ] FEATURES.md updated.
- [ ] ROADMAP.md updated.
- [ ] No documentation changes are required.

## Evidence

<!-- Add relevant API responses, test output, logs, traces, or screenshots. -->

## Final Checklist

- [ ] The pull request has a clear and focused scope.
- [ ] The branch is up to date with `main`.
- [ ] Formatting and analyzers pass.
- [ ] All required CI checks pass.
- [ ] Public contracts remain backward-compatible or are clearly marked.
- [ ] Reviewer comments have been resolved.
- [ ] The pull request is ready to merge.
