---
id: TASK-030
title: Prepare and publish the 3.0.0-beta.1 release
status: todo
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
updatedAt: 2026-08-08T16:22:29.674Z
refinementState: deferred
---

Publish the first independently maintained prerelease after the generic-core and public-API cleanup
is complete. This task is intentionally deferred until the 2026-08-08 pre-release safety,
performance, lifecycle, precompiled-runtime, and maintainability findings are complete.

## Acceptance criteria

- [ ] Version and tag policy accepts `3.0.0-beta.1`, and GitHub creates a prerelease rather than a
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
