---
id: TASK-028
title: Define the stable engine and template-cache facade
status: todo
priority: high
epic: EPIC-modernization
milestone: MILESTONE-library-quality
dependsOn:
  - TASK-018
  - TASK-014
tags:
  - api
  - caching
  - rendering
  - beta
createdAt: 2026-08-08T04:17:55.367Z
updatedAt: 2026-08-08T04:17:55.367Z
refinementState: ready
---

Replace the leaked handler/options object graph with a small application-facing engine and cache
contract suitable for the 3.x compatibility boundary.

## Acceptance criteria

- [ ] `IRazorLightEngine` exposes supported compile/render operations and a narrow template-cache
      administration surface; it does not expose `IEngineHandler` or mutable runtime options.
- [ ] Cache inspection/invalidation currently performed through `engine.Handler.Cache` has a direct,
      documented replacement that preserves coordinated invalidation from TASK-014.
- [ ] Cache administration is separated from the provider/storage extension contract so consumers
      cannot depend on page-factory and compiler-cache internals merely to invalidate a key.
- [ ] Engine configuration is snapshotted at build time and cannot be mutated through
      `engine.Options` after construction.
- [ ] The builder returns the supported engine abstraction while concrete handler and engine
      implementations are free to become internal in TASK-029.
- [ ] String, file, embedded, layout/include, changed-content, and disabled-cache cases have focused
      facade and invalidation tests.
- [ ] README and migration examples cover old handler/cache and mutable-options call sites.

## Usage evidence

Repository documentation currently instructs users to call `engine.Handler.Cache.Remove(key)`.
GitHub code search on 2026-08-08 found 23 indexed matches for the same access pattern across public
repositories. The capability must move to a supported facade rather than disappear.
