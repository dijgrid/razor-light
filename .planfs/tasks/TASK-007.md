---
id: TASK-007
title: Refresh documentation and samples
status: done
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
updatedAt: 2026-08-07T01:45:00Z
---

Update user guidance and sample projects to reflect the maintained framework, dependency, and support
baseline.

## Acceptance criteria

- [x] README setup and compatibility guidance matches supported targets.
- [x] Stale .NET Core 2.x, 3.x, and .NET 5 instructions are removed or clearly historical.
- [x] Samples build and run on supported frameworks.
- [x] Unsupported-scenario claims are revalidated against current ASP.NET Core behavior.
- [x] Generated README workflow is documented and reproducible.
- [x] Public API examples are covered by compile or smoke tests where practical.

## Implementation notes

The generated README now distinguishes the historical `2.3.1` NuGet package from this maintained
.NET 10 source line, links the compatibility and maintenance policies, and replaces inherited
blanket failure claims with the scenarios actually exercised by the repository. The raw-string
quickstart is an executable xUnit smoke test and demonstrates that a project is only required for
project lookup, layouts, and includes.

The Entity Framework sample uses an asynchronous entry point and is executed in CI. The Azure
Functions v4 isolated-worker sample resolves copied content from `AppContext.BaseDirectory`, is
build-validated in CI, and no longer carries the obsolete Functions 3.x workaround. Package
metadata includes the generated README, and `samples/README.md` records the exact run/build commands.

Validation completed with a zero-warning solution build, 183 xUnit tests, 118 NUnit tests, a
successful Entity Framework sample run, a zero-warning Functions sample build, byte-identical README
regeneration, and a package containing `README.md`, `LICENSE`, and the `net10.0` library assets.
