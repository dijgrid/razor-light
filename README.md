# RazorLight

> [!IMPORTANT]
> This repository is an independently maintained continuation of
> [toddams/RazorLight](https://github.com/toddams/RazorLight). Compatibility and release policy may
> diverge from the historical project. See [UPSTREAM.md](UPSTREAM.md) for provenance.

RazorLight is a generic C# text-template engine built on Razor. It compiles templates and renders
them outside ASP.NET MVC, making Razor syntax useful for source generation, configuration files,
messages, reports, prompts, and other structured or plain-text output.

The maintained release targets **.NET 10** and is published as `Dijgrid.RazorLight`. HTML behavior
is optional; the core library treats output as text.

![Build Status](https://github.com/dijgrid/razor-light/actions/workflows/dotnet.yml/badge.svg)
[![NuGet](https://img.shields.io/nuget/v/Dijgrid.RazorLight.svg)](https://www.nuget.org/packages/Dijgrid.RazorLight/)

## Capabilities

- Render Razor templates from strings, files, embedded resources, databases, or custom projects.
- Use dynamic or strongly typed models, standard Razor expressions, imports, LINQ, layouts,
  sections, and partial templates.
- Compose templates with trusted ordinary C# helper files through `AddCSharpSource` or
  `@compileSource`.
- Cache compiled templates and invalidate dependent templates when project sources change.
- Cancel template lookup, compilation waits, rendering, layouts, and includes cooperatively.
- Precompile templates with the `Dijgrid.RazorLight.Precompile` command-line tool and render them in a
  compiler-free execution mode that never falls back to runtime compilation.
- Select plain-text output, optional HTML encoding, or a custom output encoder.
- Control namespaces, metadata references, dependency discovery, diagnostics, and integration with
  dependency injection.
- Run framework-dependent, self-contained, and extraction-based single-file deployments on Windows
  and Linux.

## Install

```shell
dotnet add package Dijgrid.RazorLight --version 3.0.0
```

The historical `RazorLight 2.3.1` package is the upstream release, not this continuation. Moving to
the `Dijgrid.RazorLight` 3.x line is a framework and API migration. Read the
[2.3.1-to-3.0 migration guide](docs/migration-3.0.md) before upgrading.

## Quick example

```csharp
var engine = new RazorLightEngineBuilder()
    .UseMemoryCachingProvider()
    .Build();

string template = "Hello, @Model.Name. Welcome to RazorLight repository";
ViewModel model = new ViewModel { Name = "John Doe" };

string result = await engine.CompileRenderStringAsync("templateKey", template, model);
```

Each template key identifies a logical template and allows its compiled form to be reused. String
templates work without a project; project-backed templates add file lookup, embedded resources,
layouts, includes, and source change tracking.

For example, render a file relative to a configured template root:

```csharp
var engine = new RazorLightEngineBuilder()
    .UseFileSystemProject("C:/Templates")
    .UseMemoryCachingProvider()
    .Build();

string result = await engine.CompileRenderAsync(
    "Reports/Summary.cshtml",
    new { Title = "Quarterly summary" });
```

The engine exposes cache administration without leaking compiled-page factories:

```csharp
if (engine.IsTemplateCached("Reports/Summary.cshtml"))
{
    engine.InvalidateTemplate("Reports/Summary.cshtml");
}
```

Templates can also select ordinary C# helper code:

```razor
@compileSource "../Shared/ScenarioFunctions.cs"
@using ScenarioTemplates
@ScenarioFunctions.Quote(Model.Name)
```

See the **[RazorLight manual](docs/manual.md)** for installation details, template sources, models,
composition, includes, caching, encoding, precompilation, deployment, and troubleshooting examples.

## Output and trust model

The core package writes expression results as plain text. Install `Dijgrid.RazorLight.Html` and call
`UseHtmlEncoding()` when producing HTML, or implement `IOutputEncoder` for another format.

Razor templates and imported C# sources are executable .NET code with the host process's
permissions. RazorLight is intended for application-owned or otherwise trusted templates. It is not
an in-process sandbox for untrusted authors; see the
[template security guide](docs/template-security.md).

## Compatibility

- Maintained projects, tests, samples, and generated code target .NET 10 and C# 14.
- Runtime compilation requires dynamic code and a preserved compilation context. Trimming and
  Native AOT are not supported by the runtime compiler.
- The generic core does not provide MVC tag helpers or built-in HTML contracts.
- Public compatibility, language behavior, and deployment expectations are recorded in the
  repository documentation rather than inferred from the historical package.

## Documentation

- [Manual](docs/manual.md)
- [Migrating from RazorLight 2.3.1](docs/migration-3.0.md)
- [Framework support and migration](docs/framework-support.md)
- [Template language compatibility](docs/template-language-compatibility.md)
- [Caching and invalidation](docs/caching.md)
- [Cancellation](docs/cancellation.md)
- [Deployment compatibility](docs/deployment.md)
- [Template security](docs/template-security.md)
- [Public API design](docs/api-design-3.0.md)
- [Release process](docs/releasing.md)
- [Changelog](CHANGELOG.md)

## Project maintenance

Read [CONTRIBUTING.md](CONTRIBUTING.md) before contributing, use the repository's GitHub issue forms
for reproducible bugs and focused proposals, and report vulnerabilities according to
[SECURITY.md](SECURITY.md). Accepted roadmap work is tracked in [`.planfs`](.planfs).
