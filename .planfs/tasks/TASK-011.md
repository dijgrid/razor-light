---
id: TASK-011
title: Establish a supported current Razor compiler integration
status: done
priority: high
epic: EPIC-modernization
milestone: MILESTONE-language-runtime-compatibility
dependsOn:
  - TASK-002
  - TASK-004
  - TASK-010
tags:
  - razor
  - compiler
  - roslyn
  - compatibility
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-07T15:50:09.664Z
refinementState: ready
---

Replace or isolate the final Razor 6 package compatibility layer with a supported integration that
understands the current Razor and C# language used by .NET 10 consumers.

## Acceptance criteria

- [x] A technical spike documents the supported integration options for the .NET 10 Razor compiler,
      including licensing, packaging, and internal-API risks.
- [x] Generated-code and diagnostic baselines are captured before replacing compiler components.
- [x] Tests cover current syntax, including C# collection expressions, raw strings, pattern matching,
      nullable directives, async code, and representative Razor directives.
- [x] `Microsoft.AspNetCore.Mvc.Razor.Extensions` and `Microsoft.CodeAnalysis.Razor` 6.0.36 are removed
      from the runtime graph, or a sourced decision explains why no supported replacement can yet be
      shipped and isolates the compatibility layer behind a narrow boundary.
- [x] Roslyn parse options use an intentional language version rather than whichever value happens to
      be present in a host dependency context.
- [x] Public API, generated template, layout, include, and error-diagnostic regression suites pass.
- [x] Dependency and framework-support documentation describes the resulting compiler lifecycle.

## Baseline findings

The public Razor compiler package IDs used by this repository stop at `6.0.36`. The .NET 10 SDK ships
its current compiler as SDK tooling and source-generator assemblies rather than as a drop-in public
runtime package. Upstream issue
[`#555`](https://github.com/toddams/RazorLight/issues/555) demonstrates that newer valid C# syntax is
rejected by the inherited Razor parser. A package-number bump alone cannot complete this task.

## Implementation notes

- `docs/razor-compiler-integration.md` records the supported SDK, obsolete MVC runtime compiler,
  SDK-private assembly, vendoring, and retained-adapter options with Microsoft and NuGet sources.
- `Razor6CompilerCompatibility` is the sole default-engine construction boundary and refuses an
  unexpected Razor compiler major version. The inherited public API still accepts Razor compiler
  types, so removing those packages requires a separately reviewed major API migration.
- `RoslynCompilationService` selects C# 14 explicitly for the maintained .NET 10 target and ignores
  the host dependency context's language-version value while retaining its preprocessor symbols.
- `CurrentCompilerCompatibilityTest` captures generated-code fragments, the `RZ1013` malformed-model
  diagnostic, collection expressions, raw strings, property patterns, nullable directives, async
  code, and representative Razor directives.
- Validation completed with a warning-free Release solution build, 218 `RazorLight.Tests` tests,
  118 `RazorLight.Precompile.Tests` tests, and `git diff --check`.
