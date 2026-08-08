---
id: TASK-038
title: Remove inherited dead code and enforce repository formatting
status: todo
priority: high
epic: EPIC-modernization
dependsOn:
  - TASK-037
tags:
  - cleanup
  - formatting
  - tests
  - maintainability
createdAt: 2026-08-08T16:19:59.037Z
updatedAt: 2026-08-08T16:20:27.633Z
milestone: MILESTONE-library-quality
refinementState: ready
---

Remove unused inherited implementation and make mechanical consistency enforceable in CI.

## Acceptance criteria

- [ ] Unused `PropertyActivator`, dead view-start flow, unused compiler methods, stale commented code,
      obsolete regions/comments, and unnecessary internal virtuality are removed.
- [ ] `FastPropertySetter` retains only behavior used by the runtime after injection-plan refactoring,
      with focused tests for the remaining delegate generation.
- [ ] Core and precompile tests use one test framework unless a documented tool constraint requires
      both.
- [ ] Source encoding, line endings, whitespace, and using placement match `.editorconfig` and
      `.gitattributes` without changing generated/public API behavior.
- [ ] CI runs a deterministic formatting verification that passes on Windows and Linux checkouts.
- [ ] The warning-free build, API/package compatibility checks, and all maintained tests remain green.

## Baseline findings

The repository contains an unused `PropertyActivator`, a no-op view-start path with a large commented
implementation, unused compiler overloads, and a 525-line reflection helper whose getter/property
enumeration features are no longer consumed. `dotnet format --verify-no-changes` currently reports
widespread inherited whitespace and encoding drift.
