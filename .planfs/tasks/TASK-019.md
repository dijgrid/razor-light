---
id: TASK-019
title: Reduce or justify the ASP.NET Core runtime dependency
status: todo
priority: medium
epic: EPIC-modernization
milestone: MILESTONE-language-runtime-compatibility
dependsOn:
  - TASK-011
  - TASK-012
tags:
  - dependencies
  - deployment
  - packaging
  - compatibility
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-07T00:29:26Z
---

Determine whether a standalone template engine must require the full ASP.NET Core shared framework,
then minimize or clearly document that deployment requirement.

## Acceptance criteria

- [ ] An assembly and feature inventory identifies which `Microsoft.AspNetCore.App` components the
      library actually uses at compile time and runtime.
- [ ] Console, worker, desktop, framework-dependent, and self-contained consumers have publish and
      smoke-test coverage representative of the supported distribution model.
- [ ] Removing, splitting, or retaining the framework reference is recorded as an explicit
      compatibility and servicing decision.
- [ ] If the reference remains, package and README guidance clearly state the ASP.NET Core runtime
      installation requirement and self-contained alternative.
- [ ] If the reference is removed or split, Razor directives, tag helpers, encoding, buffering, and
      generated-template behavior retain regression coverage.
- [ ] Package size and deployment footprint are measured before and after the decision.
- [ ] No unsupported copy of framework assemblies is embedded merely to avoid a shared-framework
      requirement.

## Baseline findings

`RazorLight.csproj` currently carries a `FrameworkReference` to `Microsoft.AspNetCore.App`, so a
framework-dependent console or desktop consumer also requires the ASP.NET Core shared runtime.
Upstream issue [`#360`](https://github.com/toddams/RazorLight/issues/360) records this unresolved
distribution concern. The current compiler migration must be understood before changing the runtime
graph.
