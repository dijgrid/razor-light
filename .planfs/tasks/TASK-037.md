---
id: TASK-037
title: Simplify runtime construction and hot-path reflection
status: todo
priority: high
epic: EPIC-modernization
dependsOn:
  - TASK-034
  - TASK-035
tags:
  - refactor
  - performance
  - dependency-injection
  - configuration
createdAt: 2026-08-08T16:19:57.795Z
updatedAt: 2026-08-08T16:20:27.633Z
milestone: MILESTONE-library-quality
refinementState: ready
---

Remove duplicated construction/configuration logic and repeated reflection from common render paths.

## Acceptance criteria

- [ ] Fluent and DI engine creation share one internal factory and the same option validation,
      ownership, compiler, cache, and project wiring.
- [ ] Repeated `AddDefaultNamespaces`, `AddMetadataReferences`, `IncludeAssemblies`, and
      `ExcludeAssemblies` calls accumulate values consistently in fluent and DI builders.
- [ ] Builder/option conflicts and null collections fail with targeted configuration diagnostics.
- [ ] Dependency-injection property discovery and setter delegates are cached as one plan per page
      type; rendering performs no repeated property scan.
- [ ] Model-type metadata and compiled page factories are cached where benchmarks show a repeatable
      hot-path benefit without retaining collectible types unexpectedly.
- [ ] The Roslyn option initialization lock is instance-scoped or lock-free so unrelated engines do
      not serialize their first compilation.
- [ ] Focused tests prove fluent/DI parity, additive calls, injection-plan reuse, and concurrent engine
      initialization.

## Baseline findings

The fluent builder and service registration manually construct parallel runtime object graphs.
Several `Add*` methods replace earlier calls. `PropertyInjector` scans runtime properties on every
render despite caching setters, and Roslyn option initialization uses a static lock for instance
state.
