---
id: TASK-034
title: Fix compilation coordination and cache bookkeeping
status: done
priority: high
epic: EPIC-modernization
dependsOn:
  - TASK-013
  - TASK-028
  - TASK-029
tags:
  - concurrency
  - caching
  - performance
  - reliability
createdAt: 2026-08-08T16:19:53.978Z
updatedAt: 2026-08-08T16:53:46.339Z
milestone: MILESTONE-library-quality
refinementState: ready
---

Make compilation single-flight per identity without holding global locks during expensive or shared
work, and consolidate cache bookkeeping.

## Acceptance criteria

- [x] No semaphore or coordinator monitor is held while awaiting compilation or executing user/cache
      callbacks.
- [x] `TaskCompletionSource` instances used for shared compilation run continuations asynchronously.
- [x] Concurrent misses for one identity perform project lookup and source generation once, while
      unrelated template identities can progress concurrently.
- [x] Caller cancellation cancels only that wait once compilation is published and cannot poison the
      shared result.
- [x] String aliases, model variants, normalized keys, page factories, and compiler descriptors use
      one collision-safe cache-identity model.
- [x] Legitimate keys containing `.__razorlight.` are not truncated or conflated with internal keys.
- [x] Expiration and invalidation remove variant tracking; per-key version/tombstone state does not
      grow without bound.
- [x] Stress tests cover same-key races, unrelated keys, invalidation during compilation, failures,
      cancellation, expiration, and lock-order safety.

## Baseline findings

A racing miss can enter the compiler semaphore, find another caller's published task, and await it
inside the semaphore's `try/finally`. The completion source permits inline continuations, while
coordinator invalidation acquires locks in the opposite direction. Cache identity and string-key
tracking are duplicated across three components and retain per-key state after natural expiration.

## Implementation notes

- Replaced the global compiler semaphore and pre-publication source-generation race with a
  per-identity `Lazy<Task<CompiledTemplateDescriptor>>` single-flight registry. No shared
  `TaskCompletionSource` remains, eliminating inline-continuation hazards.
- Added generation identities so invalidation during compilation prevents stale compiler and page
  cache publication without cancelling an existing caller's result.
- Removed separator parsing from the cache coordinator; template identity is now passed explicitly,
  so user keys containing the former internal separator remain exact.
- Moved string alias tracking into the coordinated cache, removed cache callbacks from coordinator
  monitors, and release compiler generations, variants, and coordinator versions after failure or
  expiration.
- Added concurrency, cancellation, failure, invalidation, expiration, independent-key, and
  collision-regression coverage.

## Verification

- `dotnet test tests/RazorLight.Tests/RazorLight.Tests.csproj --framework net10.0 --configuration Release --no-restore` (301 passed)
- `dotnet test tests/RazorLight.Precompile.Tests/RazorLight.Precompile.Tests.csproj --configuration Release --no-restore` (128 passed)
- `dotnet build RazorLight.sln --configuration Release --no-restore`
- `git diff --check`
