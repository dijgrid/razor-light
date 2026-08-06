# AGENTS.md

Guidance for coding agents working in this repository.

## Project overview

RazorLight is a Razor-based template engine for rendering files, embedded resources, strings, and
custom template sources outside ASP.NET MVC. This repository is an independently maintained
continuation of `toddams/RazorLight`.

The primary library lives in `src/RazorLight`, the precompile tool lives in
`src/RazorLight.Precompile`, tests live in `tests`, samples live in `samples`, and planning records
live in `.planfs`.

## Setup and common commands

Use the SDK selected by `global.json`:

```shell
dotnet restore RazorLight.sln
dotnet build RazorLight.sln --configuration Release --no-restore
```

Run the maintained test baseline:

```shell
dotnet test tests/RazorLight.Tests/RazorLight.Tests.csproj --framework net10.0 --configuration Release
dotnet test tests/RazorLight.Precompile.Tests/RazorLight.Precompile.Tests.csproj --configuration Release
```

The maintained projects target .NET 10, the current LTS baseline. Do not reintroduce end-of-life
target frameworks without an explicit compatibility decision and secure dependency plan.

## Planning

Use `.planfs` for task tracking.

- Tasks live in `.planfs/tasks/TASK-###.md`.
- Epics live in `.planfs/epics`.
- Milestones live in `.planfs/milestones`.
- Architecture and maintenance decisions live in `.planfs/decisions`.
- Keep front matter and checkbox acceptance criteria consistent with existing files.
- When implementing a tracked task, update its status, acceptance criteria, and implementation
  notes in the same change.
- Record newly discovered work as a task instead of silently expanding the active task.

If a PlanFS initializer is available, prefer it for new records. Otherwise, mirror the existing
repository-native Markdown format.

## Compatibility and security

- Treat public API, generated Razor code, package metadata, and supported target frameworks as
  compatibility-sensitive.
- Add focused regression tests before changing compilation, caching, project lookup, or rendering
  behavior.
- Do not suppress package vulnerability warnings without documenting a concrete risk decision.
- Do not publish packages until `TASK-008` establishes package ownership, identity, and release
  controls.
- Never include secrets, private templates, or customer data in fixtures, logs, issues, or commits.

## Documentation

`README.md` is generated from `README.source.md` by MarkdownSnippets. Edit the source first and
include the synchronized generated output.

Keep `CHANGELOG.md`, `SECURITY.md`, `SUPPORT.md`, and `UPSTREAM.md` aligned with behavior and release
policy changes.

## Verification

Before handing off changes, run the narrowest relevant tests plus:

```shell
dotnet build RazorLight.sln --configuration Release
git diff --check
```

Mention any validation that could not be run and why.

Use the SDK selected by `global.json`; CI and local validation are expected to run on .NET 10.
