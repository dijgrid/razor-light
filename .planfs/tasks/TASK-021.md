---
id: TASK-021
title: Align dependency injection and ViewBag behavior
status: todo
priority: medium
epic: EPIC-modernization
milestone: MILESTONE-language-runtime-compatibility
dependsOn:
  - TASK-010
  - TASK-011
tags:
  - razor
  - dependency-injection
  - viewbag
  - compatibility
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-07T04:01:10.142Z
refinementState: needs-refinement
---

Make `@inject`, pre-render callbacks, and ViewBag semantics predictable across every supported project
and rendering entry point.

## Acceptance criteria

- [ ] `@inject` behavior is covered for string, file, embedded, custom, builder-created, and
      dependency-injection-created engines.
- [ ] Service-provider ownership and scope behavior are explicit; RazorLight does not capture a scoped
      service in a singleton unintentionally.
- [ ] Missing ViewBag member behavior is compared with ASP.NET Core Razor and either aligned or
      documented with a safe migration path.
- [ ] Null-conditional, indexer, method, and nested dynamic access have focused regression tests.
- [ ] Pre-render callbacks and property injection run exactly once per page, including layouts and
      includes.
- [ ] Tag-helper activation either uses the configured service provider correctly or is explicitly
      removed from the supported surface in TASK-018.
- [ ] Documentation distinguishes RazorLight's standalone runtime services from MVC services that are
      not available.

## Baseline findings

The tag-helper activator and template base still contain incomplete service-resolution paths, and
`@inject` often requires a manually registered pre-render callback. ViewBag is an `ExpandoObject`, so
missing members throw instead of returning null as MVC's ViewData-backed wrapper does. Upstream issues
[`#211`](https://github.com/toddams/RazorLight/issues/211) and
[`#354`](https://github.com/toddams/RazorLight/issues/354) capture the compatibility gap.
