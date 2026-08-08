---
id: TASK-027
title: Remove inherited obsolete and unsupported APIs
status: todo
priority: high
epic: EPIC-modernization
milestone: MILESTONE-library-quality
dependsOn:
  - TASK-018
tags:
  - api
  - compatibility
  - cleanup
  - beta
createdAt: 2026-08-08T04:17:53.038Z
updatedAt: 2026-08-08T04:17:53.038Z
refinementState: ready
---

Remove inherited public entry points that cannot work, have compile-time-error obsolescence, or
represent retired platform workarounds before the 3.0 beta surface becomes a compatibility promise.

## Acceptance criteria

- [ ] Remove `EngineFactory`, `IEngineFactory`, the three obsolete render/compile overloads that
      throw `NotImplementedException`, and their public API entries.
- [ ] Remove the obsolete `TemplateCompilationException` constructor after preserving its useful
      diagnostic information in the supported constructor.
- [ ] Remove `LegacyFixAssemblyPathFormatter` and `UseNetFrameworkLegacyFix`; .NET Framework is not
      a supported target in the independent line.
- [ ] Remove the redundant `IRazorLightEngineFactory` and
      `RazorLightEngineWithFileSystemProjectFactory` in favor of the builder and DI registration.
- [ ] No public member remains both unusable and retained solely as a source-compatibility trap.
- [ ] Migration examples identify the exact builder or supported overload replacement for every
      removed entry point.
- [ ] Public API and package validation record every intentional break without a broad suppression.

## Scope

Do not redesign the main engine facade, cache abstraction, DI lifetime, or public compiler surface in
this task; TASK-028, TASK-021, and TASK-029 own those changes.
