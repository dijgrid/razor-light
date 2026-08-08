# RazorLight

> [!IMPORTANT]
> This repository is an independently maintained continuation of
> [toddams/RazorLight](https://github.com/toddams/RazorLight). It is no longer maintained as a
> contribution fork, so compatibility and release policy may diverge. See [UPSTREAM.md](UPSTREAM.md)
> for provenance and the upstream synchronization policy.

> [!NOTE]
> The maintained release line targets .NET 10 and is published as `Dijgrid.RazorLight`, beginning
> with version `3.0.0`. The `RazorLight 2.3.1` package is the historical upstream build rather than
> this continuation.

Use Razor as a generic C# text-template engine with templates from files, embedded resources,
strings, databases, or a custom source. HTML output support is optional rather than built into the
core package. The maintained source and samples support **.NET 10**. See
[framework support](docs/framework-support.md), the [dependency policy](docs/dependency-policy.md),
and [testing guidance](docs/testing.md) for the current maintenance baseline.

![Build Status](https://github.com/dijgrid/razor-light/actions/workflows/dotnet.yml/badge.svg)
[![NuGet](https://img.shields.io/nuget/v/Dijgrid.RazorLight.svg)](https://www.nuget.org/packages/Dijgrid.RazorLight/)

# Table of contents

- [Quickstart](#quickstart)
- [Compatibility and support](#compatibility-and-support)
- [Models, imports, and LINQ](#models-imports-and-linq)
- [Template sources](#template-sources)
  - [Files](#file-source)
  - [Embedded resources](#embedded-resource-source)
  - [Custom sources](#custom-source)
- [C# source composition](#c-source-composition)
- [Includes and partial templates](#includes-and-partial-templates)
- [Caching and invalidation](#caching-and-invalidation)
- [Encoding](#encoding)
- [Template security](#template-security)
- [Additional metadata references](#additional-metadata-references)
- [Enable IntelliSense support](#enable-intellisense-support)
- [FAQ](#faq)
- [Project maintenance](#project-maintenance)

# Quickstart

Install the .NET 10 SDK selected by [`global.json`](global.json), then add the independently
maintained package:

```shell
dotnet add package Dijgrid.RazorLight --version 3.0.0
```

Do not use the historical `RazorLight 2.3.1` package as evidence of this continuation's framework
or dependency baseline.

The simplest scenario creates a template from a string. Each template has a `templateKey`, allowing
RazorLight to cache and reuse its compiled form. String templates do not require a project; layouts,
includes, and project-based template lookup do.

snippet: simple

To render a compiled template:

snippet: RenderCompiledTemplate

# Compatibility and support

- Maintained source, tools, tests, and samples target .NET 10 only.
- Moving from the historical `2.3.1` package is a framework-breaking migration; read
  [`docs/framework-support.md`](docs/framework-support.md) before upgrading.
- The public API and historical behavior baseline are recorded in
  [`docs/compatibility-baseline.md`](docs/compatibility-baseline.md).
- Version 3 publishes nullable reference annotations; see the
  [nullable migration guide](docs/nullability.md) for newly diagnosed call sites.
- Current model, import, LINQ, and template-cache behavior is recorded in the
  [template language compatibility matrix](docs/template-language-compatibility.md).
- Generated template code uses the .NET 10 C# 14 language baseline. The
  [Razor compiler integration decision](docs/razor-compiler-integration.md) documents the retained
  runtime parser boundary, supported alternatives, and replacement criteria.
- Runtime compilation is tested in framework-dependent, self-contained, and extraction-based
  single-file deployments on Windows and Linux. Trimming and Native AOT are rejected with analyzer
  diagnostics; see the [deployment compatibility matrix](docs/deployment.md).
- Azure Functions v4 is build-validated by the maintained sample. AWS Lambda and other hosting
  environments are not part of CI and should be treated as community-supported until a focused
  integration fixture is added.
- For support and security reporting, follow [`SUPPORT.md`](SUPPORT.md) and
  [`SECURITY.md`](SECURITY.md).

# Models, imports, and LINQ

An explicit `@model` directive is the clearest way to make a template strongly typed. When a
template cannot declare `@model`, use the overload that accepts a model `Type`:

snippet: TypedModelString

The generic render overload intentionally keeps the historical dynamic model when `@model` is
absent. Simple member access works dynamically, but C# cannot bind lambda expressions such as
`item => item.Active` to a dynamically dispatched LINQ call. For `Where`, `Select`, and similar
methods, declare `@model` or use the explicit model-type overload; adding `System.Linq` alone does
not make a dynamic receiver strongly typed.

String-template cache identity includes the content, explicit model type, and configured imports.
Reusing a key with changed input replaces the active compiled template instead of silently running
the first version. Keys should still identify one logical template; the complete two-layer cache
contract is documented in [caching and invalidation](docs/caching.md).

# Template sources

RazorLight has built-in providers for file-system and embedded-resource templates. Implement
`RazorLightProject` to load templates from another source, such as a database.

String, file, embedded-resource, and custom-project templates all receive RazorLight's built-in
imports, including `System.Linq`, plus namespaces configured with `AddDefaultNamespaces`. See the
[template language compatibility policy](docs/template-language-compatibility.md) for the tested
behavior.

## File source

For a file-system project, the template key is a path relative to the root directory passed to
`RazorLightEngineBuilder`.

snippet: FileSource

## Embedded-resource source

For an embedded resource, the template key combines the resource namespace and template file name.

The examples below use this project structure:

```text
Project/
  Model.cs
  Program.cs
  Project.csproj
Project.Core/
  EmailTemplates/
    Body.cshtml
  Project.Core.csproj
  SomeService.cs
```

snippet: EmbeddedResourceSource

Setting the root namespace lets you omit that prefix from the template key:

snippet: EmbeddedResourceSourceWithRootNamespace

## Custom source

To store templates in a database or another custom location, implement `RazorLightProject`. The
project resolves template content and imports, and RazorLight also uses it to find layouts and
included templates.

```csharp
var project = new EntityFrameworkRazorProject(new AppDbContext());
var engine = new RazorLightEngineBuilder()
    .UseProject(project)
    .UseMemoryCachingProvider()
    .Build();

// For key as a GUID
string guidResult = await engine.CompileRenderAsync(
    "6cc277d5-253e-48e0-8a9a-8fe3cae17e5b",
    new { Name = "John Doe" });

// Or integer
int templateKey = 322;
string integerResult = await engine.CompileRenderAsync(
    templateKey.ToString(),
    new { Name = "John Doe" });
```

See the [custom project sample](samples/RazorLight.Samples) for a complete implementation.

# C# source composition

Templates can compose trusted helper code from ordinary `.cs` compilation units. This keeps shared
logic in C# types instead of requiring Razor inheritance or executable Razor modules:

```csharp
var engine = new RazorLightEngineBuilder()
    .UseFileSystemProject(templateRoot)
    .AddCSharpSource("Shared/ScenarioFunctions.cs")
    .Build();
```

`AddCSharpSource(path)` compiles a project source with every template. The two-argument overload
registers source text in memory and also adds it globally. A template can instead select project
source relative to its own path:

```razor
@compileSource "../Shared/ScenarioFunctions.cs"
@using ScenarioTemplates
@ScenarioFunctions.Quote(Model.Name)
```

Each source is an ordinary C# 14 file and becomes a separate syntax tree in the consuming
template's generated assembly. Prefer `internal` helper types. The same file is compiled separately
for each template assembly, so its types do not have cross-template identity and should not be used
as models or values passed across the host boundary. Source files must end in `.cs`, cannot escape
the project root, and are never rendered as templates. File-project change tokens invalidate every
dependent template when a helper changes.

Imported code has the same full process privileges as Razor code. Only compose application-owned or
otherwise trusted source; see [template security](docs/template-security.md).

# Includes and partial templates

Includes let templates share smaller, reusable components. They reduce duplication and keep complex
templates manageable.

**Includes require a RazorLight project** so the engine can locate the referenced template.

```csharp
@model MyProject.TestViewModel
<div>
    Hello @Model.Title
</div>

@{ await IncludeAsync("SomeView.cshtml", Model); }
```

The first argument is the template key; the second is the model passed to the included template and
may be `null`.

# Caching and invalidation

RazorLight maintains an internal compilation cache and a configured page-factory cache. They are
coordinated through `engine.Handler.Cache`: removing or replacing a logical template key invalidates
both layers, and project change tokens also expire layouts and includes under their own keys.

File-style keys normalize slash direction but remain case-sensitive on every operating system.
Custom cache providers must support concurrent retrieval, insertion, replacement, and removal. See
the [caching and invalidation contract](docs/caching.md) for string-template identity, custom-project
change tokens, precompiled-provider behavior, and process-local limitations.

# Encoding

The core package writes model expressions as plain text. No escaping policy is universally correct
for HTML, XML, JSON, source code, shell scripts, or other generated formats, so format-specific
escaping is explicit.

```csharp
string template = "Render @Model.Value";
string result = await engine.CompileRenderStringAsync(
    "plain-text",
    template,
    new { Value = "<text>&" });

Console.WriteLine(result); // Output: Render <text>&
```

Install the optional HTML package and opt in when the destination is HTML:

```shell
dotnet add package Dijgrid.RazorLight.Html --version 3.0.0
```

```csharp
using RazorLight.Html;

var htmlEngine = new RazorLightEngineBuilder()
    .UseNoProject()
    .UseHtmlEncoding()
    .Build();

string encodedResult = await htmlEngine.CompileRenderStringAsync(
    "html",
    "Render @Model.Tag",
    new { Tag = "<html>&" });

Console.WriteLine(encodedResult); // Output: &lt;html&gt;&amp;
```

The optional package supplies output encoding only. The next-major generic core does not expose MVC
`IHtmlContent` contracts or tag-helper execution; templates using those APIs must migrate to ordinary
Razor expressions, includes, helpers, or another ASP.NET-integrated rendering engine.

Custom formats can implement `IOutputEncoder` and register it with `UseOutputEncoder`. Template
literals are never transformed. `Raw` produces final `ITemplateContent` and bypasses the configured
encoder for a specific expression:

```csharp
string rawTemplate = "Render @Raw(Model.Tag)";
string rawResult = await htmlEngine.CompileRenderStringAsync(
    "raw",
    rawTemplate,
    new { Tag = "<html>&" });

Console.WriteLine(rawResult); // Output: <html>&
```

# Template security

Razor templates compile to .NET assemblies and execute with the host process's permissions. Treat
every in-process template as trusted code. Output encoding can protect a selected destination format;
it does not prevent a template from executing C#, accessing files or the network, using reflection,
or resolving host services through `@inject`.

User-authored or otherwise untrusted templates require a separately secured renderer process or
service. Metadata reference filtering and limited dependency injection are useful least-privilege
controls for trusted templates, but they are not a sandbox. See the complete
[template security and trust-boundary guide](docs/template-security.md).

# Enable IntelliSense support

Visual Studio assumes a Razor file is an ASP.NET MVC view. Add an explicit base class to help
IntelliSense understand a RazorLight template:

```csharp
@using RazorLight
@inherits TemplatePage<MyModel>

<html>
    Your awesome template goes here, @Model.Name
</html>
```

---

![Intellisense](github/autocomplete.png)

# FAQ

## Coding Challenges (FAQ)

### How to use templates from memory without setting a project?

String templates work without configuring a project. The builder supplies `NoRazorProject` by
default, and the memory cache can store the compiled template:

```csharp
var razorEngine = new RazorLightEngineBuilder()
                .UseMemoryCachingProvider()
                .Build();

string html = await razorEngine.CompileRenderStringAsync(
    "welcome",
    "Hello, @Model.Name!",
    new { Name = "Ada" });
```

Configure a file, embedded-resource, or custom project when templates use layouts, includes, or
project keys. This behavior is covered by the quickstart smoke tests.

### How to embed an image in an email?

This isn't a RazorLight question, but please see
[this Stack Overflow answer](https://stackoverflow.com/a/32767496/1040437).

### How to embed CSS in an email?

This isn't a RazorLight question, but please look into PreMailer.Net.

## Compilation and Deployment Issues (FAQ)

Runtime compilation depends on metadata from the entry application. If rendering works during local
development but fails after deployment, review the following common configuration issues.

### Additional metadata references

RazorLight's default discovery includes application project assemblies, the operating assembly, and
RazorLight's runtime dependencies. It does not expose unrelated host NuGet packages automatically.
Select a dependency-context assembly by exact name with `IncludeAssemblies`, or pass a specific
metadata reference explicitly:

```csharp
var metadataReference = MetadataReference.CreateFromFile("path-to-your-assembly");

var engine = new RazorLightEngineBuilder()
    .UseMemoryCachingProvider()
    .IncludeAssemblies("Application.TemplateContracts")
    .AddMetadataReferences(metadataReference)
    .Build();
```

`ExcludeAssemblies` removes an automatically discovered exact assembly name. If migration from the
historical broad behavior is temporarily necessary, call
`UseAllDependencyContextMetadataReferences()`. Broad discovery exposes every compile-time host
dependency and should be an explicit compatibility choice. These APIs control ordinary compilation
references; they do not sandbox executable template code.

### I'm getting "Cannot find compilation library" when I deploy this library on another server

RazorLight discovers metadata from the entry-point project's dependency context. Add this property
to the entry-point project (for example, the web app, worker, or console app), not just a class
library that wraps RazorLight:

```xml
<PropertyGroup>
    <PreserveCompilationContext>true</PreserveCompilationContext>
</PropertyGroup>
```

### I'm getting "Can't load metadata reference from the entry assembly" exception

Set `PreserveCompilationContext` to `true` in the entry-point project's `.csproj` file:

```xml
<PropertyGroup>
    <PreserveCompilationContext>true</PreserveCompilationContext>
</PropertyGroup>
```

Self-contained deployment is supported with `PreserveCompilationContext`. Single-file deployment
also requires `IncludeAllContentForSelfExtract`; a non-extracting bundle hides the dependency files
needed for runtime compilation. Trimmed and Native AOT applications cannot use the runtime compiler
and receive `IL2026` or `IL3050` at the call site. See the
[deployment compatibility matrix](docs/deployment.md) for the complete configuration and supported
alternatives.

### Does RazorLight work in serverless or ASP.NET Core integration-test hosts?

The repository build-validates a .NET 10 Azure Functions v4 isolated-worker sample. AWS Lambda and
dedicated ASP.NET Core integration-test hosting are not currently exercised in CI, so they are
community-supported rather than declared broken. Windows and Linux deployment probes cover the
runtime-compilation modes listed in the [deployment matrix](docs/deployment.md). Keep template
rendering behind an application service when you need to substitute it in broader host tests, and
open a reproducible issue for host-specific failures.

# Project maintenance

- Read [CONTRIBUTING.md](CONTRIBUTING.md) before proposing or implementing changes.
- Report vulnerabilities privately according to [SECURITY.md](SECURITY.md).
- Use [SUPPORT.md](SUPPORT.md) to choose the appropriate issue type and diagnostic information.
- Review [CHANGELOG.md](CHANGELOG.md) for independent-maintenance changes.
- Follow the protected [release process](docs/releasing.md) when preparing package artifacts or tags.
- Track accepted roadmap work in [`.planfs`](.planfs), with working conventions documented in
  [AGENTS.md](AGENTS.md).

`README.md` is generated from this file and the compile-checked snippets in
`tests/RazorLight.Tests/Snippets`. Regenerate it with:

```shell
dotnet build tests/RazorLight.Tests/RazorLight.Tests.csproj --configuration Release
```

Commit `README.source.md`, the snippet source, and the resulting `README.md` together. CI rebuilds
the documentation and fails if the generated file differs.
