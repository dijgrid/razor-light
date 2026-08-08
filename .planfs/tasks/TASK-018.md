---
id: TASK-018
title: Design the next-major public API cleanup
status: review
priority: high
epic: EPIC-modernization
milestone: MILESTONE-library-quality
dependsOn:
  - TASK-016
  - TASK-024
tags:
  - api
  - cleanup
  - compatibility
  - documentation
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-08T04:24:07.754Z
refinementState: ready
---

Inventory inherited public contracts and design a coherent next-major surface after nullable and
package compatibility validation are available.

## Acceptance criteria

- [x] Public obsolete members, mutable option collections, implementation types, naming issues,
      sync-over-async risks, and error-only stubs are inventoried with usage evidence.
- [x] Public methods marked obsolete with `error: true` no longer remain as undocumented
      `NotImplementedException` traps in the next-major contract.
- [x] Interfaces expose only behavior that every supported implementation can fulfill.
- [x] Builder, dependency-injection, project, cache, compiler, and rendering abstractions have clear
      ownership and lifetime documentation.
- [x] Proposed removals and replacements include source and binary migration examples.
- [x] Package validation and nullable API baselines enumerate every intentional break.
- [x] The cleanup is split into reviewable implementation tasks instead of one repository-wide rewrite.

## Baseline findings

The assembly still exposes inherited members that are both obsolete with `error: true` and implemented
only by throwing `NotImplementedException`. Several public interfaces also force incomplete providers
to throw for operations they cannot support. The independent next major is the appropriate boundary
for deliberate cleanup, but only after compatibility tooling makes the impact visible.

## Inventory findings

The post-TASK-019 Roslyn record contains 657 public API entries. The main problems are structural,
not merely naming:

- `EngineFactory`/`IEngineFactory` are compile-time-error obsolete, return nullable engines, and end
  in a `Create` implementation that always returns null. Three obsolete engine overloads likewise
  throw `NotImplementedException`.
- `IRazorLightEngine.Options` exposes replaceable mutable sets, dictionaries, callbacks, cache,
  assembly, encoder, and debug state after construction. `Handler` exposes compiler and page-factory
  internals so applications can reach cache invalidation.
- `ICachingProvider` mixes application cache administration with storage of executable page
  factories. The public handler/compiler/factory graph forces consumers to understand internal cache
  coordination introduced by TASK-014.
- The public record contains 108 entries under `RazorLight.Internal` and 148 more under compilation,
  generation, or instrumentation namespaces. Generated templates need only a much smaller page ABI.
- Synchronous section rendering and helper-result writing block on tasks. These are Razor generated-
  code compatibility boundaries, not patterns to extend into new application APIs.
- `AddPrerenderCallbacks` has inconsistent naming and is also the mutation hook used to install DI
  property injection. `AddRazorLight(Func<IRazorLightEngine>)` produces a singleton engine connected
  to a root provider rather than a defined per-render scope.

Repository searches confirm that normal samples use builders and engine rendering. Public indexed
GitHub searches on 2026-08-08 found 23 `.Handler.Cache` matches, 40 `ICachingProvider` matches, 147
`RazorLightProject` matches, and 102 `RazorLightProjectItem` matches. Compiler-interface results were
mostly RazorLight forks or vendored source. Cache administration and custom project sources therefore
need supported replacements; compiler/buffer implementation access does not become a commitment by
default.

## Selected design

DECISION-005 and `docs/api-design-3.0.md` define four API layers: normal application API, supported
extension points, generated-template ABI, and implementation details. The 3.x engine facade will not
expose mutable options or the handler graph. It will expose narrow cache administration, while cache
storage remains a separate provider extension. Custom projects, project items, encoders, cache
providers, and page initialization remain supported with end-to-end contract tests.

Generated page types remain public only where emitted assemblies require them. Handler, compiler
wiring, Razor passes, activation implementations, cache records, and buffers become internal unless a
characterized extension proves otherwise. The obsolete/error-only APIs are excluded from the 3.x
contract and scheduled for physical removal in TASK-027; the checked criterion above describes the
accepted next-major contract, not a compatibility shim retained in current source.

Configuration is frozen at engine creation. Under DI, compilation and caches are singleton, while
each top-level render owns one service scope shared with layouts and includes. Missing ViewBag member
reads return null; unrelated dynamic errors remain visible. TASK-021 now records this implementation
policy and no longer contains stale tag-helper questions.

## Implementation sequence

- TASK-027 removes inherited obsolete, error-only, retired-platform, and redundant factory APIs.
- TASK-028 replaces `Handler`/`Options` exposure with the stable engine and cache facade.
- TASK-021 implements immutable configuration, render scopes, page initialization, and ViewBag
  semantics.
- TASK-029 internalizes implementation types and establishes the generated-template ABI baseline.
- TASK-030 validates and publishes `3.0.0-beta.1` as a prerelease.

TASK-013 cancellation, TASK-017 performance/coverage baselines, and TASK-022 precompiled-only mode can
continue during the beta series. They do not block beta.1 unless their refinement identifies another
required 3.0 API break.
