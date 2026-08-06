---
id: TASK-001
title: Establish repository governance and PlanFS
status: in_progress
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
updatedAt: 2026-08-06T00:00:00Z
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
- [ ] Repository files, CI commands, and PlanFS front matter are validated.
- [ ] The draft pull request describes the expanded repository-foundation scope.

## Implementation notes

Keep package publishing disabled until TASK-008. The existing test baseline still needs the .NET 6
runtime even though repository tooling is selected with the .NET 10 SDK.
