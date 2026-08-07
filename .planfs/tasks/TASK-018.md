---
id: TASK-018
title: Design the next-major public API cleanup
status: todo
priority: medium
epic: EPIC-modernization
milestone: MILESTONE-library-quality
dependsOn:
  - TASK-016
  - TASK-024
tags:
  - api
  - cleanup
  - compatibility
  - documentation
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-07T04:01:08.620Z
refinementState: needs-refinement
---

Inventory inherited public contracts and design a coherent next-major surface after nullable and
package compatibility validation are available.

## Acceptance criteria

- [ ] Public obsolete members, mutable option collections, implementation types, naming issues,
      sync-over-async risks, and error-only stubs are inventoried with usage evidence.
- [ ] Public methods marked obsolete with `error: true` no longer remain as undocumented
      `NotImplementedException` traps in the next-major contract.
- [ ] Interfaces expose only behavior that every supported implementation can fulfill.
- [ ] Builder, dependency-injection, project, cache, compiler, and rendering abstractions have clear
      ownership and lifetime documentation.
- [ ] Proposed removals and replacements include source and binary migration examples.
- [ ] Package validation and nullable API baselines enumerate every intentional break.
- [ ] The cleanup is split into reviewable implementation tasks instead of one repository-wide rewrite.

## Baseline findings

The assembly still exposes inherited members that are both obsolete with `error: true` and implemented
only by throwing `NotImplementedException`. Several public interfaces also force incomplete providers
to throw for operations they cannot support. The independent next major is the appropriate boundary
for deliberate cleanup, but only after compatibility tooling makes the impact visible.
