---
id: TASK-001
title: Establish repository governance and PlanFS
status: done
priority: high
epic: EPIC-modernization
milestone: MILESTONE-modernization-foundation
dependsOn: []
tags:
  - repository
  - governance
  - github-actions
  - planfs
createdAt: 2026-08-06T00:00:00Z
updatedAt: 2026-08-06T23:16:00Z
---

Replace inherited repository metadata and unsafe automation with an independently maintained project
foundation and a versioned PlanFS backlog.

## Acceptance criteria

- [x] EditorConfig, Git attributes, and ignore rules are current and cross-platform.
- [x] Contribution, security, support, conduct, changelog, and agent guidance exist.
- [x] GitHub issue forms, pull request guidance, ownership, and Dependabot configuration exist.
- [x] CI uses current official actions with read-only token permissions and concurrency controls.
- [x] Obsolete PAT-based rebase and direct package-publishing workflows are removed.
- [x] PlanFS contains the modernization epic, milestone, decisions, filter, and actionable tasks.
- [x] Repository files, solution metadata, and PlanFS front matter are validated locally.
- [x] CI completes after GitHub registers the workflow on the default branch.
- [x] The draft pull request describes the expanded repository-foundation scope.

## Implementation notes

Keep package publishing disabled until TASK-008. The existing test baseline still needs the .NET 6
runtime even though repository tooling is selected with the .NET 10 SDK.

GitHub did not register the inherited workflow files after the fork detachment, so the new workflow
cannot be dispatched before it exists on the default branch. Dependabot configuration validation
does run and passes on the pull request.

The first registered run was not assigned hosted runners. Subsequent runs executed the matrix and
identified 16 inherited, culture-sensitive precompile assertions on macOS and Linux. TASK-005 records
that work; the initial CI baseline continues to build and run the main suite on all three systems,
with three documented newline-sensitive tests filtered on Windows, and runs the precompile suite on
Windows.

The final TASK-001 validation completed successfully in GitHub Actions run
`31130094618` on the exact task commit. All three operating-system jobs completed successfully.
