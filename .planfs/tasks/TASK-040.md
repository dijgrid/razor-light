---
id: TASK-040
title: Reduce large string-render allocation duplication
status: done
priority: high
epic: EPIC-modernization
milestone: MILESTONE-release-readiness
dependsOn:
  - TASK-039
tags:
  - release
  - performance
  - memory
  - rendering
createdAt: 2026-08-10T18:53:51.858Z
updatedAt: 2026-08-10T18:55:40.000Z
refinementState: ready
---

Remove the duplicate contiguous allocation in string-returning renders before the stable 3.0 API
and performance boundary is frozen.

## Acceptance criteria

- [x] String-returning engine paths materialize one result string rather than retaining a separate
      `StringBuilder`-backed contiguous copy.
- [x] The implementation preserves all `TextWriter` write shapes, output encoding, formatting,
      cancellation, layouts, includes, and resource cleanup.
- [x] Focused tests verify cross-buffer output identity and return rented storage on disposal.
- [x] Cached 5, 10, and 15 MiB renders materially reduce managed allocation without a meaningful
      latency or high-cardinality regression.
- [x] The scaling record states the cold-allocation and retained-pool tradeoff honestly.
- [x] Maintained tests, warning-as-error build, formatting, PlanFS validation, and diff checks pass.

## Baseline findings

`EngineHandler` used `StringWriter`, which accumulates output in a `StringBuilder` and then copies it
again from `ToString()`. Cached 5, 10, and 15 MiB output consequently allocated approximately 20,
40, and 60 MiB respectively even after compilation and other pools were warm.

## Implementation notes

- Added an internal `PooledStringWriter` for the two string-returning render paths. It grows a rented
  character array, returns prior arrays when growth is required, creates the immutable result once,
  clears used characters, and returns the final array on disposal.
- Kept caller-supplied `TextWriter` rendering unchanged. Layout/include buffering and output-encoder
  behavior continue through the existing renderer.
- Added a focused writer test that crosses buffer and write-overload boundaries and verifies all
  rented arrays are returned.
- Updated the manual, changelog, and scaling report with the optimized results and the reusable
  capacity tradeoff.

## Verification

- Cached 5 MiB: 4.6 ms and 10.0 MiB allocated (previously 4.1 ms and 20.1 MiB).
- Cached 10 MiB: 5.3 ms and 20.0 MiB allocated (previously 16.5 ms and 40.1 MiB).
- Cached 15 MiB: 7.1 ms and 30.0 MiB allocated (previously 9.8 ms and 60.2 MiB).
- 1,000 cached templates: 6.1 ms and 2.2 MiB allocated (previously 6.1 ms and 2.5 MiB).
- `dotnet test tests/RazorLight.Tests/RazorLight.Tests.csproj --framework net10.0 --configuration
  Release --no-restore` (328 passed)
- `dotnet test tests/RazorLight.Precompile.Tests/RazorLight.Precompile.Tests.csproj --configuration
  Release --no-restore` (78 passed)
- `dotnet build RazorLight.sln --configuration Release --no-restore --warnaserror` (0 warnings)
- Formatting verification, `planfs validate`, and `git diff --check` passed.
