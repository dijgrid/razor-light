---
id: TASK-015
title: Define and enforce the template trust boundary
status: todo
priority: high
epic: EPIC-modernization
milestone: MILESTONE-library-quality
dependsOn:
  - TASK-004
  - TASK-011
tags:
  - security
  - templates
  - metadata
  - documentation
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-07T00:29:26Z
---

Make it explicit that Razor templates execute .NET code, then reduce accidental exposure of host
assemblies and services without promising an in-process sandbox that .NET cannot provide.

## Acceptance criteria

- [ ] SECURITY.md and consumer documentation state that templates must be treated as trusted code by
      default and explain why HTML encoding is not a code-execution sandbox.
- [ ] A threat model covers template sources, metadata references, `@inject`, file access, reflection,
      generated assemblies, diagnostics, and cache poisoning.
- [ ] Default metadata-reference discovery is minimized to what compilation requires and has a tested
      allow/deny customization path.
- [ ] Untrusted-template scenarios fail closed or require an explicit isolated-process architecture;
      no API claims to safely sandbox arbitrary Razor in process.
- [ ] Exceptions and logs do not expose template content, secrets, or private filesystem paths unless
      an explicit diagnostic mode is enabled.
- [ ] Security-sensitive defaults and opt-ins have focused regression tests.
- [ ] Any remaining risk is documented as an explicit decision rather than an analyzer suppression.

## Baseline findings

The compiler discovers references from the host dependency context and generated templates can call
arbitrary referenced .NET APIs. The current documentation emphasizes HTML encoding but does not
define the executable-code trust boundary. This task must preserve useful trusted-template scenarios
without misrepresenting reference filtering as a secure sandbox.
