---
id: TASK-038
title: Remove inherited dead code and enforce repository formatting
status: done
priority: high
epic: EPIC-modernization
dependsOn:
  - TASK-037
tags:
  - cleanup
  - formatting
  - tests
  - maintainability
createdAt: 2026-08-08T16:19:59.037Z
updatedAt: 2026-08-08T17:29:21.019Z
milestone: MILESTONE-library-quality
refinementState: ready
---

Remove unused inherited implementation and make mechanical consistency enforceable in CI.

## Acceptance criteria

- [x] Unused `PropertyActivator`, dead view-start flow, unused compiler methods, stale commented code,
      obsolete regions/comments, and unnecessary internal virtuality are removed.
- [x] `FastPropertySetter` retains only behavior used by the runtime after injection-plan refactoring,
      with focused tests for the remaining delegate generation.
- [x] Core and precompile tests use one test framework unless a documented tool constraint requires
      both.
- [x] Source encoding, line endings, whitespace, and using placement match `.editorconfig` and
      `.gitattributes` without changing generated/public API behavior.
- [x] CI runs a deterministic formatting verification that passes on Windows and Linux checkouts.
- [x] The warning-free build, API/package compatibility checks, and all maintained tests remain green.

## Baseline findings

The repository contains an unused `PropertyActivator`, a no-op view-start path with a large commented
implementation, unused compiler overloads, and a 525-line reflection helper whose getter/property
enumeration features are no longer consumed. `dotnet format --verify-no-changes` currently reports
widespread inherited whitespace and encoding drift.

## Implementation notes

- Removed the unused property activator, no-op view-start path, unused compiler overloads, stale
  commented tests/callbacks, and virtual dispatch from the sealed internal renderer.
- Replaced the inherited 525-line reflection utility with a focused expression-compiled instance
  property setter. Existing setter tests and injection-plan tests cover its remaining runtime role.
- Migrated all 133 precompile tests from NUnit to xUnit, removed the NUnit packages, and explicitly
  disabled parallel execution for the CLI test assembly because it shares process-wide console state.
- Normalized C# source using placement, whitespace, UTF-8 encoding, and LF line endings using the
  repository configuration. CI now verifies whitespace and the configured imports diagnostics on
  every operating-system matrix entry.
- Updated dependency documentation to describe the single xUnit test stack.

## Verification

- `dotnet build RazorLight.sln --configuration Release --no-restore --warnaserror` (0 warnings)
- `dotnet test tests/RazorLight.Tests/RazorLight.Tests.csproj --framework net10.0 --configuration Release --no-build` (314 passed)
- `dotnet test tests/RazorLight.Precompile.Tests/RazorLight.Precompile.Tests.csproj --configuration Release --no-build` (133 passed)
- `dotnet format RazorLight.sln whitespace --no-restore --verify-no-changes`
- `dotnet format RazorLight.sln style --no-restore --verify-no-changes --diagnostics IDE0005 IDE0065`
- Packed all three release artifacts with warnings as errors and validated package/symbol layout with
  `scripts/Validate-Packages.ps1`.
