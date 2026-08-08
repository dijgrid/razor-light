---
id: TASK-036
title: Make precompiled caches deterministic and contract-correct
status: todo
priority: high
epic: EPIC-modernization
dependsOn:
  - TASK-031
  - TASK-032
  - TASK-034
tags:
  - precompile
  - caching
  - determinism
  - tooling
createdAt: 2026-08-08T16:19:56.545Z
updatedAt: 2026-08-08T16:20:27.633Z
milestone: MILESTONE-library-quality
refinementState: ready
---

Make file-backed and precompiled cache artifacts deterministic, dependency-aware, and compliant with
the cache-provider contract before building the supported precompiled-only mode.

## Acceptance criteria

- [ ] Disk cache identity fingerprints the template, imports, composed C# sources, model contract,
      namespaces/options, compiler/runtime format version, and relevant reference identity.
- [ ] Hashing streams inputs with SHA-256 and avoids whole-file concatenation allocations.
- [ ] Missing source files can be queried and invalidated without throwing from `Contains`,
      `TryGetTemplate`, or `Remove`.
- [ ] `TryGetTemplate` returns `false` with a null factory on misses; strict precompiled-only failure is
      enforced by the execution mode rather than by violating the provider contract.
- [ ] Assembly inspection reports corrupt or incompatible artifacts instead of swallowing every
      exception, while unrelated DLLs can still be skipped with useful diagnostics.
- [ ] The exposed precompiled map is immutable and duplicate/incompatible keys have deterministic
      diagnostics.
- [ ] Tests prove dependency changes invalidate artifacts and unchanged inputs produce stable
      identities and repeatable package outputs.

## Baseline findings

The file-hash strategy reads and concatenates the primary template and key, hashes them with MD5,
and ignores imports, C# sources, options, references, and compiler version. The precompiled provider
throws on a cache miss and silently catches all assembly-inspection failures.
