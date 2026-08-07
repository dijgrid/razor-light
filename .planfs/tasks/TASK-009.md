---
id: TASK-009
title: Enable nullable reference types in non-public projects
status: todo
priority: medium
epic: EPIC-modernization
milestone: MILESTONE-library-quality
dependsOn:
  - TASK-006
tags:
  - cleanup
  - nullable
  - ready-for-implementation
createdAt: 2026-08-07T00:35:00Z
updatedAt: 2026-08-07T04:00:47.403Z
refinementState: ready
---

Enable nullable reference types in tests, tools, samples, and the sandbox without mixing those
mechanical fixes with compatibility-sensitive public library annotations.

## Implementation readiness

Ready for implementation. Its only dependency is complete, the affected projects are not public
library contracts, and no maintainer decision is required.

## Acceptance criteria

- [x] Precompile test fixture nullability warnings are resolved and its temporary `NoWarn` is removed.
- [ ] Nullable reference types are enabled in `src/RazorLight.Precompile`.
- [ ] Nullable reference types are enabled in the sandbox and both maintained sample projects.
- [ ] Nullable reference types are enabled in `tests/RazorLight.Tests` without changing test
      behavior.
- [ ] Warnings are fixed with accurate annotations and control flow rather than broad `NoWarn`,
      `#nullable disable`, or unjustified null-forgiving operators.
- [ ] The maintained build, test, sample, and precompile-tool baselines pass with warnings as errors.

## Implementation plan

1. Enable `<Nullable>enable</Nullable>` one project at a time, starting with the precompile tool and
   then moving through the sandbox, samples, and legacy xUnit suite.
2. Build after each project to keep warnings attributable and reviewable.
3. Prefer nullable assertions, guarded access, and intentional optional members; add `!` only where
   the surrounding test or framework lifecycle proves the value is initialized.
4. Run both maintained test projects and the sample checks used by CI.

## Scope boundaries

- Do not enable nullable in `src/RazorLight` under this task.
- Do not change public signatures, runtime semantics, generated Razor code, or template nullability.
- Public library annotations and compatibility evidence are tracked by TASK-024.

## Verification

```shell
dotnet build RazorLight.sln --configuration Release --warnaserror
dotnet test tests/RazorLight.Tests/RazorLight.Tests.csproj --framework net10.0 --configuration Release --no-build
dotnet test tests/RazorLight.Precompile.Tests/RazorLight.Precompile.Tests.csproj --configuration Release --no-build
dotnet run --project samples/RazorLight.Samples/Samples.EntityFrameworkProject.csproj --configuration Release --no-build
dotnet build samples/FunctionApp-WebMvc-Sample/FunctionApp-WebMvc-Sample/FunctionApp-WebMvc-Sample.csproj --configuration Release --warnaserror
```
