---
id: TASK-004
title: Centralize and secure package dependencies
status: todo
priority: high
epic: EPIC-modernization
milestone: MILESTONE-modernization-foundation
dependsOn:
  - TASK-003
tags:
  - dependencies
  - nuget
  - security
createdAt: 2026-08-06T00:00:00Z
updatedAt: 2026-08-06T00:00:00Z
---

Consolidate package version management, update direct and transitive dependencies, and eliminate
known security advisories.

## Acceptance criteria

- [ ] Package versions are centrally managed where practical.
- [ ] `Microsoft.Extensions.Caching.Memory` no longer reports GHSA-qj66-m88j-hmgj.
- [ ] Razor, Roslyn, hosting, caching, test, and tool dependencies are on maintained versions.
- [ ] Unused and redundant package references are removed.
- [ ] Restore uses only intended HTTPS package sources.
- [ ] NuGet audit completes without known high or critical severity advisories.
- [ ] Dependabot updates can be reviewed in coherent groups without excessive duplicate pull requests.

## Implementation notes

Use compatible version families rather than independently bumping closely coupled ASP.NET Core and
Razor compiler packages.

The 2026-08-06 baseline restore also reports high-severity advisories through inherited
`Microsoft.NETCore.App` 2.0 and 2.1 package dependencies in the tests and sample project. Removing
those application targets belongs with TASK-003 and must be confirmed by the audit in this task.
