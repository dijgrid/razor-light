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

The maintained build and test baseline uses the .NET 10 SDK and ASP.NET Core runtime selected by
`global.json`:

```shell
dotnet test tests/RazorLight.Tests/RazorLight.Tests.csproj --framework net10.0 --configuration Release
dotnet test tests/RazorLight.Precompile.Tests/RazorLight.Precompile.Tests.csproj --configuration Release
```

Do not install or reintroduce end-of-life .NET runtimes to make a change pass. The maintained build
and test baseline is .NET 10.

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
- Package publication occurs only through the protected process in [`docs/releasing.md`](docs/releasing.md).
  Pull requests and local validation must never push packages.
