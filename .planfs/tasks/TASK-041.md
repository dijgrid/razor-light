---
id: TASK-041
title: Publish the stable 3.0.0 release
status: in-progress
priority: high
epic: EPIC-modernization
milestone: MILESTONE-release-readiness
dependsOn:
  - TASK-039
  - TASK-040
tags:
  - release
  - nuget
  - stable
  - github
createdAt: 2026-08-10T19:03:58.402Z
updatedAt: 2026-08-10T19:03:58.402Z
refinementState: ready
---

Publish the first stable independently maintained RazorLight release after the documented API,
documentation, and scaling gates are complete.

## Acceptance criteria

- [ ] The release PR sets the evaluated package version to `3.0.0`, promotes the changelog, and
      provides matching stable release notes.
- [ ] The reviewed PR is merged to `master` with the final tests and release validation green.
- [ ] An annotated `v3.0.0` tag points to the merged `master` commit and triggers the release
      workflow.
- [ ] The release workflow's reviewed artifacts are approved for the protected `nuget` environment,
      publishes all primary packages and symbols, and creates a non-prerelease GitHub Release with
      the matching assets and notes.
- [ ] NuGet package pages, symbols, installation, GitHub assets, hashes, and release notes are
      verified after publication.

## Implementation notes

In progress.

## Verification

In progress.
