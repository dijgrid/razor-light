---
id: MILESTONE-library-quality
title: Library Quality and API Design
targetDate: 2027-03-31
status: active
owner: justin
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-07T03:54:00Z
---

Modernize the public .NET library contract and strengthen correctness, security, diagnostics, and
performance evidence after the foundation release work.

## Deliverables

- Nullable analysis across non-public projects followed by reviewed public API annotations
- Cancellation-aware asynchronous compilation, lookup, and rendering
- An explicit trust model for executable templates and metadata references
- Enforced coverage ratchets and repeatable compiler/render/cache benchmarks
- A reviewed next-major API cleanup plan backed by package compatibility validation

## Success criteria

- TASK-009, TASK-013, TASK-015, TASK-017, TASK-018, and TASK-024 are complete.
- Public API changes have migration notes and machine-readable compatibility evidence.
- Security and performance claims are backed by tests or benchmarks rather than assumptions.
