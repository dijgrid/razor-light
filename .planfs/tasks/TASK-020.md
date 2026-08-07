---
id: TASK-020
title: Reconcile Dependabot with central package management
status: done
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
  - ready-for-implementation
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-07T06:45:34.678Z
refinementState: ready
---

Clean up dependency-update state created before central package management reached the default branch
and ensure future updates modify the authoritative version declarations coherently.

## Implementation readiness

Implementation, default-branch validation, and a fresh Dependabot NuGet update are complete. The
update recognized `Directory.Packages.props` for every project and processed each configured package
family as a single group.

## Implementation plan

1. Trigger or observe a Dependabot NuGet update against the default branch and confirm that it edits
   `Directory.Packages.props` rather than individual project files.
2. Adjust dependency groups only where the observed update graph shows that packages must move
   together; do not add speculative ignores.
3. Add a least-privilege scheduled dependency-audit workflow, or a dedicated CI job, that restores
   the solution and fails on known direct or transitive vulnerabilities at the repository's chosen
   severity threshold.
4. Confirm dependency pull requests exercise the existing cross-platform build/test job, package
   validation, README check, and sample checks.
5. Document the weekly update cadence, group rationale, audit schedule, alert handling, and the
   process for time-bounded suppressions.

## Scope boundaries

- Keep package versions authoritative in `Directory.Packages.props`.
- Do not update unrelated packages merely to test automation.
- Do not suppress an advisory or permanently ignore a version without a separate documented risk
  decision, owner, and expiry/review date.

## Verification

- Validate `.github/dependabot.yml` and every changed workflow as YAML.
- Run the selected direct/transitive NuGet audit locally with the .NET 10 SDK.
- Run `dotnet build RazorLight.sln --configuration Release` and the two maintained test projects.
- Confirm the GitHub Actions event filters do not special-case or bypass Dependabot pull requests.

## Acceptance criteria

- [x] Dependabot pull requests 2 through 8 are compared with `Directory.Packages.props` and closed or
      superseded without discarding a newer secure version.
- [x] Dependabot recognizes the central package file and produces one coherent update per configured
      dependency family rather than project-scoped duplicates.
- [x] The default branch has no open Dependabot security alert after the merged dependency baseline is
      re-indexed.
- [x] Direct and transitive NuGet audits run in CI or an explicitly scheduled security workflow.
- [x] Dependency update pull requests run the same build, tests, package validation, and sample checks
      as maintainer branches.
- [x] Update cadence, grouping, and ignore rules are documented and contain no unexplained permanent
      version suppression.

## Baseline findings

Dependabot opened pull requests 2 through 8 from the pre-modernization project files. Their proposed
versions are already present in the merged central package file. After pull request 9 merged, GitHub
closed the superseded Dependabot pull requests and marked the only recorded security alert,
GHSA-qj66-m88j-hmgj, fixed. The remaining work is to verify that future updates originate from the
central package declarations and run the complete security and CI policy.

## Implementation notes

- Replaced the broad Microsoft, build-tooling, and test-tooling groups with narrowly coupled Razor,
  Roslyn, Entity Framework, Microsoft.Extensions, Azure Functions, xUnit, and NUnit families.
  Unmatched NuGet packages and GitHub Actions update independently; no ignore rules were added.
- Added repository-wide NuGet audit settings with direct and transitive coverage and a moderate
  severity threshold. The dedicated audit pipeline also fails when vulnerability data cannot be
  obtained.
- Added a read-only `Dependency Audit` workflow for dependency-changing pull requests and default
  branch pushes, weekly Tuesday execution, and manual dispatch. The existing CI workflow has an
  unfiltered `pull_request` trigger and no Dependabot actor exclusions.
- Updated `docs/dependency-policy.md` with the group rationale, schedules, local commands, alert
  handling, and the required time-bounded decision record for any suppression.
- The real solution and Azure Functions sample have no known direct or transitive vulnerabilities.
  A temporary `Microsoft.Extensions.Caching.Memory` 6.0.1 probe verified that the audit pipeline
  fails on the repository's previously fixed high-severity advisory (`NU1903`).
- Default-branch [Dependabot run 31154789591](https://github.com/dijgrid/razor-light/actions/runs/31154789591)
  succeeded against commit `b082639`, recognized `Directory.Packages.props` as the package-management
  special file throughout the solution, and processed all seven configured groups. It opened no
  pull request because the central versions were already current.
- The same commit passed [Dependency Audit run 31154787276](https://github.com/dijgrid/razor-light/actions/runs/31154787276)
  and the complete three-platform [CI run 31154787277](https://github.com/dijgrid/razor-light/actions/runs/31154787277).
