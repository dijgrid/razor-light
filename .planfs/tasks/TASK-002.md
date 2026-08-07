---
id: TASK-002
title: Capture the compatibility and package baseline
status: done
priority: high
epic: EPIC-modernization
milestone: MILESTONE-modernization-foundation
dependsOn:
  - TASK-001
tags:
  - compatibility
  - api
  - packaging
  - baseline
createdAt: 2026-08-06T00:00:00Z
updatedAt: 2026-08-06T23:04:00Z
---

Record the behavior and package surface that existing users rely on before changing frameworks and
dependencies.

## Acceptance criteria

- [x] Public API is captured in a reviewable baseline or compatibility test.
- [x] Current NuGet package contents, dependencies, symbols, and Source Link metadata are inspected.
- [x] Representative compilation, rendering, caching, includes, layouts, and error behavior have
      regression coverage.
- [x] Current supported and unsupported scenarios are reconciled with tests and documentation.
- [x] Compatibility risks for changing Razor and Roslyn versions are documented.
- [x] A migration policy distinguishes intentional breaking changes from regressions.

## Implementation notes

Do not begin by mechanically updating every package. Establish what must remain compatible so later
changes can be evaluated against evidence.

The inherited `2.3.1` package was packed from the detached repository baseline and inspected before
framework or dependency changes. `docs/compatibility-baseline.md` records its assets, dependency
groups, missing symbol package, Source Link limitation, behavior evidence, unsupported-claim status,
and migration policy. `PublicApiBaselineTest` fingerprints a deterministic reflection description of
the exported API so later tasks cannot change it silently.
