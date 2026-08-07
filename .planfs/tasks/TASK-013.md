---
id: TASK-013
title: Add cancellation to asynchronous operations
status: todo
priority: medium
epic: EPIC-modernization
milestone: MILESTONE-library-quality
dependsOn:
  - TASK-005
  - TASK-009
tags:
  - async
  - cancellation
  - api
  - reliability
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-07T00:29:26Z
---

Add cooperative cancellation to template lookup, compilation, rendering, includes, and precompile
operations without breaking existing async callers.

## Acceptance criteria

- [ ] Public async APIs have `CancellationToken` overloads that preserve the existing source and
      binary-compatible entry points.
- [ ] Cancellation propagates through project lookup, import lookup, compilation locking, rendering,
      includes, and output writing wherever the underlying operation can stop safely.
- [ ] Cancelling one waiter does not cancel shared compilation needed by unrelated callers unless the
      ownership model explicitly requires it.
- [ ] A cancelled or failed compile does not permanently poison either compilation cache.
- [ ] Tests cover cancellation before start, during lookup, while waiting for the compile lock, during
      rendering, and after cache population.
- [ ] XML documentation distinguishes cancelling the operation from cancelling only the wait.
- [ ] Existing non-token overloads delegate to the new implementation with `CancellationToken.None`.

## Baseline findings

The public API returns `Task` throughout but exposes no cancellation tokens. The compiler semaphore
uses `WaitAsync()` without a token, project APIs cannot observe caller shutdown, and long template
operations therefore outlive request or host lifetimes.
