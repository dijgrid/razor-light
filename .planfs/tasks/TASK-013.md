---
id: TASK-013
title: Add cancellation to asynchronous operations
status: done
priority: medium
epic: EPIC-modernization
milestone: MILESTONE-library-quality
dependsOn:
  - TASK-005
  - TASK-024
tags:
  - async
  - cancellation
  - api
  - reliability
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-08T15:10:20.662Z
refinementState: ready
---

Add cooperative cancellation to template lookup, compilation, rendering, includes, and precompile
operations without breaking existing async callers.

## Acceptance criteria

- [x] Public async APIs have `CancellationToken` overloads that preserve the existing source and
      binary-compatible entry points.
- [x] Cancellation propagates through project lookup, import lookup, compilation locking, rendering,
      includes, and output writing wherever the underlying operation can stop safely.
- [x] Cancelling one waiter does not cancel shared compilation needed by unrelated callers unless the
      ownership model explicitly requires it.
- [x] A cancelled or failed compile does not permanently poison either compilation cache.
- [x] Tests cover cancellation before start, during lookup, while waiting for the compile lock, during
      rendering, and after cache population.
- [x] XML documentation distinguishes cancelling the operation from cancelling only the wait.
- [x] Existing non-token overloads delegate to the new implementation with `CancellationToken.None`.

## Baseline findings

The public API returns `Task` throughout but exposes no cancellation tokens. The compiler semaphore
uses `WaitAsync()` without a token, project APIs cannot observe caller shutdown, and long template
operations therefore outlive request or host lifetimes.

## Refinement decisions

- Preserve every existing public signature. Add token-last overloads and have existing engine,
  built-in project, and page-helper overloads delegate with `CancellationToken.None`.
- Treat cached compilation as shared work. A caller token cancels that caller's wait for the shared
  task; it does not cancel compilation still needed by another caller. Failed shared tasks remain
  evictable under the existing cache policy.
- Add virtual token-aware project overloads without making existing custom project implementations
  recompile. The base implementation cancels the wait around the legacy override; updated projects
  can override the token overload to stop their own I/O.
- Carry the render token on `PageContext` and expose it through `TemplatePageBase.CancellationToken`.
  Includes and layouts reuse that context. Generated template code can pass the property to its own
  cancellable operations.
- Cancellation cannot preempt synchronous Razor code or synchronous `TextWriter` calls. Check the
  token at safe page/layout/include boundaries and use cancellable framework overloads where they
  exist.
- Wire console cancellation into precompile and render commands, returning the conventional exit
  code 130 without treating cancellation as an ordinary template failure.

## Implementation notes

- Added token-last overloads across the engine, handler, compiler, project, rendering, page-helper,
  source-generation, and precompile command surfaces while retaining all existing signatures.
- Added token-aware virtual project methods so existing custom projects remain compatible and newer
  implementations can cancel their underlying I/O.
- Published the render token through `PageContext` and `TemplatePageBase`, and propagated it through
  includes, layouts, section rendering, flushing, source imports, and source-file resolution.
- Kept cached compilation shared: a cancelled caller stops waiting without cancelling a compilation
  already published for other callers. Cancellation before publication leaves the cache retryable.
- Added focused tests for pre-cancellation, project and import lookup, compile-lock contention,
  shared waiters, rendering, includes, populated caches, and precompile command entry points.
- Documented cancellation behavior and limitations in `docs/cancellation.md`, with links from the
  README and manual.

## Verification

- `dotnet build RazorLight.sln --configuration Release`
- `dotnet test tests/RazorLight.Tests/RazorLight.Tests.csproj --framework net10.0 --configuration Release --no-build` (283 passed)
- `dotnet test tests/RazorLight.Precompile.Tests/RazorLight.Precompile.Tests.csproj --configuration Release --no-build` (124 passed)
- `git diff --check`
