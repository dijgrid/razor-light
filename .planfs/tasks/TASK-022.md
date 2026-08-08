---
id: TASK-022
title: Build a supported precompiled-only execution mode
status: todo
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
updatedAt: 2026-08-08T16:22:29.473Z
refinementState: ready
---

Turn the precompile tool into a supported build-time path that can render known templates without
shipping Roslyn or performing runtime compilation.

## Acceptance criteria

- [ ] A documented MSBuild or CLI workflow discovers and precompiles templates deterministically at
      build or publish time.
- [ ] Runtime loading validates template keys, model contracts, compiler version, and stale inputs with
      actionable diagnostics.
- [ ] Precompiled-only execution does not load Roslyn, inspect SDK files, or compile generated code at
      runtime.
- [ ] File, embedded, layout, include, encoding, and dependency-injection scenarios have end-to-end
      precompiled tests.
- [ ] Symbols and generated-source mappings preserve useful template diagnostics.
- [ ] Single-file and trimming support is proven with publish-and-run tests; Native AOT is claimed only
      if an executable test passes without dynamic-code warnings.
- [ ] The runtime package does not silently fall back to compilation when precompiled-only mode is
      selected.
- [ ] Package boundaries and release artifacts follow TASK-008's identity decision.
- [ ] The precompiled runtime path constructs no Razor language engine, Roslyn compilation service,
      metadata-reference manager, or runtime compiler cache.
- [ ] CLI entry points and file/model reads are asynchronous and propagate cancellation without
      blocking through `GetAwaiter().GetResult()`.

## Baseline findings

The repository contains a precompile CLI and a `PrecompiledCachingProvider`, but rendering a
precompiled page still constructs the full runtime Razor/Roslyn engine graph. The provider's runtime
cache methods now exist, but artifact identity and compatibility validation remain incomplete. A
complete build-time path is the most plausible route to smaller deployments and to any future
AOT-adjacent scenario, but it must not overstate platform support.
