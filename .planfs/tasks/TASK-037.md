---
id: TASK-037
title: Simplify runtime construction and hot-path reflection
status: done
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
updatedAt: 2026-08-08T17:20:02.072Z
milestone: MILESTONE-library-quality
refinementState: ready
---

Remove duplicated construction/configuration logic and repeated reflection from common render paths.

## Acceptance criteria

- [x] Fluent and DI engine creation share one internal factory and the same option validation,
      ownership, compiler, cache, and project wiring.
- [x] Repeated `AddDefaultNamespaces`, `AddMetadataReferences`, `IncludeAssemblies`, and
      `ExcludeAssemblies` calls accumulate values consistently in fluent and DI builders.
- [x] Builder/option conflicts and null collections fail with targeted configuration diagnostics.
- [x] Dependency-injection property discovery and setter delegates are cached as one plan per page
      type; rendering performs no repeated property scan.
- [x] Model-type metadata and compiled page factories are cached where benchmarks show a repeatable
      hot-path benefit without retaining collectible types unexpectedly.
- [x] The Roslyn option initialization lock is instance-scoped or lock-free so unrelated engines do
      not serialize their first compilation.
- [x] Focused tests prove fluent/DI parity, additive calls, injection-plan reuse, and concurrent engine
      initialization.

## Baseline findings

The fluent builder and service registration manually construct parallel runtime object graphs.
Several `Add*` methods replace earlier calls. `PropertyInjector` scans runtime properties on every
render despite caching setters, and Roslyn option initialization uses a static lock for instance
state.

## Implementation notes

- Added one internal engine factory for the fluent and dependency-injection paths. Both now use the
  same snapshot validation, metadata-reference manager, source generator, compiler, cache wiring,
  and engine handler while retaining their explicit ownership rules.
- Fluent and DI `Add*` calls union namespaces, metadata references, and included/excluded assemblies;
  null arrays, entries, option collections, and encoders produce targeted diagnostics.
- Property injection caches a complete discovery/setter plan per page type. Model metadata and
  declared template model types use `ConditionalWeakTable`, avoiding repeated reflection without
  pinning collectible types. Compiled page factories continue to live in the bounded template cache.
- Replaced the static Roslyn initialization lock with an instance lock and added a blocking
  concurrency regression test proving independent compilers initialize concurrently.
- Added `RazorLightDependencyBuilder.AddDefaultNamespaces` for fluent/DI configuration parity and
  updated the public API baselines.

## Verification

- `dotnet test tests/RazorLight.Tests/RazorLight.Tests.csproj --framework net10.0 --configuration Release --no-restore` (314 passed)
- `dotnet test tests/RazorLight.Precompile.Tests/RazorLight.Precompile.Tests.csproj --configuration Release --no-restore` (133 passed)
