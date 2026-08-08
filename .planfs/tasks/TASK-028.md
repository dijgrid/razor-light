---
id: TASK-028
title: Define the stable engine and template-cache facade
status: review
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
updatedAt: 2026-08-08T06:26:22.429Z
refinementState: ready
---

Replace the leaked handler/options object graph with a small application-facing engine and cache
contract suitable for the 3.x compatibility boundary.

## Acceptance criteria

- [x] `IRazorLightEngine` exposes supported compile/render operations and a narrow template-cache
      administration surface; it does not expose `IEngineHandler` or mutable runtime options.
- [x] Cache inspection/invalidation currently performed through `engine.Handler.Cache` has a direct,
      documented replacement that preserves coordinated invalidation from TASK-014.
- [x] Cache administration is separated from the provider/storage extension contract so consumers
      cannot depend on page-factory and compiler-cache internals merely to invalidate a key.
- [x] Engine configuration is snapshotted at build time and cannot be mutated through
      `engine.Options` after construction.
- [x] The builder returns the supported engine abstraction while concrete handler and engine
      implementations are free to become internal in TASK-029.
- [x] String, file, embedded, layout/include, changed-content, and disabled-cache cases have focused
      facade and invalidation tests.
- [x] README and migration examples cover old handler/cache and mutable-options call sites.

## Usage evidence

Repository documentation currently instructs users to call `engine.Handler.Cache.Remove(key)`.
GitHub code search on 2026-08-08 found 23 indexed matches for the same access pattern across public
repositories. The capability must move to a supported facade rather than disappear.

## Implementation notes

- `IRazorLightEngine` now exposes `IsTemplateCached` and `InvalidateTemplate`; both delegate through
  the coordinated provider without exposing cache records or compiler/page factories.
- `RazorLightEngineBuilder.Build()` returns `IRazorLightEngine` and copies every mutable options
  collection before constructing the engine. The public handler and options properties were removed.
- The facade treats disabled caching as an empty cache with idempotent invalidation. Focused tests
  cover string, project, embedded, file, layout/include, changed-content, and disabled-cache paths.
- README, manual, caching contract, API migration guide, changelog, snippets, and the precompile tool
  now use only the supported engine abstraction.
- Validation passed with 261 core tests, 122 precompile tests, a warning-free Release solution build,
  package validation against the inherited baseline, `git diff --check`, and `planfs validate`.
