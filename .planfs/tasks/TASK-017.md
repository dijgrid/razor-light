---
id: TASK-017
title: Ratchet coverage and establish performance baselines
status: todo
priority: medium
epic: EPIC-modernization
milestone: MILESTONE-library-quality
dependsOn:
  - TASK-005
  - TASK-010
  - TASK-014
tags:
  - tests
  - coverage
  - performance
  - benchmarks
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-07T00:29:26Z
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

## Baseline findings

The 2026-08-06 baseline records 64.37% line and 51.19% branch coverage for RazorLight, and 46.32%
line and 32.17% branch coverage for the precompile tool. CI collects reports but does not enforce a
minimum. The repository has elapsed-time diagnostics but no repeatable performance benchmark.
