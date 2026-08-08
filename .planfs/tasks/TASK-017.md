---
id: TASK-017
title: Ratchet coverage and establish performance baselines
status: done
priority: high
epic: EPIC-modernization
milestone: MILESTONE-library-quality
dependsOn:
  - TASK-005
  - TASK-010
  - TASK-014
  - TASK-022
  - TASK-032
  - TASK-033
  - TASK-034
  - TASK-035
  - TASK-036
  - TASK-037
  - TASK-038
tags:
  - tests
  - coverage
  - performance
  - benchmarks
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-08T18:11:29.470Z
refinementState: ready
---

Turn the initial coverage observation into a regression gate and measure the cost of compilation,
rendering, caching, and common project implementations.

## Acceptance criteria

- [x] Critical compiler, metadata-reference, model-binding, cache invalidation, include/layout, and
      error paths have focused tests before a repository-wide percentage target is raised.
- [x] CI enforces a coverage floor no lower than the accepted cross-platform baseline and prevents
      line and branch coverage from silently decreasing.
- [x] Coverage reports are merged or presented consistently across the xUnit suites.
- [x] A BenchmarkDotNet or equivalent benchmark project measures cold compile, cached render, string,
      file, embedded, include/layout, and concurrent scenarios.
- [x] Benchmark inputs and environment metadata are versioned and reproducible.
- [x] Performance budgets are introduced only after stable history exists and distinguish noise from
      material regressions.
- [x] Benchmark and coverage execution is documented for contributors.
- [x] Benchmarks include same-key and unrelated-key cold concurrency, cached rendering with and
      without dependency injection, large string templates, deterministic disk caching, repeated
      engine construction/disposal, and layout/include-heavy rendering.
- [x] Optimization claims made by TASK-034 through TASK-037 are supported by before/after benchmark
      evidence and allocation measurements.

## Baseline findings

The 2026-08-06 baseline records 64.37% line and 51.19% branch coverage for RazorLight, and 46.32%
line and 32.17% branch coverage for the precompile tool. CI collects reports but does not enforce a
minimum. The repository has elapsed-time diagnostics but no repeatable performance benchmark.

The 2026-08-08 repository review also identified unmeasured duplicate source generation during
concurrent misses, repeated DI property reflection, full-template hashing on cached string calls,
whole-file disk-cache hashing, and per-render allocation costs. The hardening tasks must land before
this task establishes the release performance baseline.

## Implementation notes

- Added one cross-platform coverage command that runs both xUnit suites, selects the intended
  production assembly from each Cobertura report, prints a consistent JSON/table summary, and enforces
  versioned line and branch floors. Current observations are 73.96%/60.00% for RazorLight and
  80.92%/71.15% for the precompile tool; floors retain deliberate cross-platform margin.
- Added a BenchmarkDotNet project with checked-in template inputs, a reproducible ShortRun job,
  environment reporting, and managed-allocation measurements for all requested compilation,
  concurrency, render, DI, composition, disk-cache, and lifecycle scenarios.
- Added a manual benchmark workflow that preserves raw JSON and Markdown results without turning
  noisy shared-runner timing into a release gate. Contributor documentation defines when repeated
  history can justify a material-regression budget.
- Measured the same harness back-to-back against `45b0c73` and `b5f1f12`. Cached DI improved from
  9.556 us/7.19 KB to 1.577 us/2.45 KB, while the stronger dependency-aware disk fingerprint is
  explicitly recorded as a safety tradeoff rather than a performance claim. The complete comparison
  and environment are versioned in `benchmarks/baseline-2026-08-08.md`.

## Verification

- `scripts/Test-Coverage.ps1 -NoBuild` (318 core and 134 precompile tests passed; both floors passed)
- Full 11-scenario BenchmarkDotNet suite completed for the before and after revisions with allocation
  diagnostics and no failed benchmarks.
- `dotnet build RazorLight.sln --configuration Release --no-restore --warnaserror`
- `dotnet format` whitespace and configured import-style verification.
- `planfs validate` and `git diff --check`.
