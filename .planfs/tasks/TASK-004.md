---
id: TASK-004
title: Centralize and secure package dependencies
status: done
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
updatedAt: 2026-08-06T23:38:00Z
---

Consolidate package version management, update direct and transitive dependencies, and eliminate
known security advisories.

## Acceptance criteria

- [x] Package versions are centrally managed where practical.
- [x] `Microsoft.Extensions.Caching.Memory` no longer reports GHSA-qj66-m88j-hmgj.
- [x] Razor, Roslyn, hosting, caching, test, and tool dependencies are on maintained versions.
- [x] Unused and redundant package references are removed.
- [x] Restore uses only intended HTTPS package sources.
- [x] NuGet audit completes without known high or critical severity advisories.
- [x] Dependabot updates can be reviewed in coherent groups without excessive duplicate pull requests.

## Implementation notes

Use compatible version families rather than independently bumping closely coupled ASP.NET Core and
Razor compiler packages.

The 2026-08-06 baseline restore also reports high-severity advisories through inherited
`Microsoft.NETCore.App` 2.0 and 2.1 package dependencies in the tests and sample project. Removing
those application targets belongs with TASK-003 and must be confirmed by the audit in this task.

`Directory.Packages.props` now owns direct versions and `NuGet.config` limits restore to HTTPS
NuGet.org. Direct packages are current, direct and transitive audits report no vulnerabilities, and
the caching advisory is gone. The final Razor compiler package IDs remain at `6.0.36` as a documented
compatibility layer while their Roslyn runtime is current; `docs/dependency-policy.md` records that
constraint. Abandoned CLI/glob dependencies were removed from the precompile tool, and Dependabot
groups align with the central version families.
