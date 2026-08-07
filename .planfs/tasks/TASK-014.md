---
id: TASK-014
title: Make template caching and invalidation coherent
status: todo
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
updatedAt: 2026-08-07T00:29:26Z
---

Define one observable cache contract across compiled descriptors, rendered page factories, dynamic
templates, project change tokens, includes, and precompiled templates.

## Acceptance criteria

- [ ] Documentation and tests distinguish the internal compilation cache from the configured
      `ICachingProvider` page-factory cache.
- [ ] Removing or replacing a template invalidates every cache layer that can return its old compiled
      form.
- [ ] Reusing a string-template key with changed content or model/import context cannot silently render
      the previous compiled template.
- [ ] File and custom-project change tokens invalidate layouts and includes as well as direct templates.
- [ ] `PrecompiledCachingProvider.CacheTemplate` and `Remove` have supported behavior instead of
      throwing `NotImplementedException` through a public interface.
- [ ] Key normalization and case sensitivity are intentional and tested on Windows, Linux, and macOS.
- [ ] Concurrent compile, retrieve, replace, remove, and failure paths have deterministic regression
      coverage.
- [ ] Cache exceptions preserve their original cause and do not leave permanently faulted entries.

## Baseline findings

`EngineHandler` consults the public page-factory cache, while `RazorTemplateCompiler` maintains a
separate private cache of compilation tasks. Removing an item from `Handler.Cache` does not invalidate
the compiler cache, and `CompileRenderStringAsync` overwrites dynamic content without invalidating
compiled output for that key. Upstream issues
[`#177`](https://github.com/toddams/RazorLight/issues/177) and
[`#515`](https://github.com/toddams/RazorLight/issues/515) describe the resulting stale-template
behavior.
