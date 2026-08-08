---
id: TASK-034
title: Fix compilation coordination and cache bookkeeping
status: todo
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
updatedAt: 2026-08-08T16:20:27.633Z
milestone: MILESTONE-library-quality
refinementState: ready
---

Make compilation single-flight per identity without holding global locks during expensive or shared
work, and consolidate cache bookkeeping.

## Acceptance criteria

- [ ] No semaphore or coordinator monitor is held while awaiting compilation or executing user/cache
      callbacks.
- [ ] `TaskCompletionSource` instances used for shared compilation run continuations asynchronously.
- [ ] Concurrent misses for one identity perform project lookup and source generation once, while
      unrelated template identities can progress concurrently.
- [ ] Caller cancellation cancels only that wait once compilation is published and cannot poison the
      shared result.
- [ ] String aliases, model variants, normalized keys, page factories, and compiler descriptors use
      one collision-safe cache-identity model.
- [ ] Legitimate keys containing `.__razorlight.` are not truncated or conflated with internal keys.
- [ ] Expiration and invalidation remove variant tracking; per-key version/tombstone state does not
      grow without bound.
- [ ] Stress tests cover same-key races, unrelated keys, invalidation during compilation, failures,
      cancellation, expiration, and lock-order safety.

## Baseline findings

A racing miss can enter the compiler semaphore, find another caller's published task, and await it
inside the semaphore's `try/finally`. The completion source permits inline continuations, while
coordinator invalidation acquires locks in the opposite direction. Cache identity and string-key
tracking are duplicated across three components and retain per-key state after natural expiration.
