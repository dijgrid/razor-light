---
id: TASK-010
title: Make LINQ and imports consistent across template sources
status: todo
priority: high
epic: EPIC-modernization
milestone: MILESTONE-language-runtime-compatibility
dependsOn:
  - TASK-005
tags:
  - razor
  - linq
  - compiler
  - compatibility
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-07T00:29:26Z
---

Define and implement predictable model typing, default imports, and LINQ behavior for string, file,
embedded-resource, and custom-project templates.

## Acceptance criteria

- [ ] A regression matrix covers strongly typed, anonymous, `ExpandoObject`, and dynamic models
      across string, file, embedded, and custom template sources.
- [ ] Built-in imports and `AddDefaultNamespaces` have intentional, documented behavior for string
      templates instead of being silently discarded.
- [ ] Strongly typed models can use `Any`, `Where`, `Select`, and `FirstOrDefault` with the documented
      import policy.
- [ ] Callers can select an explicit model type without relying on an obsolete error-only overload.
- [ ] Dynamic receiver limitations produce actionable guidance and are not presented as missing
      `System.Linq` references.
- [ ] Compilation cache identity accounts for template content, effective model type, and imports, or
      rejects unsafe key reuse deterministically.
- [ ] Layouts, includes, encoding, and existing strongly typed templates retain regression coverage.
- [ ] README and compatibility guidance show supported LINQ patterns and dynamic-model limitations.

## Baseline findings

`RazorSourceGenerator.GetImportsAsync` currently returns an empty collection for
`TextSourceRazorProjectItem`, which skips both the built-in `System.Linq` import and namespaces added
through `AddDefaultNamespaces`. Templates without `@model` compile with `dynamic`; C# extension
methods cannot be resolved through normal dynamic dispatch even when `System.Linq` is imported.

The inherited reports in
[`#523`](https://github.com/toddams/RazorLight/issues/523),
[`#520`](https://github.com/toddams/RazorLight/issues/520),
[`#387`](https://github.com/toddams/RazorLight/issues/387), and
[`#257`](https://github.com/toddams/RazorLight/issues/257) describe both failure modes. Treat this as
a compatibility design task, not as a blind addition of `@using System.Linq`.
