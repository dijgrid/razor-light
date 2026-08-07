---
id: TASK-023
title: Capture the template import and model compatibility matrix
status: done
priority: high
epic: EPIC-modernization
milestone: MILESTONE-language-runtime-compatibility
dependsOn:
  - TASK-005
tags:
  - razor
  - linq
  - testing
  - compatibility
  - ready-for-implementation
createdAt: 2026-08-07T03:54:00Z
updatedAt: 2026-08-07T04:39:53.693Z
refinementState: ready
---

Turn the inherited LINQ, import, and model-typing reports into an executable baseline before TASK-010
changes production behavior.

## Implementation readiness

Ready for implementation. This is a characterization task: it records current behavior and known
failure modes without selecting the future compatibility policy.

## Acceptance criteria

- [x] A data-driven matrix covers string, file, embedded-resource, and custom/in-memory project
      sources where each source type supports the scenario.
- [x] Model cases include explicit strongly typed `@model`, a generic render call without `@model`,
      an anonymous object, `ExpandoObject`, and a dynamic receiver.
- [x] Import cases distinguish built-in imports, `AddDefaultNamespaces`, explicit `@using`, and no
      import.
- [x] LINQ cases cover `Any`, `Where`, `Select`, and `FirstOrDefault`, including a lambda-based
      extension method that exposes dynamic binding limitations.
- [x] Generated Razor source or compiler diagnostics are captured where they explain a pass or
      failure, without brittle full-file snapshots.
- [x] Reusing a string-template key with changed content, model type, or imports has a focused
      characterization test that records current cache behavior.
- [x] A concise compatibility note summarizes observed behavior and labels known limitations rather
      than presenting expected failures as regressions.
- [x] Production source files and public APIs are unchanged.

## Implementation plan

1. Add a dedicated compatibility test class and small fixtures for file, embedded, and custom
   project items; reuse existing test infrastructure where possible.
2. Represent the dimensions as named test cases so failures identify the source, model, import, and
   LINQ combination.
3. Assert successful output for supported combinations and the specific diagnostic category for
   known failures. Avoid snapshots of absolute paths, assembly versions, or complete generated code.
4. Document the results in `docs/template-language-compatibility.md`, including the difference
   between a missing import and C# dynamic extension-method dispatch.
5. Link the evidence and any newly discovered incompatibility from TASK-010 rather than changing
   behavior in this task.

## Scope boundaries

- This task adds tests, fixtures, and compatibility documentation only.
- Do not add default imports, infer model types, alter cache keys, or change compiler packages.
- If a source type cannot represent a matrix case, document the reason instead of manufacturing a
  misleading equivalent.

## Verification

```shell
dotnet test tests/RazorLight.Tests/RazorLight.Tests.csproj --framework net10.0 --configuration Release
dotnet build RazorLight.sln --configuration Release
```

## Baseline findings

`RazorSourceGenerator.GetImportsAsync` returns no imports for `TextSourceRazorProjectItem`, so string
templates lose both built-in `System.Linq` and namespaces configured through
`AddDefaultNamespaces`. A template without `@model` is generated with a dynamic model, and normal C#
extension-method binding cannot dispatch a lambda against a dynamic receiver. These are separate
failure modes and need separate test expectations.

## Implementation notes

- Added a 15-case characterization suite covering the four source types, import paths, five model
  forms, generated-code evidence, diagnostic categories, and string-key cache reuse.
- Added file and embedded-resource fixtures that exercise `Any`, `Where`, `Select`, and
  `FirstOrDefault`; the custom source uses a non-text in-memory project item so project imports are
  represented accurately.
- Documented the observed matrix and known limitations in
  `docs/template-language-compatibility.md`, and linked the evidence from TASK-010.
- Linked the compatibility matrix from `README.source.md`, cleaned up related usage guidance and
  examples, and regenerated `README.md` with MarkdownSnippets.
- No files under `src` and no public APIs were changed.
