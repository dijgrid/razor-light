---
id: TASK-005
title: Modernize the test suite and quality gates
status: todo
priority: high
epic: EPIC-modernization
milestone: MILESTONE-modernization-foundation
dependsOn:
  - TASK-003
  - TASK-004
tags:
  - tests
  - ci
  - quality
createdAt: 2026-08-06T00:00:00Z
updatedAt: 2026-08-06T00:00:00Z
---

Make the test suite reliable on supported runtimes and turn CI into a trustworthy merge gate.

## Acceptance criteria

- [ ] Test projects target only supported runtimes.
- [ ] Test SDKs, runners, mocking, snapshot, and assertion packages are current.
- [ ] Tests pass on Windows, Linux, and macOS or documented platform exclusions are enforced.
- [ ] Flaky, skipped, and timing-sensitive tests are inventoried and resolved or justified.
- [ ] CI performs restore, Release build, tests, and package validation.
- [ ] Code coverage is produced in a portable format with an agreed initial baseline.
- [ ] Required status checks are configured after stable check names are established.

## Baseline findings

On 2026-08-06, the precompile suite passed 118/118 when its `net6.0` binary was explicitly rolled
forward to ASP.NET Core 10. The main suite passed 179/182 under the same supported-runtime probe.
The three failures are newline-sensitive assertions:

- `TemplateRendererTest.Templates_Supports_Local_Functions`
- `TemplateRendererTest.Template_Shares_Model_With_Layout`
- `TemplateRendererTest.Templates_Supports_Local_Functions_Using_Helper`

Determine whether the new whitespace is an intentional Razor-version behavior change or a RazorLight
regression before updating assertions.
