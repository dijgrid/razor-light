---
id: MILESTONE-release-readiness
title: Independent Release Readiness
targetDate: 2026-10-31
status: active
owner: justin
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-10T17:29:07Z
---

Establish package ownership, compatibility validation, dependency automation, and protected release
controls before publishing the first independently maintained RazorLight packages.

The first publication is planned as `3.0.0-beta.1` so the generic-core and API reset can receive
consumer feedback before the stable 3.0 compatibility boundary is frozen.

## Deliverables

- A documented package identity, ownership, and versioning decision
- Reproducible package and symbol artifacts with API compatibility validation
- Trusted publishing or minimally scoped release credentials with maintainer approval
- Dependabot behavior aligned with central package management and a clean default-branch audit
- A reviewed 3.0 migration guide and validated beta package set
- A stable-tag gate covering complete feature documentation, executable examples, the frozen public
  API boundary, and measured large-input/high-cardinality scaling

## Success criteria

- TASK-008, TASK-016, and TASK-020 are complete before release automation is enabled; TASK-030 owns
  the first beta publication after its API-cleanup dependencies complete; TASK-039 owns the final
  documentation, API, performance, and memory evaluation before the stable tag.
- No package is published before an artifact review and explicit release approval.
- Package compatibility changes are reviewable rather than represented only by a hash.
- GitHub and NuGet release controls do not depend on a long-lived broad-scope secret.
