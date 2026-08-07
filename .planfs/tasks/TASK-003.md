---
id: TASK-003
title: Move to supported target frameworks
status: done
priority: high
epic: EPIC-modernization
milestone: MILESTONE-modernization-foundation
dependsOn:
  - TASK-002
tags:
  - dotnet
  - frameworks
  - compatibility
createdAt: 2026-08-06T00:00:00Z
updatedAt: 2026-08-06T23:18:00Z
---

Replace end-of-life application targets with an intentional framework matrix based on supported .NET
releases and required library compatibility.

## Acceptance criteria

- [x] The library target-framework policy is documented, including whether `netstandard2.0` remains.
- [x] .NET Core 3.1, .NET 5, and .NET 6 application targets are removed.
- [x] Maintained projects build against the selected supported .NET LTS target or targets.
- [x] Samples and tools use supported target frameworks.
- [x] CI installs only supported SDKs and runtimes after the migration.
- [x] Consumer migration notes explain any dropped targets or changed runtime requirements.

## Implementation notes

.NET 10 is the current active LTS baseline. Preserve `netstandard2.0` only if its compatibility value
outweighs the dependency and test complexity and it can be kept secure.

All maintained projects now target `net10.0`. The library no longer targets `netstandard2.0`
because that asset required the separate ASP.NET Core 2.1 Razor graph. Redundant framework-provided
package references were removed, the Azure Functions sample moved to the current isolated worker and
references the local project, and `docs/framework-support.md` records the policy and major-version
consumer migration requirements.
