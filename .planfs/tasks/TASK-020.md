---
id: TASK-020
title: Reconcile Dependabot with central package management
status: todo
priority: high
epic: EPIC-modernization
milestone: MILESTONE-release-readiness
dependsOn:
  - TASK-004
tags:
  - dependencies
  - dependabot
  - security
  - github
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-07T00:29:26Z
---

Clean up dependency-update state created before central package management reached the default branch
and ensure future updates modify the authoritative version declarations coherently.

## Acceptance criteria

- [x] Dependabot pull requests 2 through 8 are compared with `Directory.Packages.props` and closed or
      superseded without discarding a newer secure version.
- [ ] Dependabot recognizes the central package file and produces one coherent update per configured
      dependency family rather than project-scoped duplicates.
- [x] The default branch has no open Dependabot security alert after the merged dependency baseline is
      re-indexed.
- [ ] Direct and transitive NuGet audits run in CI or an explicitly scheduled security workflow.
- [ ] Dependency update pull requests run the same build, tests, package validation, and sample checks
      as maintainer branches.
- [ ] Update cadence, grouping, and ignore rules are documented and contain no unexplained permanent
      version suppression.

## Baseline findings

Dependabot opened pull requests 2 through 8 from the pre-modernization project files. Their proposed
versions are already present in the merged central package file. After pull request 9 merged, GitHub
closed the superseded Dependabot pull requests and marked the only recorded security alert,
GHSA-qj66-m88j-hmgj, fixed. The remaining work is to verify that future updates originate from the
central package declarations and run the complete security and CI policy.
