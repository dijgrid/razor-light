---
id: TASK-009
title: Enable nullable reference types in non-public projects
status: done
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
updatedAt: 2026-08-07T06:57:22.548Z
refinementState: ready
---

Enable nullable reference types in tests, tools, samples, and the sandbox without mixing those
mechanical fixes with compatibility-sensitive public library annotations.

## Implementation readiness

Ready for implementation. Its only dependency is complete, the affected projects are not public
library contracts, and no maintainer decision is required.

## Acceptance criteria

- [x] Precompile test fixture nullability warnings are resolved and its temporary `NoWarn` is removed.
- [x] Nullable reference types are enabled in `src/RazorLight.Precompile`.
- [x] Nullable reference types are enabled in the sandbox and both maintained sample projects.
- [x] Nullable reference types are enabled in `tests/RazorLight.Tests` without changing test
      behavior.
- [x] Warnings are fixed with accurate annotations and control flow rather than broad `NoWarn`,
      `#nullable disable`, or unjustified null-forgiving operators.
- [x] The maintained build, test, sample, and precompile-tool baselines pass with warnings as errors.

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

## Implementation notes

- Enabled nullable reference types in the precompile tool, sandbox, Entity Framework sample, and
  legacy xUnit test project. The Function sample and precompile test project were already enabled.
- Replaced nullable tool state with local command state, guarded filesystem and reflection results,
  and annotated optional JSON, logging, model, and test-fixture values accurately.
- Kept `src/RazorLight` unchanged; the only null-forgiving operators are intentional null-contract
  test inputs.
- Verified the warning-as-error solution build, 198 RazorLight tests, 118 precompile tests, the
  running Entity Framework sample, and the warning-as-error Function sample build.
