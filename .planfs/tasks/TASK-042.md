---
id: TASK-042
title: Separate compiler messages from mapped-source diagnostics
status: done
priority: high
createdAt: 2026-08-20T21:14:51.946Z
updatedAt: 2026-08-20T21:22:58.608Z
---

Compiler diagnostics currently treat Roslyn's message text and mapped template paths as one debug-only
unit. Keep mapped paths and generated-source details behind debug mode, but return actionable compiler
messages by default and give sensitive hosts an explicit message-redaction option.

## Acceptance criteria

- [x] Non-debug compilation failures include the Roslyn diagnostic message while keeping mapped paths redacted.
- [x] Hosts can explicitly redact compiler message text without enabling or disabling other debug diagnostics.
- [x] Warning-as-error diagnostics follow the same message policy.
- [x] Focused tests, public API baselines, security guidance, and release notes describe the behavior.
- [x] The maintained test suite and release build pass.

## Implementation notes

- Added `RazorLightOptions.RedactCompilerDiagnosticMessages`, defaulting to actionable compiler text.
- Kept mapped paths controlled by `EnableDebugMode` and verified both policies through the complete engine.
- Validated the change with SAFEGen's historical 5k dataset: the former generic `CS0104` now names both
  ambiguous `IRazorModelAccessor` types without enabling debug mode.
- Prepared the backward-compatible `3.0.1` patch release and fixed the Windows package-consumer smoke
  test's NuGet source URI so the documented local release validation remains cross-platform.
- Passed 332 runtime tests, 134 precompile tests, the release solution build, API baseline, diff checks,
  deterministic-build validation, package validation, and PlanFS validation.
