---
id: TASK-043
title: Upgrade and validate stable NuGet dependencies
status: review
priority: medium
createdAt: 2026-10-27T19:32:00.161Z
updatedAt: 2026-10-27T19:38:54.764Z
---

Upgrade the existing NuGet package IDs to their latest stable versions from official registry data,
validate all projects, and prepare the changes for review.

## Acceptance criteria

- [x] Inventory central versions, all projects, samples, tools and lock files.
- [x] Check existing dependency PRs read-only and record overlaps.
- [x] Apply compatible stable updates and assess major-version and replacement-package migrations.
- [x] Run Release build, test/coverage, sample, package, consumer and vulnerability checks.
- [x] Record validation limitations and deferred migration/tooling work.

## Implementation notes

18 central versions were updated from master at merged PR #47. No source compatibility fixes were
required. Local validation passes, including 332 runtime tests and 78 precompile tests on macOS.
All 13 maintained projects and generated Functions dependencies have no reported vulnerabilities.
The separate Source Link CLI audit has advisory findings tracked in TASK-045; legacy xUnit/Verify
migration is TASK-044. Full results and version tables are in
`docs/dependency-upgrade-2026-10-27.md`. Changes are ready for draft PR review; merging and package publication are separate decisions.
