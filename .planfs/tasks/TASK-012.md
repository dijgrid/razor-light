---
id: TASK-012
title: Define single-file, trimming, and Native AOT compatibility
status: todo
priority: high
epic: EPIC-modernization
milestone: MILESTONE-language-runtime-compatibility
dependsOn:
  - TASK-011
tags:
  - deployment
  - single-file
  - trimming
  - aot
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-07T00:29:26Z
---

Test modern .NET deployment modes and make RazorLight's runtime-code-generation requirements visible
to consumers at build time and runtime.

## Acceptance criteria

- [ ] Integration samples publish and run in framework-dependent, self-contained, and single-file
      modes on at least Windows and Linux.
- [ ] Trim analysis runs against the library and a representative consumer with every warning either
      fixed or documented through a narrowly justified annotation.
- [ ] Runtime-compilation entry points carry appropriate `RequiresDynamicCode` and
      `RequiresUnreferencedCode` annotations when platform analysis requires them.
- [ ] Native AOT support is either proven by an executable integration test or explicitly rejected
      with build-time diagnostics and a documented precompiled alternative.
- [ ] Metadata-reference discovery no longer assumes that every assembly has a usable location or
      adjacent dependency-context file.
- [ ] CI exercises every deployment mode claimed as supported.
- [ ] Framework and README guidance distinguish runtime compilation from precompiled-only execution.

## Baseline findings

RazorLight compiles C# and loads generated assemblies at runtime. Native AOT does not support runtime
code generation or dynamic assembly loading, so claiming AOT compatibility would be incorrect unless
a precompiled-only path avoids those operations. Upstream issues
[`#429`](https://github.com/toddams/RazorLight/issues/429) and
[`#552`](https://github.com/toddams/RazorLight/issues/552) record unresolved single-file and AOT
expectations. Follow the official .NET Native AOT and trimming analyzer guidance rather than
suppressing deployment warnings globally.
