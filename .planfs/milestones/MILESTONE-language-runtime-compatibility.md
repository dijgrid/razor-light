---
id: MILESTONE-language-runtime-compatibility
title: Language and Runtime Compatibility
targetDate: 2027-01-31
status: active
owner: justin
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-07T03:54:00Z
---

Bring RazorLight's template compiler, model behavior, caching, and deployment story in line with the
current .NET and Razor toolchain.

## Deliverables

- An executable baseline matrix for template source, imports, model typing, and LINQ behavior
- Predictable LINQ, import, and model-type behavior for every template source
- A supported current Razor compiler integration and current C# syntax coverage
- Tested framework-dependent, self-contained, single-file, trimming, and AOT guidance
- Coherent cache invalidation and complete precompiled-cache contracts
- Intentional ASP.NET Core runtime, dependency-injection, and ViewBag behavior
- A supported precompiled-only execution path where the platform permits it

## Success criteria

- Runtime behavior is covered across string, file, embedded, and custom projects.
- Unsupported deployment modes fail early with actionable diagnostics.
- Compiler and cache changes preserve or intentionally migrate the recorded compatibility baseline.
- No public cache or rendering path relies on an undocumented `NotImplementedException`.
