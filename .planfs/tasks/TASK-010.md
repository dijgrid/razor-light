---
id: TASK-010
title: Make LINQ and imports consistent across template sources
status: todo
priority: high
epic: EPIC-modernization
milestone: MILESTONE-language-runtime-compatibility
dependsOn:
  - TASK-005
  - TASK-023
tags:
  - razor
  - linq
  - compiler
  - compatibility
  - needs-maintainer-decision
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-07T04:01:02.469Z
refinementState: needs-refinement
---

Define and implement predictable model typing, default imports, and LINQ behavior for string, file,
embedded-resource, and custom-project templates.

## Implementation readiness

Needs the evidence from TASK-023 and the maintainer decisions below. Do not change import, model, or
cache behavior until those choices are recorded because each can alter how an existing template is
compiled.

## Maintainer decisions required

Review these after TASK-023 captures the executable baseline:

1. Should string templates receive the same built-in imports and `AddDefaultNamespaces` values as
   file, embedded-resource, and custom-project templates? **Recommendation:** yes for the next major
   line; source type should not silently disable configured imports.
2. When a generic render API receives `TModel` but the template has no `@model`, should RazorLight
   infer `TModel` automatically? **Recommendation:** preserve the existing dynamic default and add a
   deliberate typed-model API or option. Silent inference changes generated code, does not work
   uniformly for anonymous or inaccessible runtime types, and affects cache identity.
3. When a caller reuses a string-template key with different content, model type, or effective
   imports, should RazorLight replace the cached compilation or reject the reuse? **Recommendation:**
   replace and invalidate all affected cache layers; reserve rejection for combinations that cannot
   be represented safely. TASK-014 implements the full cache contract.

## Implementation plan

1. Convert TASK-023's baseline cases into expected-behavior tests using the recorded decisions.
2. Normalize effective imports so built-in and configured namespaces follow the same documented
   policy for every project item type.
3. Add an explicit typed-model path without changing the meaning of anonymous and dynamic models.
4. Include effective model/import context in the compilation request and coordinate key reuse with
   TASK-014's invalidation contract.
5. Add focused diagnostics or documentation for dynamic extension-method limitations.
6. Update `README.source.md`, regenerate `README.md`, and add migration guidance for intentional
   behavior changes.

## Scope boundaries

- Do not replace the Razor compiler packages here; that work belongs to TASK-011.
- Do not redesign the complete caching provider contract here; that work belongs to TASK-014.
- Preserve layouts, includes, encoding, and explicit `@model` behavior unless a failing regression
  demonstrates that a coordinated change is required.

## Acceptance criteria

- [ ] TASK-023's baseline matrix is converted to intentional expected-behavior regression tests.
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

TASK-023 records the executable evidence in
`tests/RazorLight.Tests/Compatibility/TemplateLanguageCompatibilityTest.cs` and the observed matrix
in `docs/template-language-compatibility.md`. Use those cases when converting the baseline into an
intentional policy.
