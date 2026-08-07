---
id: TASK-015
title: Define and enforce the template trust boundary
status: done
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
updatedAt: 2026-08-07T23:01:16.092Z
refinementState: ready
---

Make it explicit that Razor templates execute .NET code, then reduce accidental exposure of host
assemblies and services without promising an in-process sandbox that .NET cannot provide.

## Acceptance criteria

- [x] SECURITY.md and consumer documentation state that templates must be treated as trusted code by
      default and explain why HTML encoding is not a code-execution sandbox.
- [x] A threat model covers template sources, metadata references, `@inject`, file access, reflection,
      generated assemblies, diagnostics, and cache poisoning.
- [x] Default metadata-reference discovery is minimized to what compilation requires and has a tested
      allow/deny customization path.
- [x] Untrusted-template scenarios fail closed or require an explicit isolated-process architecture;
      no API claims to safely sandbox arbitrary Razor in process.
- [x] Exceptions and logs do not expose template content, secrets, or private filesystem paths unless
      an explicit diagnostic mode is enabled.
- [x] Security-sensitive defaults and opt-ins have focused regression tests.
- [x] Any remaining risk is documented as an explicit decision rather than an analyzer suppression.

## Baseline findings

The compiler discovers references from the host dependency context and generated templates can call
arbitrary referenced .NET APIs. The current documentation emphasizes HTML encoding but does not
define the executable-code trust boundary. This task must preserve useful trusted-template scenarios
without misrepresenting reference filtering as a secure sandbox.

## Resolved questions

1. Should the formal policy state that in-process templates are trusted code and that untrusted
   templates require process isolation?
   - Decision: Yes. Do not imply that metadata-reference filtering, HTML encoding, or
     dependency-injection restrictions create an in-process security sandbox.
2. How aggressively can automatic assembly-reference discovery be reduced without breaking existing
   trusted-template scenarios?
   - Decision: Establish compatibility tests first, retain references required for the
     framework and declared model contracts, and require explicit configuration for additional host
     assemblies.
3. What public API should consumers use to permit or deny metadata references?
   - Decision: Extend the existing metadata-reference configuration surface where
     practical instead of introducing a separate security abstraction.
4. Which diagnostic information should be redacted by default, and how should trusted development
   environments opt in to fuller diagnostics?
   - Decision: Do not expose template contents or private absolute paths by default.
     Provide an explicit diagnostic opt-in for trusted development environments.
5. Should reference filtering, diagnostic redaction, and the threat-model documentation remain one
   implementation task?
   - Decision: Keep this task as the parent policy and threat-model task, then create
     bounded implementation tasks if reference filtering and diagnostic redaction prove independently
     substantial.

## Implementation notes

- `MetadataReferenceDiscoveryMode.Minimal` is now the default. It includes application project
  assemblies, the operating assembly, RazorLight's runtime dependency closure, and exact assemblies
  selected with `IncludeAssemblies`; unrelated dependency-context packages are excluded.
- `ExcludeAssemblies` now matches exact assembly names, and
  `UseAllDependencyContextMetadataReferences` provides an explicit compatibility opt-in to the broad
  historical behavior. Explicit Roslyn references remain an intentional override.
- Compiler and Razor generation diagnostics retain IDs and positions by default while redacting
  template-derived messages and mapped paths. Missing-template messages and known-key inventories
  follow the same `EnableDebugMode` boundary.
- `docs/template-security.md`, `SECURITY.md`, and README guidance document trusted templates,
  `@inject`, file and reflection access, generated artifacts, caching, diagnostic handling, and the
  external isolation requirements for untrusted authors.
- DECISION-004 records the residual-risk decision that arbitrary Razor cannot be sandboxed safely in
  the application process.
