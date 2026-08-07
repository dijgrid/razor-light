---
id: TASK-014
title: Make template caching and invalidation coherent
status: done
priority: high
epic: EPIC-modernization
milestone: MILESTONE-language-runtime-compatibility
dependsOn:
  - TASK-005
  - TASK-010
tags:
  - caching
  - concurrency
  - precompile
  - correctness
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-07T18:29:33.876Z
refinementState: ready
---

Define one observable cache contract across compiled descriptors, rendered page factories, dynamic
templates, project change tokens, includes, and precompiled templates.

## Acceptance criteria

- [x] Documentation and tests distinguish the internal compilation cache from the configured
      `ICachingProvider` page-factory cache.
- [x] Removing or replacing a template invalidates every cache layer that can return its old compiled
      form.
- [x] Reusing a string-template key with changed content or model/import context cannot silently render
      the previous compiled template.
- [x] File and custom-project change tokens invalidate layouts and includes as well as direct templates.
- [x] `PrecompiledCachingProvider.CacheTemplate` and `Remove` have supported behavior instead of
      throwing `NotImplementedException` through a public interface.
- [x] Key normalization and case sensitivity are intentional and tested on Windows, Linux, and macOS.
- [x] Concurrent compile, retrieve, replace, remove, and failure paths have deterministic regression
      coverage.
- [x] Cache exceptions preserve their original cause and do not leave permanently faulted entries.

## Baseline findings

`EngineHandler` consults the public page-factory cache, while `RazorTemplateCompiler` maintains a
separate private cache of compilation tasks. Removing an item from `Handler.Cache` does not invalidate
the compiler cache, and `CompileRenderStringAsync` overwrites dynamic content without invalidating
compiled output for that key. Upstream issues
[`#177`](https://github.com/toddams/RazorLight/issues/177) and
[`#515`](https://github.com/toddams/RazorLight/issues/515) describe the resulting stale-template
behavior.

## Implementation notes

- Added an internal coordinated provider around the configured `ICachingProvider`. Logical-key
  removal and replacement now clear compiler descriptors and every registered page-factory variant;
  a version check prevents an in-flight compilation from restoring a stale page factory.
- Failed compiler tasks are evicted before their original exception is propagated, allowing a
  corrected template to retry the same key. File and custom-project change-token tests cover direct
  templates, layouts, and includes.
- `PrecompiledCachingProvider` now supports runtime entries and idempotent removal. Cache paths
  normalize slash direction and use ordinal, case-sensitive comparison consistently across hosts.
- Added [`docs/caching.md`](../../docs/caching.md), synchronized README guidance, and regression tests
  for string identity, concurrent access, failure recovery, key semantics, and both caching layers.
