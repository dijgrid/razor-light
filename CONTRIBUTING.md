# Contributing

Thank you for helping maintain RazorLight. This repository is an independent continuation of the
original project; see [UPSTREAM.md](UPSTREAM.md) for provenance and the upstream policy.

## Before starting

- Search existing issues and `.planfs/tasks` for related work.
- Open an issue before making a large behavioral or public API change.
- Keep compatibility impact explicit. RazorLight is consumed as a library, so seemingly small
  changes can affect compilation and runtime template behavior.
- Do not include secrets, private templates, or proprietary customer data in issues or tests.

## Development setup

Install the SDK selected by `global.json` and restore the solution:

```shell
dotnet restore RazorLight.sln
dotnet build RazorLight.sln --configuration Release --no-restore
```

The legacy test project currently targets several end-of-life runtimes. Until the modernization
tasks are complete, the maintained CI baseline temporarily uses the .NET 6 SDK and ASP.NET Core
runtime in addition to the SDK selected by `global.json`:

```shell
dotnet test tests/RazorLight.Tests/RazorLight.Tests.csproj --framework net6.0 --configuration Release
dotnet test tests/RazorLight.Precompile.Tests/RazorLight.Precompile.Tests.csproj --configuration Release
```

Do not install the older .NET Core 2.x, .NET Core 3.1, or .NET 5 runtimes. On a machine without the
.NET 6 ASP.NET Core runtime, `DOTNET_ROLL_FORWARD=Major` can be used for a supported-runtime probe;
the three known newline-sensitive failures from that probe are recorded in `TASK-005`.

## Documentation

`README.md` is generated from `README.source.md` by MarkdownSnippets. Edit the source file and make
sure the generated file is included in the same change.

## Planning

Project work is tracked in `.planfs`:

- Tasks: `.planfs/tasks/TASK-###.md`
- Epics: `.planfs/epics/EPIC-*.md`
- Milestones: `.planfs/milestones/MILESTONE-*.md`
- Decisions: `.planfs/decisions/DECISION-###.md`

When implementing a tracked task, update its status, acceptance criteria, and implementation notes
as part of the same pull request.

## Pull requests

- Keep each pull request focused and explain compatibility or package impact.
- Add tests for behavior changes and update documentation for user-visible changes.
- Run the relevant build and test commands plus `git diff --check`.
- Link the PlanFS task in the pull request description when applicable.
- Do not add or restore package-publishing automation until the package identity and release policy
  task is complete.
