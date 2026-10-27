---
id: TASK-044
title: Migrate legacy xUnit 2 and Verify test integration to xUnit v3
status: todo
priority: medium
createdAt: 2026-10-27T19:34:56.615Z
updatedAt: 2026-10-27T19:34:56.615Z
---

NuGet marks the latest `xunit` 2.9.3 and `Verify.Xunit` 31.12.5 as legacy and recommends replacement
package IDs. This is a coordinated test-framework migration, not a same-package version bump.

## Acceptance criteria

- [ ] Replace xUnit 2 with xUnit v3 in both maintained suites and configure executable test projects.
- [ ] Replace Verify.Xunit with Verify.XunitV3 and preserve existing snapshots.
- [ ] Remove or replace the xUnit 2-specific SkippableFact integration as appropriate.
- [ ] Validate test discovery, theory row counts, fixtures, snapshots and coverage on all supported CI OSes.
- [ ] Confirm no legacy xUnit 2 dependencies remain and rerun the vulnerability audit.

## Implementation notes

Discovered during TASK-043. Current same-ID latest versions and the 4.0.1 runner pass all macOS tests.
See https://xunit.net/docs/getting-started/v3/migration and the dependency upgrade report.
