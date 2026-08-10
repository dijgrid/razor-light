---
id: TASK-039
title: Complete stable 3.0 documentation, API, and scaling gate
status: done
priority: high
epic: EPIC-modernization
milestone: MILESTONE-release-readiness
dependsOn:
  - TASK-030
tags:
  - release
  - documentation
  - api
  - performance
  - memory
createdAt: 2026-08-10T17:16:25.526Z
updatedAt: 2026-08-10T17:29:07.000Z
refinementState: ready
---

Establish the final evidence needed to decide whether the independently maintained 3.0 line is
ready for a stable tag. This gate covers user-facing feature documentation, executable examples,
the supported public API contract, and representative large-input and high-cardinality scaling.

## Acceptance criteria

- [x] `docs/manual.md` describes every supported application feature and extension point with
      useful, current examples and links to the deeper compatibility or operational contracts.
- [x] A clearly named executable-documentation test suite demonstrates the documented features and
      fails when an example's behavior changes.
- [x] All supported public API and generated-template ABI entries have useful XML documentation;
      CI prevents undocumented public API from being added accidentally.
- [x] The public API baseline and tiering are reviewed for accidental exposure, mutability,
      nullability, cancellation, ownership, and compatibility before the stable tag.
- [x] Reproducible measurements cover cold compilation/rendering of 5, 10, and 15 MB templates,
      cached rendering of a large template, and compilation/rendering of approximately 1,000
      distinct templates, including elapsed time and managed-memory evidence.
- [x] Material correctness, resource-lifetime, or scaling defects found during evaluation are fixed
      with focused regression coverage; non-blocking findings are documented honestly.
- [x] Maintained tests, release build, formatting/API checks, PlanFS validation, and `git diff
      --check` pass, with any environment-limited validation called out.

## Baseline findings

- The manual already covers the main 3.x workflows but lacks an explicit executable-documentation
  map and several smaller builder/options, diagnostics, writer-output, and error-handling examples.
- XML documentation generation is enabled, but CS1591 is disabled repository-wide and several
  supported types and members have no XML documentation, so completeness is not enforced.
- The versioned benchmark suite includes only a roughly 125 KB source under its `LargeTemplate`
  scenario and does not cover the requested 5-15 MB or 1,000-template scaling ranges.

## Implementation notes

- Expanded the manual with an executable-example entry point, dynamic-template seeding, builder and
  options semantics, caller-owned writer output, operating-assembly selection, diagnostics and
  exception taxonomy, and direct links to the stable scaling evidence.
- Added eight readable `ManualFeatureExamplesTest` scenarios spanning typed/dynamic data, file
  composition, reusable/cached pages, writer output, plain/HTML/raw policies, trusted C# source,
  page initialization, cancellation, and missing-template behavior. Existing specialized suites
  remain the deeper contracts for precompilation, deployment, caching, and language compatibility.
- Enabled CS1591 for package projects and documented every exported entry. The warning-as-error
  build now prevents undocumented API additions. Sealed `RazorLightEngineBuilder`, removed its
  protected mutable construction fields and virtual fluent surface, updated both API baselines, and
  locked the intended built-in/extension boundary in `PublicApiTierTest`.
- Added a fresh-process scaling harness and recorded output-verified results for 5, 10, and 15 MiB
  sources plus 1,000 distinct templates. No correctness or super-linear growth defect appeared.
  The evidence documents the meaningful capacity constraint: a 15 MiB cold compile allocated
  817.3 MiB cumulatively and left about 600.5 MiB working set on the measured host.
- Updated the release-readiness milestone so this task is the explicit final stable-tag gate after
  beta publication preparation.

## Verification

- `dotnet build RazorLight.sln --configuration Release --no-restore --warnaserror` (0 warnings)
- `dotnet test tests/RazorLight.Tests/RazorLight.Tests.csproj --framework net10.0 --configuration
  Release --no-restore` (327 passed)
- `dotnet test tests/RazorLight.Precompile.Tests/RazorLight.Precompile.Tests.csproj --configuration
  Release --no-restore` (78 passed)
- Focused executable documentation/API suite (12 passed)
- Fresh-process scaling harness: 5 MiB 1,154.9 ms/281.8 MiB allocated; 10 MiB 1,724.3 ms/550.9
  MiB; 15 MiB 2,077.0 ms/817.3 MiB; 1,000 templates 7,350.9 ms/1,066.4 MiB. Cached runs were
  4.1-16.5 ms for large templates and 6.1 ms total for 1,000 templates.
- `dotnet format RazorLight.sln whitespace --no-restore --verify-no-changes`
- `dotnet format RazorLight.sln style --no-restore --verify-no-changes --diagnostics IDE0005 IDE0065`
- `planfs validate`
- `git diff --check`
