---
id: TASK-005
title: Modernize the test suite and quality gates
status: done
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
updatedAt: 2026-08-07T00:15:00Z
---

Make the test suite reliable on supported runtimes and turn CI into a trustworthy merge gate.

## Acceptance criteria

- [x] Test projects target only supported runtimes.
- [x] Test SDKs, runners, mocking, snapshot, and assertion packages are current.
- [x] Tests pass on Windows, Linux, and macOS or documented platform exclusions are enforced.
- [x] Flaky, skipped, and timing-sensitive tests are inventoried and resolved or justified.
- [x] CI performs restore, Release build, tests, and package validation.
- [x] Code coverage is produced in a portable format with an agreed initial baseline.
- [x] Required status checks are configured after stable check names are established.

## Baseline findings

On 2026-08-06, the precompile suite passed 118/118 when its `net6.0` binary was explicitly rolled
forward to ASP.NET Core 10. The main suite passed 179/182 under the same supported-runtime probe.
The three failures are newline-sensitive assertions:

- `TemplateRendererTest.Templates_Supports_Local_Functions`
- `TemplateRendererTest.Template_Shares_Model_With_Layout`
- `TemplateRendererTest.Templates_Supports_Local_Functions_Using_Helper`

Determine whether the new whitespace is an intentional Razor-version behavior change or a RazorLight
regression before updating assertions. CI temporarily filters these three tests on Windows while
running the other 179 main-suite tests there; Linux and macOS run all 182.

The first cross-platform CI runs on 2026-08-06 also found 16 precompile failures on macOS and Linux.
The rendered output uses `3:35:49PM` while the inherited expected fixture uses `3:35:49 PM`.
CI temporarily runs the precompile suite only on Windows while retaining restore, build, and
main-suite coverage on all three systems. Replace the culture-sensitive expectation and restore the
macOS and Linux precompile jobs as part of this task.

## Resolution

The newline assertions and culture fixture were made deterministic, so all 301 tests now run on all
three operating systems without exclusions. CI produces per-suite Cobertura reports, uploads them as
per-platform artifacts, and validates the Release NuGet package on Linux. See
[`docs/testing.md`](../../docs/testing.md) for the reliability inventory and initial coverage
baseline. The stable matrix check names are `ubuntu-latest`, `windows-latest`, and `macos-latest`;
all three are configured as strict required status checks for the protected `master` branch.
