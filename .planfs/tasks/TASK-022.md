---
id: TASK-022
title: Build a supported precompiled-only execution mode
status: done
priority: high
epic: EPIC-modernization
milestone: MILESTONE-language-runtime-compatibility
dependsOn:
  - TASK-011
  - TASK-012
  - TASK-014
  - TASK-035
  - TASK-036
  - TASK-037
tags:
  - precompile
  - msbuild
  - deployment
  - tooling
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-08T17:57:11.864Z
refinementState: ready
---

Turn the precompile tool into a supported build-time path that can render known templates without
shipping Roslyn or performing runtime compilation.

## Acceptance criteria

- [x] A documented MSBuild or CLI workflow discovers and precompiles templates deterministically at
      build or publish time.
- [x] Runtime loading validates template keys, model contracts, compiler version, and stale inputs with
      actionable diagnostics.
- [x] Precompiled-only execution does not load Roslyn, inspect SDK files, or compile generated code at
      runtime.
- [x] File, embedded, layout, include, encoding, and dependency-injection scenarios have end-to-end
      precompiled tests.
- [x] Symbols and generated-source mappings preserve useful template diagnostics.
- [x] Single-file and trimming support is proven with publish-and-run tests; Native AOT is claimed only
      if an executable test passes without dynamic-code warnings.
- [x] The runtime package does not silently fall back to compilation when precompiled-only mode is
      selected.
- [x] Package boundaries and release artifacts follow TASK-008's identity decision.
- [x] The precompiled runtime path constructs no Razor language engine, Roslyn compilation service,
      metadata-reference manager, or runtime compiler cache.
- [x] CLI entry points and file/model reads are asynchronous and propagate cancellation without
      blocking through `GetAwaiter().GetResult()`.

## Baseline findings

The repository contains a precompile CLI and a `PrecompiledCachingProvider`, but rendering a
precompiled page still constructs the full runtime Razor/Roslyn engine graph. The provider's runtime
cache methods now exist, but artifact identity and compatibility validation remain incomplete. A
complete build-time path is the most plausible route to smaller deployments and to any future
AOT-adjacent scenario, but it must not overstate platform support.

## Implementation notes

- Added a supported precompiled-only engine factory that uses registered page factories and a
  sentinel compiler, so misses and runtime source fail without constructing any compiler graph.
- Added versioned template metadata for key, model contract, compiler identity, and source checksum.
  The trusted-assembly loader rejects legacy, incomplete, duplicate, or incompatible artifacts with
  recompile diagnostics. Clean deterministic `FileHash` artifact sets prevent stale-output mixing.
- Moved the reusable precompiled provider into the runtime package, made CLI dispatch and file reads
  asynchronous with cancellation, and retained PDB loading for generated template source mappings.
- Added a transitive publish target for explicitly precompiled-only consumers. The executable probe
  proves trimmed, self-contained, single-file output without loaded or loose compiler assemblies.
  Native AOT remains explicitly unsupported until it has its own warning-free executable proof.
- Documented the deterministic CLI workflow, clean deployment-unit requirement, package property,
  runtime loading API, diagnostics, and current deployment support boundary.

## Verification

- `dotnet build RazorLight.sln --configuration Release --no-restore --warnaserror` (0 warnings)
- Core test suite: 318 passed; precompile test suite: 134 passed.
- `scripts/Test-DeploymentModes.ps1` published and ran the supported deployment matrix, including the
  trimmed self-contained single-file precompiled-only probe.
- `scripts/Test-DeploymentDiagnostics.ps1` confirmed the runtime-compilation trim/AOT warning inventory.
- `dotnet format` whitespace and configured import-style verification passed.
- Packed all three release artifacts with warnings as errors; `scripts/Validate-Packages.ps1` passed,
  and the core package contains `buildTransitive/Dijgrid.RazorLight.targets`.
