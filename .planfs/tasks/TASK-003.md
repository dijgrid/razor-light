---
id: TASK-003
title: Move to supported target frameworks
status: todo
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
updatedAt: 2026-08-06T00:00:00Z
---

Replace end-of-life application targets with an intentional framework matrix based on supported .NET
releases and required library compatibility.

## Acceptance criteria

- [ ] The library target-framework policy is documented, including whether `netstandard2.0` remains.
- [ ] .NET Core 3.1, .NET 5, and .NET 6 application targets are removed.
- [ ] Maintained projects build against the selected supported .NET LTS target or targets.
- [ ] Samples and tools use supported target frameworks.
- [ ] CI installs only supported SDKs and runtimes after the migration.
- [ ] Consumer migration notes explain any dropped targets or changed runtime requirements.

## Implementation notes

.NET 10 is the current active LTS baseline. Preserve `netstandard2.0` only if its compatibility value
outweighs the dependency and test complexity and it can be kept secure.
