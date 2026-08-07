---
id: TASK-024
title: Annotate the public RazorLight API for nullable reference types
status: done
priority: medium
epic: EPIC-modernization
milestone: MILESTONE-library-quality
dependsOn:
  - TASK-009
  - TASK-016
tags:
  - nullable
  - api
  - compatibility
  - dependency-gated
createdAt: 2026-08-07T03:54:00Z
updatedAt: 2026-08-07T14:35:15.726Z
refinementState: ready
---

Enable nullable reference types in the public library and annotate its contract using package and API
compatibility evidence.

## Implementation readiness

Dependency-gated. Complete TASK-009 first to establish repository conventions and TASK-016 so
annotation changes have human-readable API compatibility evidence. No maintainer answer is required
until that evidence exposes an ambiguous public contract.

## Acceptance criteria

- [x] Nullable reference types are enabled in `src/RazorLight` without broad warning suppression.
- [x] Public parameters, return values, properties, delegates, and generic constraints reflect
      documented runtime behavior rather than compiler-warning convenience.
- [x] Optional values and null-state transitions use appropriate attributes where C# annotations
      alone cannot express the contract.
- [x] Existing public API fingerprint tests and package validation identify every externally visible
      annotation change.
- [x] Intentional source-compatibility changes are recorded in shipped/unshipped API evidence and
      next-major migration notes.
- [x] Rendering, compilation, project lookup, caching, layouts, includes, and generated-template
      baselines pass after annotation changes.
- [x] Consumers have a short nullable migration guide with examples for any newly diagnosed call
      sites.

## Implementation plan

1. Inventory public members and group them by compilation, project lookup, caching, rendering, and
   configuration behavior.
2. Enable nullable for the library and annotate one group at a time, adding focused behavioral tests
   where the current null contract is unclear.
3. Review the API/package compatibility diff after each group and record intentional next-major
   changes rather than suppressing them globally.
4. Update XML documentation and migration guidance where annotations reveal previously implicit
   preconditions or optional results.

## Scope boundaries

- Do not redesign APIs solely to make annotations cleaner; TASK-018 owns next-major API redesign.
- Do not change generated template semantics unless a separate compatibility task requires it.
- Nullable warnings must not be hidden with assembly-wide `NoWarn` or `#nullable disable`.

## Implementation notes

- Enabled nullable reference types for `src/RazorLight` and annotated rendering, page lifecycle,
  compilation, project, caching, configuration, and obsolete compatibility contracts. The
  `IsCachingEnabled` contract uses `MemberNotNullWhen` to relate the state flag to `Cache`.
- Regenerated `PublicAPI.Unshipped.txt` in nullable mode so each reference type is recorded with an
  explicit `!` or `?`. The reflection fingerprint now includes nullable metadata, with focused
  contract assertions in `NullableContractTest`.
- Added `docs/nullability.md`, linked it from the README, and recorded the source-compatibility
  impact in the changelog and compatibility baseline.
- Updated repository consumers and test doubles to honor the published contracts. Full library and
  precompile suites, warning-as-error solution build, SDK package compatibility checks, and package
  content validation pass on .NET 10.
