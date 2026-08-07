---
id: TASK-026
title: Fix deployment CI regressions
status: done
priority: high
createdAt: 2026-08-07T20:43:34.467Z
updatedAt: 2026-08-07T20:47:05.193Z
tags:
  - ci
  - deployment
  - symbols
  - regression
refinementState: ready
---

Repair the two deployment checks that failed in the first CI run containing TASK-012: runtime
symbol emission on a Windows single-file host and expected-failure exit handling on Linux.

## Acceptance criteria

- [x] Runtime-compiled templates emit portable PDBs without using the legacy Windows COM writer.
- [x] A focused unit test locks the runtime symbol format to portable PDB.
- [x] Deployment diagnostics return success after validating expected `IL2026` and `IL3050` failures.
- [x] Framework-dependent, self-contained, and single-file deployment probes pass locally.
- [x] Warning-as-error build, maintained tests, deployment diagnostics, package validation, and
      PlanFS validation pass.

## Baseline findings

CI run [`31210424299`](https://github.com/dijgrid/razor-light/actions/runs/31210424299)
failed on Windows because the hosted runner exposed a full-PDB writer older than Roslyn required.
The Ubuntu diagnostic assertions succeeded but left `$LASTEXITCODE` at `1` from the intentionally
failing consumer builds, so the Actions PowerShell wrapper reported a failed step.

## Implementation notes

- Runtime compilation now always selects `DebugInformationFormat.PortablePdb`; the obsolete native
  Windows PDB capability probe was removed.
- `Test-DeploymentDiagnostics.ps1` clears `$LASTEXITCODE` only after verifying that each expected
  failing build contains the required analyzer ID and message.
- A focused `EmitOptions` regression test was added, and the portable-symbol behavior is recorded in
  the changelog.
- Verified 240 core tests, 120 precompile tests, the warning-as-error solution build, all three local
  Windows deployment modes, expected trimming/AOT diagnostics, deterministic outputs, and both
  package and symbol layouts.
