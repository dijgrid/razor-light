---
id: TASK-030
title: Prepare and publish the 3.0.0-beta.4 release
status: in-progress
priority: high
epic: EPIC-modernization
milestone: MILESTONE-release-readiness
dependsOn:
  - TASK-017
  - TASK-022
  - TASK-021
  - TASK-027
  - TASK-028
  - TASK-029
  - TASK-032
  - TASK-033
  - TASK-034
  - TASK-035
  - TASK-036
  - TASK-037
  - TASK-038
tags:
  - release
  - nuget
  - beta
  - compatibility
createdAt: 2026-08-08T04:17:59.721Z
updatedAt: 2026-08-08T23:06:00.000Z
refinementState: ready
---

Publish the first independently maintained prerelease after the generic-core and public-API cleanup
is complete. This task is intentionally deferred until the 2026-08-08 pre-release safety,
performance, lifecycle, precompiled-runtime, and maintainability findings are complete.

## Acceptance criteria

- [ ] Version and tag policy accepts `3.0.0-beta.4`, and GitHub creates a prerelease rather than a
      stable release.
- [ ] `Dijgrid.RazorLight`, `Dijgrid.RazorLight.Html`, and
      `Dijgrid.RazorLight.Precompile` are built once, validated, approved through the protected
      `nuget` environment, and published from the reviewed artifacts.
- [ ] Release notes lead with the .NET 10 requirement, generic-text default, optional HTML encoding,
      removed tag helpers, package-ID change, and intentional API removals.
- [ ] A complete 2.3.1-to-3.0 migration guide is linked from the README and package metadata.
- [ ] A clean consumer installs the locally packed beta artifacts and smoke-tests generic text and
      opt-in HTML before publication.
- [ ] Warning-as-error build, all maintained tests, deployment probes, dependency audit, deterministic
      outputs, package validation, symbols, and Source Link pass on the release commit.
- [ ] Public API entries remain unshipped and package validation remains anchored to `RazorLight`
      2.3.1 until the first stable `Dijgrid.RazorLight` release.
- [ ] NuGet.org and GitHub artifacts have matching hashes and the release is not promoted to stable
      without explicit maintainer approval.

## Implementation notes

- Configured the package projects to evaluate to `3.0.0-beta.4` and updated release automation to
  require an exact SemVer prerelease tag, publish Core, optional HTML, and precompile packages plus
  symbols, verify artifact hashes after the protected deployment approval, and create a GitHub
  prerelease with versioned notes.
- Added `docs/migration-3.0.md`, linked it from the README and package metadata, and added beta
  release notes covering the framework, package, rendering-default, tag-helper, and API changes.
- Added a clean-consumer smoke test that restores locally packed Core and optional HTML packages,
  verifies plain and encoded rendering, and installs the precompile tool from the same package source.
- Fixed the macOS CI regression in file-system project tests: Unix-leading-slash template keys retain
  their documented project-root-relative meaning, while fully qualified C# source paths are rejected.
- Local release-candidate validation passed on Windows: warning-as-error build; 318 Core and 134
  precompile tests with coverage floors; deployment-mode and deployment-diagnostic checks; dependency
  audit; deterministic builds; package/symbol validation; and clean-consumer smoke test. Source Link
  and the multi-OS suite remain to be verified by CI for the committed branch.
- The `v3.0.0-beta.1` build-and-verify job succeeded, but its approved-artifact job stopped before
  NuGet authentication because it tried to compare downloaded artifacts with absent local build output.
  The release workflow now uses artifact-only validation for that job while retaining archive, symbol,
  Source Link, and SHA-256 checks; a new prerelease tag is required because reruns use the workflow
  definition recorded in the existing tag.
- `v3.0.0-beta.2` correctly failed before building because its tag did not match the evaluated
  `3.0.0-beta.1` package version. `v3.0.0-beta.3` published the Core package and symbol successfully,
  then failed when the workflow redundantly submitted the automatically uploaded symbol package.
  `3.0.0-beta.4` submits primary packages only and uses duplicate-tolerant publication.
- The clean-consumer smoke test now uses a canonical trailing-slash NuGet v3 source URL so the local
  Windows command path is not interpreted as a file-system source.
