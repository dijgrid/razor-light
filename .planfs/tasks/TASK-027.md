---
id: TASK-027
title: Remove inherited obsolete and unsupported APIs
status: review
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
updatedAt: 2026-08-08T06:16:26.742Z
refinementState: ready
---

Remove inherited public entry points that cannot work, have compile-time-error obsolescence, or
represent retired platform workarounds before the 3.0 beta surface becomes a compatibility promise.

## Acceptance criteria

- [x] Remove `EngineFactory`, `IEngineFactory`, the three obsolete render/compile overloads that
      throw `NotImplementedException`, and their public API entries.
- [x] Remove the obsolete `TemplateCompilationException` constructor after preserving its useful
      diagnostic information in the supported constructor.
- [x] Remove `LegacyFixAssemblyPathFormatter` and `UseNetFrameworkLegacyFix`; .NET Framework is not
      a supported target in the independent line.
- [x] Remove the redundant `IRazorLightEngineFactory` and
      `RazorLightEngineWithFileSystemProjectFactory` in favor of the builder and DI registration.
- [x] No public member remains both unusable and retained solely as a source-compatibility trap.
- [x] Migration examples identify the exact builder or supported overload replacement for every
      removed entry point.
- [x] Public API and package validation record every intentional break without a broad suppression.

## Scope

Do not redesign the main engine facade, cache abstraction, DI lifetime, or public compiler surface in
this task; TASK-028, TASK-021, and TASK-029 own those changes.

## Implementation notes

- Deleted the obsolete `EngineFactory`/`IEngineFactory` pair, redundant file-system factory pair,
  error-only compile/render overloads, and retired .NET Framework assembly-path workaround.
- Removed the string-only `TemplateCompilationException` constructor; the structured diagnostic
  constructor continues to retain messages, mapped paths, and source positions.
- Replaced compatibility-only tests with supported builder and DI coverage, updated the API baseline
  hash, and removed every deleted entry from `PublicAPI.Unshipped.txt`.
- Added an exact removed-to-supported migration table to `docs/api-design-3.0.md`. Package validation
  against `RazorLight` 2.3.1 passes with no new API suppression; only the existing retired-framework
  `PKV006` entries remain.
- Validated the solution build, 258 core tests, 122 precompile tests, package creation, PlanFS, and
  whitespace checks on .NET 10.
