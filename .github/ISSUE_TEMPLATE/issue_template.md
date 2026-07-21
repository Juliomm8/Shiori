---
name: Engineering issue
about: Report a defect or propose a scoped engineering change
title: ""
labels: "status: needs-triage"
assignees: ""
---

> Do not disclose security vulnerabilities in public issues. Use private vulnerability reporting instead.

## Summary

<!-- Describe the problem, proposal, or required outcome. -->

## Issue Type

- [ ] Bug
- [ ] Feature
- [ ] Enhancement
- [ ] Refactor
- [ ] Test improvement
- [ ] Documentation
- [ ] Infrastructure or maintenance
- [ ] Security
- [ ] Performance

## Problem or Context

<!-- Explain what is happening, why it matters, and what is affected. -->

## Expected Outcome

<!-- Describe the result that should exist when this issue is complete. -->

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
- [ ] Unknown or cross-cutting

## Proposed Scope

### In Scope

-

### Out of Scope

-

## Acceptance Criteria

<!-- Write testable outcomes. -->

- [ ]
- [ ]
- [ ]

## Reproduction Steps

<!-- Complete this section only for defects. -->

1.
2.
3.

### Current Behavior

### Expected Behavior

## Technical Considerations

### API Impact

- [ ] No API impact
- [ ] Additive API change
- [ ] Breaking API change
- [ ] OpenAPI update required

### Data Impact

- [ ] No data-model impact
- [ ] PostgreSQL migration required
- [ ] MongoDB migration or bootstrap change required
- [ ] Existing data migration required

### Messaging Impact

- [ ] No messaging impact
- [ ] New integration event
- [ ] Existing event contract change
- [ ] Consumer projection change
- [ ] Outbox or Inbox behavior affected

### Architecture Impact

- [ ] No architecture change
- [ ] Existing ADR covers the decision
- [ ] New or updated ADR required
- [ ] Architecture review required before implementation

## Dependencies and Blockers

- Depends on:
- Blocks:
- Related to:

## Validation Plan

- [ ] Unit tests
- [ ] Integration tests
- [ ] Contract tests
- [ ] End-to-end test through the API Gateway
- [ ] Manual Docker Compose verification
- [ ] Load or resilience testing
- [ ] Security review

## Definition of Done

- [ ] Acceptance criteria are satisfied.
- [ ] Tests are added or updated.
- [ ] CI passes.
- [ ] API and event compatibility are reviewed.
- [ ] Database changes are versioned and repeatable.
- [ ] Logging, metrics, tracing, and health checks are addressed.
- [ ] Documentation is updated.
- [ ] No unresolved security or data-integrity concern remains.

## Additional Context

<!-- Add relevant logs, diagrams, references, or screenshots. -->
