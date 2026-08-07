---
id: TASK-009
title: Adopt nullable reference types incrementally
status: todo
priority: medium
epic: EPIC-modernization
milestone: MILESTONE-modernization-foundation
dependsOn:
  - TASK-006
tags:
  - cleanup
  - nullable
  - compatibility
createdAt: 2026-08-07T00:35:00Z
updatedAt: 2026-08-07T00:35:00Z
---

Enable nullable reference types project by project without obscuring compatibility changes in the
public library.

## Acceptance criteria

- [x] Precompile test fixture nullability warnings are resolved and its temporary `NoWarn` is removed.
- [ ] Nullable reference types are enabled in the sandbox, samples, and precompile tool.
- [ ] The legacy xUnit suite is annotated without changing test behavior.
- [ ] Public library annotations are reviewed as compatibility-sensitive API changes.
- [ ] The public API fingerprint and package behavior baselines pass after annotation changes.
