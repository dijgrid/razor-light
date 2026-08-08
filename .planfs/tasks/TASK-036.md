---
id: TASK-036
title: Make precompiled caches deterministic and contract-correct
status: done
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
updatedAt: 2026-08-08T17:13:40.731Z
milestone: MILESTONE-library-quality
refinementState: ready
---

Make file-backed and precompiled cache artifacts deterministic, dependency-aware, and compliant with
the cache-provider contract before building the supported precompiled-only mode.

## Acceptance criteria

- [x] Disk cache identity fingerprints the template, imports, composed C# sources, model contract,
      namespaces/options, compiler/runtime format version, and relevant reference identity.
- [x] Hashing streams inputs with SHA-256 and avoids whole-file concatenation allocations.
- [x] Missing source files can be queried and invalidated without throwing from `Contains`,
      `TryGetTemplate`, or `Remove`.
- [x] `TryGetTemplate` returns `false` with a null factory on misses; strict precompiled-only failure is
      enforced by the execution mode rather than by violating the provider contract.
- [x] Assembly inspection reports corrupt or incompatible artifacts instead of swallowing every
      exception, while unrelated DLLs can still be skipped with useful diagnostics.
- [x] The exposed precompiled map is immutable and duplicate/incompatible keys have deterministic
      diagnostics.
- [x] Tests prove dependency changes invalidate artifacts and unchanged inputs produce stable
      identities and repeatable package outputs.

## Baseline findings

The file-hash strategy reads and concatenates the primary template and key, hashes them with MD5,
and ignores imports, C# sources, options, references, and compiler version. The precompiled provider
throws on a cache miss and silently catches all assembly-inspection failures.

## Implementation notes

- Replaced the MD5/whole-file hash with a length-framed, streamed SHA-256 fingerprint. The supported
  precompile pipeline has a dynamic model contract and fixed compiler options; those markers,
  runtime/compiler versions, references, namespaces, and every Razor/C# source under the project
  root participate in the identity.
- Added deterministic Roslyn assembly names and deterministic compilation so unchanged inputs emit
  byte-identical assemblies and stable cache paths.
- File-backed cache misses and invalidation remain safe after source deletion. Sidecar key metadata
  allows stale hash artifacts to be removed without reconstructing the old fingerprint.
- Precompiled discovery is ordered, its public map and diagnostics are immutable, and corrupt,
  unrelated, duplicate, and incompatible assemblies produce deterministic diagnostics. Normal cache
  lookup returns `false`; the CLI render execution wrapper enforces precompiled-only operation.
- Documented the behavior in `docs/manual.md` and made both CLI paths dispose their engines and
  caching providers.

## Verification

- `dotnet test tests/RazorLight.Tests/RazorLight.Tests.csproj --framework net10.0 --configuration Release --no-restore` (309 passed)
- `dotnet test tests/RazorLight.Precompile.Tests/RazorLight.Precompile.Tests.csproj --configuration Release --no-restore` (133 passed)
- `dotnet build RazorLight.sln --configuration Release --no-restore`
- `git diff --check`
