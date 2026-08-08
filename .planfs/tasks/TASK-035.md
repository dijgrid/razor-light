---
id: TASK-035
title: Define reusable page and engine resource lifecycles
status: todo
priority: high
epic: EPIC-modernization
dependsOn:
  - TASK-033
  - TASK-034
tags:
  - lifecycle
  - rendering
  - disposal
  - api
createdAt: 2026-08-08T16:19:55.237Z
updatedAt: 2026-08-08T16:20:27.633Z
milestone: MILESTONE-library-quality
refinementState: ready
---

Define safe reuse semantics for compiled templates and deterministic ownership of engine resources.

## Acceptance criteria

- [ ] Rendering a page with a null model cannot retain a model from an earlier render.
- [ ] Sequential page reuse either resets layout/body/section/writer state completely or is rejected
      with a clear single-use contract; concurrent reuse is explicitly handled or rejected.
- [ ] The supported API offers a reusable compiled-template abstraction or factory when callers need
      compile-once/render-many behavior without sharing mutable page state.
- [ ] Engines expose a disposal contract for compiler caches, semaphores, owned caching providers,
      physical file providers/watchers, and other owned resources.
- [ ] Builder-created, caller-supplied, and DI-owned dependencies have explicit non-double-disposal
      ownership rules.
- [ ] DI disposes singleton engines and their owned resources; manually built engines have documented
      `using`/`await using` guidance.
- [ ] Tests cover null-after-non-null models, section/layout reuse, concurrent reuse, repeated engine
      creation/disposal, file watchers, and caller-owned provider/project lifetimes.

## Baseline findings

The generic model path only calls `SetModel` when the new model is non-null, and page instances retain
section and layout state. Engines also own memory caches, a semaphore, and sometimes a
`PhysicalFileProvider`, but `IRazorLightEngine` has no disposal path.
