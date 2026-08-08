---
id: TASK-035
title: Define reusable page and engine resource lifecycles
status: done
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
updatedAt: 2026-08-08T17:03:47.519Z
milestone: MILESTONE-library-quality
refinementState: ready
---

Define safe reuse semantics for compiled templates and deterministic ownership of engine resources.

## Acceptance criteria

- [x] Rendering a page with a null model cannot retain a model from an earlier render.
- [x] Sequential page reuse either resets layout/body/section/writer state completely or is rejected
      with a clear single-use contract; concurrent reuse is explicitly handled or rejected.
- [x] The supported API offers a reusable compiled-template abstraction or factory when callers need
      compile-once/render-many behavior without sharing mutable page state.
- [x] Engines expose a disposal contract for compiler caches, semaphores, owned caching providers,
      physical file providers/watchers, and other owned resources.
- [x] Builder-created, caller-supplied, and DI-owned dependencies have explicit non-double-disposal
      ownership rules.
- [x] DI disposes singleton engines and their owned resources; manually built engines have documented
      `using`/`await using` guidance.
- [x] Tests cover null-after-non-null models, section/layout reuse, concurrent reuse, repeated engine
      creation/disposal, file watchers, and caller-owned provider/project lifetimes.

## Baseline findings

The generic model path only calls `SetModel` when the new model is non-null, and page instances retain
section and layout state. Engines also own memory caches, a semaphore, and sometimes a
`PhysicalFileProvider`, but `IRazorLightEngine` has no disposal path.

## Implementation notes

- Raw `ITemplatePage` instances now have a clear single-use contract enforced across engine
  instances; sequential and concurrent reuse fail before mutable render state can leak.
- Added `RazorLightTemplate` and `CompileReusableTemplateAsync` for compile-once/render-many use.
  Each render obtains a fresh page and is safe to run concurrently.
- Null generic models now explicitly clear the page model rather than retaining a preexisting value.
- Engines implement synchronous and asynchronous disposal. Compiler memory caches, builder-created
  caches, and builder-created file projects/watchers are owned and disposed; caller-supplied project
  and cache instances remain caller-owned. DI disposes the singleton engine and its compiler.
- Documented page reuse, engine disposal, and ownership rules in the manual.

## Verification

- `dotnet test tests/RazorLight.Tests/RazorLight.Tests.csproj --framework net10.0 --configuration Release --no-restore` (309 passed)
- `dotnet test tests/RazorLight.Precompile.Tests/RazorLight.Precompile.Tests.csproj --configuration Release --no-restore` (128 passed)
- `dotnet build RazorLight.sln --configuration Release --no-restore`
- `git diff --check`
