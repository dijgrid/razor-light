# RazorLight 2.3.1 compatibility baseline

This document records the inherited package and behavior baseline before the independent
modernization changes begin. It is evidence for evaluating later changes, not a promise to retain
end-of-life frameworks or vulnerable dependencies.

## Public API

`PublicApiBaselineTest` reflects RazorLight's exported types and their public or protected declared
members into a deterministic API description and compares its SHA-256 fingerprint with the checked-in
baseline. The formatter is deliberately kept in the test so the captured surface is inspectable and
the complete description can be emitted while reviewing a proposed baseline change. A fingerprint
change must be classified as either an intentional breaking change or a regression before it is
accepted.

## Package inspection

The baseline was produced from commit `75b1c346e64d62abbe84e44b150545347a196640` with:

```powershell
dotnet pack src/RazorLight/RazorLight.csproj --configuration Release --output artifacts/baseline
tar -tf artifacts/baseline/RazorLight.2.3.1.nupkg
tar -xOf artifacts/baseline/RazorLight.2.3.1.nupkg RazorLight.nuspec
```

The `RazorLight.2.3.1.nupkg` contains `RazorLight.dll` and XML documentation for
`netstandard2.0`, `netcoreapp3.1`, `net5.0`, and `net6.0`, plus the Apache 2.0 license and NuGet
metadata. It does not contain a package readme or portable PDB. No `.snupkg` was emitted, so the
inherited Source Link configuration does not provide usable symbols or source navigation to package
consumers. The nuspec does record the repository URL and exact commit.

Each app target depends on the matching 3.1, 5.0, or 6.0 Razor, Roslyn, caching, dependency
injection, dependency model, file-provider, and primitives packages, plus `System.Buffers`. The
`netstandard2.0` asset carries the larger ASP.NET Core 2.1 dependency set. All four dependency
groups are tied to end-of-life platform generations; dependency modernization is therefore allowed
to change the package graph intentionally.

## Behavior baseline

The existing test suite provides regression evidence for:

| Scenario | Evidence |
| --- | --- |
| String, file, embedded-resource, and custom project compilation | `RazorLightEngineTest`, project tests, and compiler tests |
| Rendering, HTML encoding, local functions, and model binding | `RazorLightEngineTest` and `TemplateRendererTest` |
| Memory caching and cache lookup behavior | `DefaultCachingProviderTest` and `TemplateCacheLookupResultTest` |
| Includes, nested includes, layouts, sections, and layout model sharing | `RendererCommonCasesTests` and `TemplateRendererTest` |
| Missing templates, invalid Razor, and compilation diagnostics | `RazorTemplateCompilerTest`, `RoslynCompilerServiceTest`, and `TemplateCompilationExceptionTests` |
| Concurrent compilation and rendering | `RaceConditionTests` |

The README's core scenarios—strings, file and embedded projects, custom project implementations,
includes, layouts, encoding, and caching—are covered by executable tests. Its older statements about
AWS Lambda, Azure Functions, ASP.NET Core integration testing, and raw-string caching are inherited
claims without current, automated environment coverage. Treat those claims as unverified until
TASK-007 revalidates or removes them; they are not compatibility guarantees.

## Razor and Roslyn risks

RazorLight invokes Razor language and Roslyn compiler APIs directly rather than only consuming
stable ASP.NET Core hosting abstractions. Updating those components can change generated C#,
whitespace, diagnostic text and locations, metadata-reference discovery, supported Razor syntax,
and assembly-loading behavior. Layout, include, local-function, compilation-error, and concurrency
tests must therefore run whenever Razor or Roslyn moves. A passing compile alone is insufficient.

## Migration policy

- A **regression** is an unplanned public API removal or a behavior change in a covered scenario.
  Regressions block the change or require a fix before merge.
- An **intentional breaking change** must remove an unsafe or unsupported constraint, be recorded in
  the relevant PlanFS task, update the API snapshot and migration notes, and be released with an
  appropriate major-version signal.
- Dropping end-of-life target frameworks and changing dependencies are expected modernization
  changes, but their consumer impact must be documented.
- Unverified inherited claims are not silently promoted to supported scenarios. New support claims
  require repeatable tests on maintained platforms.
