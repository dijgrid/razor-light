---
id: MILESTONE-modernization-foundation
title: Modernization Foundation
targetDate: 2026-10-31
status: active
owner: justin
createdAt: 2026-08-06T00:00:00Z
updatedAt: 2026-08-06T00:00:00Z
---

Move RazorLight from its inherited maintenance state to a secure, testable, and releasable
independent project baseline.

## Deliverables

- Repository governance, contribution, security, and planning metadata
- A documented compatibility baseline before changing target frameworks
- Supported .NET target frameworks and current package dependencies
- A reliable cross-platform test suite on supported runtimes
- Resolved compiler, analyzer, and package vulnerability warnings
- Current samples and user documentation
- An explicit package identity, versioning, and release policy

## Success criteria

- Supported frameworks are covered by CI and are still supported by Microsoft.
- Restore, build, test, and pack complete without known high-severity dependency advisories.
- Compatibility-sensitive changes have an explicit baseline and migration notes.
- Package publishing is reproducible, reviewable, and protected from accidental execution.
- All milestone work is represented by completed PlanFS tasks.
