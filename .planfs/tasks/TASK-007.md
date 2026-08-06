---
id: TASK-007
title: Refresh documentation and samples
status: todo
priority: medium
epic: EPIC-modernization
milestone: MILESTONE-modernization-foundation
dependsOn:
  - TASK-003
  - TASK-004
tags:
  - documentation
  - samples
  - onboarding
createdAt: 2026-08-06T00:00:00Z
updatedAt: 2026-08-06T00:00:00Z
---

Update user guidance and sample projects to reflect the maintained framework, dependency, and support
baseline.

## Acceptance criteria

- [ ] README setup and compatibility guidance matches supported targets.
- [ ] Stale .NET Core 2.x, 3.x, and .NET 5 instructions are removed or clearly historical.
- [ ] Samples build and run on supported frameworks.
- [ ] Unsupported-scenario claims are revalidated against current ASP.NET Core behavior.
- [ ] Generated README workflow is documented and reproducible.
- [ ] Public API examples are covered by compile or smoke tests where practical.
