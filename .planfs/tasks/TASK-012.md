---
id: TASK-012
title: Define single-file, trimming, and Native AOT compatibility
status: done
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
updatedAt: 2026-08-07T16:33:34.277Z
refinementState: ready
---

Test modern .NET deployment modes and make RazorLight's runtime-code-generation requirements visible
to consumers at build time and runtime.

## Acceptance criteria

- [x] Integration samples publish and run in framework-dependent, self-contained, and single-file
      modes on at least Windows and Linux.
- [x] Trim analysis runs against the library and a representative consumer with every warning either
      fixed or documented through a narrowly justified annotation.
- [x] Runtime-compilation entry points carry appropriate `RequiresDynamicCode` and
      `RequiresUnreferencedCode` annotations when platform analysis requires them.
- [x] Native AOT support is either proven by an executable integration test or explicitly rejected
      with build-time diagnostics and a documented precompiled alternative.
- [x] Metadata-reference discovery no longer assumes that every assembly has a usable location or
      adjacent dependency-context file.
- [x] CI exercises every deployment mode claimed as supported.
- [x] Framework and README guidance distinguish runtime compilation from precompiled-only execution.

## Baseline findings

RazorLight compiles C# and loads generated assemblies at runtime. Native AOT does not support runtime
code generation or dynamic assembly loading, so claiming AOT compatibility would be incorrect unless
a precompiled-only path avoids those operations. Upstream issues
[`#429`](https://github.com/toddams/RazorLight/issues/429) and
[`#552`](https://github.com/toddams/RazorLight/issues/552) record unresolved single-file and AOT
expectations. Follow the official .NET Native AOT and trimming analyzer guidance rather than
suppressing deployment warnings globally.

## Implementation notes

- Added a representative console probe and PowerShell harness that publish and execute
  framework-dependent, self-contained, and extraction-based single-file applications. CI runs the
  matrix on Windows and Linux.
- Marked runtime-compilation entry points with `RequiresUnreferencedCode` and
  `RequiresDynamicCode`. Consumer analyzer probes require `IL2026` for trimming and `IL3050` for
  Native AOT, and runtime compilation fails early when dynamic code is unavailable.
- Recorded and CI-guarded the library's internal trim-warning categories without applying global
  suppressions. `docs/deployment.md` explains why each remaining category belongs to a currently
  unsupported dynamic path.
- Hardened fallback reference discovery to ignore dynamic assemblies, empty locations, and missing
  files while honoring explicitly supplied metadata references and returning actionable guidance
  when none are usable.
- Documented that non-extracting single-file, trimmed, and Native AOT runtime compilation are not
  supported. The existing precompile tool still loads assemblies dynamically; `TASK-022` remains
  the work item for a true precompiled-only execution path.
