---
id: TASK-025
title: Simplify shared project configuration
status: done
priority: low
createdAt: 2026-08-07T19:20:37.266Z
updatedAt: 2026-08-07T19:24:39.145Z
tags:
  - build
  - configuration
  - maintenance
refinementState: ready
---

Reduce repeated SDK properties without removing the root files that enforce repository-wide build,
restore, formatting, packaging, and toolchain policy.

## Acceptance criteria

- [x] `TargetFramework` and nullable analysis are declared once for all maintained projects.
- [x] Package and application behavior, including packability and evaluated versions, is unchanged.
- [x] Redundant package-property and item-group declarations are removed.
- [x] Root SDK, NuGet-source, central-package, Git, and editor configuration remains explicit.
- [x] Restore, warning-as-error build, maintained tests, package validation, and PlanFS validation pass.

## Implementation notes

- Moved the repository-wide .NET 10 target and nullable-analysis setting into
  `Directory.Build.props`, removing duplicate declarations from all eight maintained project files.
- Removed the redundant `Version` assignment because the SDK derives it from `VersionPrefix`, and
  combined the identical package-only item groups.
- Compared evaluated target framework, nullable mode, packability, version, and package identity
  before and after the change. All values are unchanged, including the generated Azure Functions
  worker project's explicit .NET 8 override.
- Verified solution and Functions-sample restore/build, both maintained test suites, deterministic
  outputs, and both NuGet package layouts and Source Link artifacts.
