---
id: TASK-021
title: Align dependency injection and ViewBag behavior
status: todo
priority: high
epic: EPIC-modernization
milestone: MILESTONE-language-runtime-compatibility
dependsOn:
  - TASK-010
  - TASK-011
  - TASK-018
tags:
  - razor
  - dependency-injection
  - viewbag
  - compatibility
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-08T04:19:48.485Z
refinementState: ready
---

Make `@inject`, page initialization, service scopes, and ViewBag semantics predictable across every
supported project and rendering entry point before the 3.0 beta.

## Acceptance criteria

- [ ] `@inject` behavior is covered for string, file, embedded, custom, builder-created, and
      dependency-injection-created engines.
- [ ] Service-provider ownership and scope behavior are explicit; RazorLight does not capture a scoped
      service in a singleton unintentionally.
- [ ] A top-level render creates one service scope; its page, layout, sections, and includes share that
      scope, and RazorLight disposes it after rendering.
- [ ] Builder-created engines do not imply service injection; DI-created engines resolve `@inject`
      properties from the render scope without exposing internal handler/compiler services.
- [ ] Missing ViewBag members return null to match normal Razor expectations while invalid method,
      conversion, and index operations retain actionable dynamic-binding errors.
- [ ] Null-conditional, indexer, method, and nested dynamic access have focused regression tests.
- [ ] Replace the inconsistently cased `AddPrerenderCallbacks` surface with a documented page
      initializer contract that runs exactly once per page, including layouts and includes.
- [x] Tag-helper activation is absent; TASK-019 removed it from the generic core.
- [ ] Mutable options are consumed during registration/build and snapshotted before singleton runtime
      services are created.
- [ ] Documentation distinguishes RazorLight's standalone runtime services from MVC services that are
      not available.

## Baseline findings

The post-TASK-019 core has no tag-helper activation, but `@inject` still depends on mutation through
`engine.Options.PreRenderCallbacks`. `AddRazorLight(Func<IRazorLightEngine>)` captures a singleton
engine and root provider, while direct resolution of `IEngineHandler` is deliberately registered as
an exception. ViewBag is an `ExpandoObject`, so missing members throw instead of returning null as
MVC's ViewData-backed wrapper does. Upstream issues
[`#211`](https://github.com/toddams/RazorLight/issues/211) and
[`#354`](https://github.com/toddams/RazorLight/issues/354) capture the compatibility gap.

## Selected policy

Keep compilation and caches singleton, create one dependency-injection scope per top-level render,
and share it through the complete layout/include graph. Treat page initialization as a supported
render-lifecycle extension rather than exposing mutable options. Align missing-member ViewBag reads
with Razor's null-returning behavior, without hiding unrelated dynamic programming errors.
