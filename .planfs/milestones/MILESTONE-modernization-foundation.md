---
id: MILESTONE-modernization-foundation
title: Modernization Foundation
targetDate: 2026-10-31
status: completed
owner: justin
createdAt: 2026-08-06T00:00:00Z
updatedAt: 2026-08-07T00:23:26Z
---

Move RazorLight from its inherited maintenance state to a secure, testable independent project
baseline. TASK-001 through TASK-007 were completed and merged in pull request 9.

## Deliverables

- Repository governance, contribution, security, and planning metadata
- A documented compatibility baseline before changing target frameworks
- Supported .NET target frameworks and current package dependencies
- A reliable cross-platform test suite on supported runtimes
- Resolved compiler, analyzer, and package vulnerability warnings
- Current samples and user documentation
- A staged nullable-adoption plan and follow-up roadmap

## Success criteria

- Supported frameworks are covered by CI and are still supported by Microsoft.
- Restore, build, test, and pack complete without known high-severity dependency advisories.
- Compatibility-sensitive changes have an explicit baseline and migration notes.
- All foundation work is represented by completed PlanFS tasks.
- Release automation and post-foundation runtime work are assigned to separate active milestones.

## Completed tasks

- TASK-001: Establish repository governance and PlanFS
- TASK-002: Capture the compatibility and package baseline
- TASK-003: Move to supported target frameworks
- TASK-004: Centralize and secure package dependencies
- TASK-005: Modernize the test suite and quality gates
- TASK-006: Resolve implementation warnings and obsolete APIs
- TASK-007: Refresh documentation and samples

TASK-008 moved to `MILESTONE-release-readiness`, and TASK-009 moved to
`MILESTONE-library-quality`, so this milestone reflects only the work that actually shipped in the
foundation pull request.
