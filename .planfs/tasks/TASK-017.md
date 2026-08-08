---
id: TASK-017
title: Ratchet coverage and establish performance baselines
status: todo
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
updatedAt: 2026-08-08T16:22:29.279Z
refinementState: ready
---

Turn the initial coverage observation into a regression gate and measure the cost of compilation,
rendering, caching, and common project implementations.

## Acceptance criteria

- [ ] Critical compiler, metadata-reference, model-binding, cache invalidation, include/layout, and
      error paths have focused tests before a repository-wide percentage target is raised.
- [ ] CI enforces a coverage floor no lower than the accepted cross-platform baseline and prevents
      line and branch coverage from silently decreasing.
- [ ] Coverage reports are merged or presented consistently across the xUnit and NUnit suites.
- [ ] A BenchmarkDotNet or equivalent benchmark project measures cold compile, cached render, string,
      file, embedded, include/layout, and concurrent scenarios.
- [ ] Benchmark inputs and environment metadata are versioned and reproducible.
- [ ] Performance budgets are introduced only after stable history exists and distinguish noise from
      material regressions.
- [ ] Benchmark and coverage execution is documented for contributors.
- [ ] Benchmarks include same-key and unrelated-key cold concurrency, cached rendering with and
      without dependency injection, large string templates, deterministic disk caching, repeated
      engine construction/disposal, and layout/include-heavy rendering.
- [ ] Optimization claims made by TASK-034 through TASK-037 are supported by before/after benchmark
      evidence and allocation measurements.

## Baseline findings

The 2026-08-06 baseline records 64.37% line and 51.19% branch coverage for RazorLight, and 46.32%
line and 32.17% branch coverage for the precompile tool. CI collects reports but does not enforce a
minimum. The repository has elapsed-time diagnostics but no repeatable performance benchmark.

The 2026-08-08 repository review also identified unmeasured duplicate source generation during
concurrent misses, repeated DI property reflection, full-template hashing on cached string calls,
whole-file disk-cache hashing, and per-render allocation costs. The hardening tasks must land before
this task establishes the release performance baseline.
