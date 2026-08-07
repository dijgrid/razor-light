---
id: TASK-010
title: Make LINQ and imports consistent across template sources
status: done
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
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-07T15:13:44.583Z
refinementState: ready
---

Define and implement predictable model typing, default imports, and LINQ behavior for string, file,
embedded-resource, and custom-project templates.

## Implementation readiness

The maintainer approved the recorded recommendations on 2026-08-07 after reviewing TASK-023's
executable baseline.

## Approved maintainer decisions

1. String templates receive the same built-in imports and `AddDefaultNamespaces` values as file,
   embedded-resource, and custom-project templates.
2. Generic rendering preserves the existing dynamic default. Callers can deliberately select a
   visible, closed model type through explicit overloads when the template has no `@model`.
3. Reusing a string-template key with different content, model type, or configured imports replaces
   and invalidates the prior in-process compilation. TASK-014 retains ownership of the complete
   cross-process and custom-provider cache contract.

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

- [x] TASK-023's baseline matrix is converted to intentional expected-behavior regression tests.
- [x] Built-in imports and `AddDefaultNamespaces` have intentional, documented behavior for string
      templates instead of being silently discarded.
- [x] Strongly typed models can use `Any`, `Where`, `Select`, and `FirstOrDefault` with the documented
      import policy.
- [x] Callers can select an explicit model type without relying on an obsolete error-only overload.
- [x] Dynamic receiver limitations produce actionable guidance and are not presented as missing
      `System.Linq` references.
- [x] Compilation cache identity accounts for template content, effective model type, and imports, or
      rejects unsafe key reuse deterministically.
- [x] Layouts, includes, encoding, and existing strongly typed templates retain regression coverage.
- [x] README and compatibility guidance show supported LINQ patterns and dynamic-model limitations.

## Baseline findings

Before this task, `RazorSourceGenerator.GetImportsAsync` returned an empty collection for
`TextSourceRazorProjectItem`, which skipped both the built-in `System.Linq` import and namespaces
added through `AddDefaultNamespaces`. Templates without `@model` still compile with `dynamic`; C#
extension methods cannot be resolved through normal dynamic dispatch even when `System.Linq` is
imported.

The inherited reports in
[`#523`](https://github.com/toddams/RazorLight/issues/523),
[`#520`](https://github.com/toddams/RazorLight/issues/520),
[`#387`](https://github.com/toddams/RazorLight/issues/387), and
[`#257`](https://github.com/toddams/RazorLight/issues/257) describe both failure modes. Treat this as
a compatibility design task, not as a blind addition of `@using System.Linq`.

TASK-023 recorded the executable evidence in
`tests/RazorLight.Tests/Compatibility/TemplateLanguageCompatibilityTest.cs` and the observed matrix
in `docs/template-language-compatibility.md`; those cases were converted into the intentional policy
tests completed here.

## Implementation notes

- `RazorSourceGenerator` now applies built-in and configured namespace imports to string content;
  string content remains independent of location-based project import files.
- Explicit model-type overloads inject a fallback model directive while preserving a template's own
  `@model`. Visible nested, array, and generic type names are formatted for generated C#; anonymous,
  inaccessible, and open generic types are rejected.
- String compilation identities include content, selected model type, and configured namespaces.
  Replacement removes the prior compiler/provider entries and refreshes the logical-key alias used
  by layouts, includes, and direct cache retrieval.
- CS1977 dynamic-lambda failures now explain that `@model` or an explicit model type is required and
  that adding `System.Linq` alone does not change dynamic dispatch.
- The compatibility policy, README, changelog, public API inventory, and reflection fingerprint were
  updated with the intentional behavior and API additions.
