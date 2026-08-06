---
id: TASK-002
title: Capture the compatibility and package baseline
status: todo
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
updatedAt: 2026-08-06T00:00:00Z
---

Record the behavior and package surface that existing users rely on before changing frameworks and
dependencies.

## Acceptance criteria

- [ ] Public API is captured in a reviewable baseline or compatibility test.
- [ ] Current NuGet package contents, dependencies, symbols, and Source Link metadata are inspected.
- [ ] Representative compilation, rendering, caching, includes, layouts, and error behavior have
      regression coverage.
- [ ] Current supported and unsupported scenarios are reconciled with tests and documentation.
- [ ] Compatibility risks for changing Razor and Roslyn versions are documented.
- [ ] A migration policy distinguishes intentional breaking changes from regressions.

## Implementation notes

Do not begin by mechanically updating every package. Establish what must remain compatible so later
changes can be evaluated against evidence.
