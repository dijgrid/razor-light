---
id: TASK-006
title: Resolve implementation warnings and obsolete APIs
status: todo
priority: medium
epic: EPIC-modernization
milestone: MILESTONE-modernization-foundation
dependsOn:
  - TASK-003
  - TASK-004
tags:
  - cleanup
  - compiler
  - analyzers
createdAt: 2026-08-06T00:00:00Z
updatedAt: 2026-08-06T00:00:00Z
---

Remove obsolete runtime assumptions and establish a manageable compiler and analyzer baseline.

## Acceptance criteria

- [ ] `Assembly.CodeBase` usage is replaced with supported assembly location behavior.
- [ ] Existing warning suppressions are reviewed and narrowed or documented.
- [ ] Nullable reference types are evaluated project by project with a staged adoption plan.
- [ ] Dead code and unreachable target-framework conditions are removed.
- [ ] Formatting and analyzer rules are automated without a repository-wide unrelated rewrite.
- [ ] Build output has no unexplained compiler or analyzer warnings.
