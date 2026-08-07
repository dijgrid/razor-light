---
id: TASK-016
title: Add package and API compatibility validation
status: done
priority: high
epic: EPIC-modernization
milestone: MILESTONE-release-readiness
dependsOn:
  - TASK-008
tags:
  - api
  - packaging
  - compatibility
  - ci
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-07T07:13:22.166Z
refinementState: ready
---

Use supported .NET package validation to make public API and package-layout changes reviewable before
publishing the independent release line.

## Implementation readiness

Ready for implementation. TASK-008 selected `Dijgrid.RazorLight` 3.0.0 as the independent package
and `RazorLight` 2.3.1 as the inherited compatibility baseline. The .NET 10 SDK cannot validate the
retired baseline frameworks directly, so those framework removals must be explicit suppressions while
the source API inventory protects the inherited member surface.

## Acceptance criteria

- [x] `EnablePackageValidation` runs during pack for the chosen package identity.
- [x] The baseline package and version follow TASK-008's ownership and versioning decision.
- [x] Intentional framework and API breaks from the inherited `2.3.1` line are represented by reviewed
      compatibility suppressions and migration notes.
- [x] A human-readable shipped/unshipped API record or equivalent review artifact supplements or
      replaces the current hash-only reflection baseline.
- [x] Package contents, reference assemblies, implementation assemblies, symbols, and framework groups
      are validated in CI.
- [x] Accidental binary breaks fail CI while approved next-major changes remain explicit in source.
- [x] The validation baseline advances after each stable independent release.

## Baseline findings

The repository currently fingerprints formatted reflection output with SHA-256. That detects change
but does not show reviewers which API changed. The .NET SDK's package validation tooling can compare
against a released package and record intentional compatibility suppressions as versioned evidence.

## Implementation plan

1. Enable SDK package validation for `Dijgrid.RazorLight` with the historical package name and version
   selected by TASK-008, strict baseline mode, and parameter-name compatibility.
2. Generate and review narrow compatibility suppressions for the inherited end-of-life framework
   groups that the .NET 10 SDK cannot validate.
3. Add Roslyn shipped/unshipped public API tracking so every member change produces a readable diff.
4. Tighten package inspection around exact framework assets, build reference and implementation
   assemblies, symbols, metadata, and Source Link.
5. Document how to advance both baselines after a stable independent release, then run the complete
   warning-as-error, test, pack, and package-inspection gates.

## Implementation notes

- Enabled SDK package validation for `Dijgrid.RazorLight`, using `RazorLight` 2.3.1 as the initial
  baseline with strict comparison and parameter-name checks.
- Generated and reviewed four `PKV006` suppressions for the deliberately removed `netstandard2.0`,
  `netcoreapp3.1`, `net5.0`, and `net6.0` baseline groups. No member-level API break is suppressed.
- Added `Microsoft.CodeAnalysis.PublicApiAnalyzers` and a 658-entry unshipped public API record. The
  API moves to the shipped record when the first independent stable package is released.
- Disabled only the analyzer's RS0026 and RS0027 design rules for inherited optional-parameter
  overloads; TASK-018 owns their redesign, while API and binary compatibility remain enforced.
- Tightened CI and release pack commands to treat warnings as errors. Package inspection now rejects
  unexpected asset groups, verifies generated reference and implementation assemblies, and proves
  the packaged library DLL matches the Release build.
- Documented how to move the public API record and advance the package baseline to the latest stable
  `Dijgrid.RazorLight` release.

## Verification results

- Warning-as-error solution build: passed with zero warnings.
- RazorLight tests: 198 passed; precompile tests: 118 passed.
- Deterministic DLL and PDB verification: passed for the library and precompile tool.
- Warning-as-error pack: passed against the historical package baseline.
- Package layout, metadata, implementation, symbol, and Source Link validation: passed for both
  `Dijgrid.*` packages.
