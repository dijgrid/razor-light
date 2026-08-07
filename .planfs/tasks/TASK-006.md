---
id: TASK-006
title: Resolve implementation warnings and obsolete APIs
status: done
priority: medium
epic: EPIC-modernization
milestone: MILESTONE-modernization-foundation
dependsOn:
  - TASK-003
  - TASK-004
tags:
  - cleanup
  - compiler
  - analyzers
createdAt: 2026-08-06T00:00:00Z
updatedAt: 2026-08-07T00:35:00Z
---

Remove obsolete runtime assumptions and establish a manageable compiler and analyzer baseline.

## Acceptance criteria

- [x] `Assembly.CodeBase` usage is replaced with supported assembly location behavior.
- [x] Existing warning suppressions are reviewed and narrowed or documented.
- [x] Nullable reference types are evaluated project by project with a staged adoption plan.
- [x] Dead code and unreachable target-framework conditions are removed.
- [x] Formatting and analyzer rules are automated without a repository-wide unrelated rewrite.
- [x] Build output has no unexplained compiler or analyzer warnings.

## Implementation notes

The solution now builds with zero warnings under the latest SDK analyzer level, and CI promotes any
future compiler or analyzer warning to an error. Supported assembly paths use `Assembly.Location`;
recursive reference discovery uses assembly identity instead of obsolete code-base URIs. Obsolete
test APIs and blocking task access were replaced, while the one test that deliberately covers an
obsolete public constructor has a local suppression.

The nested `src/Directory.Build.props` file was removed so the repository-wide deterministic and
Source Link packaging policy actually reaches the library; its version property moved to the root
file with the package-only settings scoped to the RazorLight project. Production and sample
target-framework branches that can no longer be reached on .NET 10 were removed. See
[`docs/code-quality.md`](../../docs/code-quality.md) for the warning and nullable adoption policy;
remaining project-by-project nullable adoption is recorded as TASK-009.
