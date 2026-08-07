---
id: TASK-016
title: Add package and API compatibility validation
status: todo
priority: high
epic: EPIC-modernization
milestone: MILESTONE-release-readiness
dependsOn:
  - TASK-008
tags:
  - api
  - packaging
  - compatibility
  - ci
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-07T04:01:07.109Z
refinementState: needs-refinement
---

Use supported .NET package validation to make public API and package-layout changes reviewable before
publishing the independent release line.

## Acceptance criteria

- [ ] `EnablePackageValidation` runs during pack for the chosen package identity.
- [ ] The baseline package and version follow TASK-008's ownership and versioning decision.
- [ ] Intentional framework and API breaks from the inherited `2.3.1` line are represented by reviewed
      compatibility suppressions and migration notes.
- [ ] A human-readable shipped/unshipped API record or equivalent review artifact supplements or
      replaces the current hash-only reflection baseline.
- [ ] Package contents, reference assemblies, implementation assemblies, symbols, and framework groups
      are validated in CI.
- [ ] Accidental binary breaks fail CI while approved next-major changes remain explicit in source.
- [ ] The validation baseline advances after each stable independent release.

## Baseline findings

The repository currently fingerprints formatted reflection output with SHA-256. That detects change
but does not show reviewers which API changed. The .NET SDK's package validation tooling can compare
against a released package and record intentional compatibility suppressions as versioned evidence.
