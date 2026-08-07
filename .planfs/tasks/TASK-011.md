---
id: TASK-011
title: Establish a supported current Razor compiler integration
status: todo
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
updatedAt: 2026-08-07T00:29:26Z
---

Replace or isolate the final Razor 6 package compatibility layer with a supported integration that
understands the current Razor and C# language used by .NET 10 consumers.

## Acceptance criteria

- [ ] A technical spike documents the supported integration options for the .NET 10 Razor compiler,
      including licensing, packaging, and internal-API risks.
- [ ] Generated-code and diagnostic baselines are captured before replacing compiler components.
- [ ] Tests cover current syntax, including C# collection expressions, raw strings, pattern matching,
      nullable directives, async code, and representative Razor directives.
- [ ] `Microsoft.AspNetCore.Mvc.Razor.Extensions` and `Microsoft.CodeAnalysis.Razor` 6.0.36 are removed
      from the runtime graph, or a sourced decision explains why no supported replacement can yet be
      shipped and isolates the compatibility layer behind a narrow boundary.
- [ ] Roslyn parse options use an intentional language version rather than whichever value happens to
      be present in a host dependency context.
- [ ] Public API, generated template, layout, include, and error-diagnostic regression suites pass.
- [ ] Dependency and framework-support documentation describes the resulting compiler lifecycle.

## Baseline findings

The public Razor compiler package IDs used by this repository stop at `6.0.36`. The .NET 10 SDK ships
its current compiler as SDK tooling and source-generator assemblies rather than as a drop-in public
runtime package. Upstream issue
[`#555`](https://github.com/toddams/RazorLight/issues/555) demonstrates that newer valid C# syntax is
rejected by the inherited Razor parser. A package-number bump alone cannot complete this task.
