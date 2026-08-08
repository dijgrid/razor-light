# RazorLight manual

This manual covers the practical use of the independently maintained `Dijgrid.RazorLight` 3.x
package. For a short project overview, start with the [README](../README.md).

## Contents

- [Installation](#installation)
- [Rendering a string template](#rendering-a-string-template)
- [Models, imports, and LINQ](#models-imports-and-linq)
- [Template projects and sources](#template-projects-and-sources)
- [Layouts, sections, and includes](#layouts-sections-and-includes)
- [Composing templates with C# source](#composing-templates-with-c-source)
- [Caching and invalidation](#caching-and-invalidation)
- [Cancellation](#cancellation)
- [Output encoding](#output-encoding)
- [Compilation references](#compilation-references)
- [Precompilation](#precompilation)
- [Deployment](#deployment)
- [IntelliSense](#intellisense)
- [Troubleshooting](#troubleshooting)

## Installation

Install the .NET 10 SDK selected by [`global.json`](../global.json), then add the core package:

```shell
dotnet add package Dijgrid.RazorLight --version 3.0.0-beta.1
```

The `RazorLight 2.3.1` package is the historical upstream build. The independently maintained 3.x
line uses the `Dijgrid.RazorLight` package identity, targets .NET 10, publishes nullable reference
annotations, and intentionally changes parts of the inherited API and behavior.

Runtime compilation reads metadata from the entry application's dependency context. Add this to the
entry-point project when its SDK does not already preserve compilation metadata:

```xml
<PropertyGroup>
  <PreserveCompilationContext>true</PreserveCompilationContext>
</PropertyGroup>
```

## Rendering a string template

A string template needs a key, content, and model. The key identifies one logical template for
caching and invalidation:

```csharp
await using IRazorLightEngine engine = new RazorLightEngineBuilder()
    .UseMemoryCachingProvider()
    .Build();

string template = "Hello, @Model.Name!";
string result = await engine.CompileRenderStringAsync(
    "welcome",
    template,
    new { Name = "Ada" });
```

String templates do not require a project. Configure a project when a template needs file or
resource lookup, layouts, includes, or project-backed C# sources.

Reusing a string-template key with different content, model type, or configured imports replaces
the prior compiled variant. Keys should nevertheless remain stable and describe one logical
template.

`ITemplatePage` instances are mutable and single-use. Compile and render one directly when only one
render is needed:

```csharp
var page = await engine.CompileTemplateAsync("welcome");
string rendered = await engine.RenderTemplateAsync(page, new { Name = "Ada" });
```

For compile-once/render-many use, create a reusable handle. It reuses the compiled descriptor while
creating a fresh page for every render, including concurrent renders:

```csharp
RazorLightTemplate template = await engine.CompileReusableTemplateAsync("welcome");
string first = await template.RenderAsync(new { Name = "Ada" });
string second = await template.RenderAsync(new { Name = "Grace" });
```

Rendering the same page twice—or concurrently—throws `InvalidOperationException` rather than
leaking layout, section, writer, or model state from one render into another.

## Models, imports, and LINQ

Templates without an explicit model type retain RazorLight's dynamic model behavior:

```razor
Hello, @Model.Name!
```

Use `@model` when a template owns its model declaration:

```razor
@model MyApplication.ReportModel
@Model.Items.Count active items
```

When the template cannot declare `@model`, pass the model's runtime type explicitly:

```csharp
const string template =
    "@(Model.Items.Where(item => item.Length > 3).Select(item => item.ToUpperInvariant()).FirstOrDefault())";
object model = new ReportModel { Items = new[] { "one", "three" } };

string result = await engine.CompileRenderStringAsync(
    "typed-linq",
    template,
    model,
    typeof(ReportModel));
```

The explicit type is important for lambda expressions. C# cannot bind a lambda such as
`item => item.Active` to a dynamically dispatched LINQ call. Adding `System.Linq` alone does not
make a dynamic receiver strongly typed.

Add namespaces to every generated template through the builder:

```csharp
var engine = new RazorLightEngineBuilder()
    .AddDefaultNamespaces("MyApplication.Models", "MyApplication.TemplateHelpers")
    .Build();
```

String, file, embedded-resource, and custom-project templates all receive RazorLight's built-in
imports and configured default namespaces.

## Template projects and sources

A `RazorLightProject` resolves logical template keys. RazorLight includes file-system and embedded-
resource implementations, and applications can provide their own implementation for databases,
object storage, or other sources.

### File-system templates

Configure a directory as the project root and render a path relative to it:

```csharp
var engine = new RazorLightEngineBuilder()
    .UseFileSystemProject("C:/Templates")
    .UseMemoryCachingProvider()
    .Build();

string result = await engine.CompileRenderAsync(
    "Reports/Summary.cshtml",
    new { Title = "Quarterly summary" });
```

The default template extension is `.cshtml`, so it may be omitted from a logical key. File-style
keys normalize slash direction but remain case-sensitive across operating systems.

### Embedded-resource templates

Mark templates as embedded resources in the project file:

```xml
<ItemGroup>
  <EmbeddedResource Include="EmailTemplates\**\*.cshtml" />
</ItemGroup>
```

Then select the assembly and, optionally, the resource root namespace:

```csharp
var engine = new RazorLightEngineBuilder()
    .UseEmbeddedResourcesProject(
        typeof(SomeService).Assembly,
        "Project.Core.EmailTemplates")
    .UseMemoryCachingProvider()
    .Build();

string result = await engine.CompileRenderAsync("Body", new MessageModel());
```

Without an explicit root namespace, the template key includes the resource namespace and file name.

### Custom projects

Derive from `RazorLightProject` to load templates from another store:

```csharp
public sealed class DatabaseTemplateProject : RazorLightProject
{
    public override Task<RazorLightProjectItem> GetItemAsync(string templateKey)
    {
        // Return an item that exposes Exists, Key, Read(), and an optional change token.
        throw new NotImplementedException();
    }

    public override Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(string templateKey) =>
        Task.FromResult(Enumerable.Empty<RazorLightProjectItem>());
}
```

Configure the project normally:

```csharp
var engine = new RazorLightEngineBuilder()
    .UseProject(new DatabaseTemplateProject())
    .UseMemoryCachingProvider()
    .Build();

string result = await engine.CompileRenderAsync(
    "6cc277d5-253e-48e0-8a9a-8fe3cae17e5b",
    new { Name = "Ada" });
```

Override `GetKnownKeysAsync` to improve detailed missing-template diagnostics. Override
`GetSourceItemAsync` when the project also supplies exact `.cs` files for source composition.

## Layouts, sections, and includes

Layouts and includes require a configured project because RazorLight must resolve another logical
template key.

A template can render an included template with a separate model:

```razor
@model MyApplication.ReportModel

Report: @Model.Title
@{ await IncludeAsync("Shared/Footer.cshtml", Model.Footer); }
```

The include model may be `null`. Includes are rendered templates; use C# source composition instead
when the goal is to share helper algorithms or domain-specific functions.

A layout template renders the calling template's body and optional sections using standard Razor
layout syntax. Keep all related templates within the same project so lookup and invalidation remain
coherent.

## Composing templates with C# source

RazorLight can compile trusted ordinary `.cs` files into a consuming template's generated assembly.
This supports composition without requiring Razor inheritance or Razor files that exist only to
hold `@functions` blocks.

### Global project source

Compile one project source with every template created by an engine:

```csharp
var engine = new RazorLightEngineBuilder()
    .UseFileSystemProject("C:/Templates")
    .AddCSharpSource("Shared/ScenarioFunctions.cs")
    .Build();
```

### Global in-memory source

Register source text under a logical `.cs` key and add it globally:

```csharp
var engine = new RazorLightEngineBuilder()
    .AddCSharpSource(
        "Shared/Words.cs",
        "namespace Templates; internal static class Words " +
        "{ internal static string Upper(string value) => value.ToUpperInvariant(); }")
    .Build();
```

### Per-template source

Use `@compileSource` to select source for one template. Relative paths start from the consuming
template's directory; a leading slash starts from the configured project root:

```razor
@compileSource "../Shared/ScenarioFunctions.cs"
@using ScenarioTemplates
@ScenarioFunctions.Quote(Model.Name)
```

Multiple directives are allowed, and duplicate normalized paths are compiled once. Sources must
end in `.cs` and cannot traverse outside a file-system project root.

File template keys are logical project paths. A leading slash means “from the project root,” including
on Unix where the same spelling resembles an operating-system absolute path; it never grants access
outside the configured project. C# source keys are likewise logical paths and reject fully qualified
operating-system paths explicitly.

Imported sources are normal C# 14 compilation units and receive standard mapped compiler
diagnostics. Top-level executable statements are rejected. Prefer `internal` helper types: the same
source is compiled separately into each consuming template assembly, so its types do not have
cross-template identity and should not cross the host/template boundary.

Project change tokens from every imported source are combined with the template's token. Changing a
shared file therefore invalidates all dependent compiler and page-factory cache entries.

Imported source is executable trusted code with the host process's permissions. Read the
[template security guide](template-security.md) before accepting templates or helper code from
outside the application trust boundary.

## Caching and invalidation

RazorLight maintains an internal compiler cache and can use a configured page-factory cache:

```csharp
var engine = new RazorLightEngineBuilder()
    .UseFileSystemProject("C:/Templates")
    .UseMemoryCachingProvider()
    .Build();
```

The caches share an invalidation contract. Explicit removal or replacement, project change tokens,
and failed-compilation retries cannot permanently retain a stale compiled template. Project-backed
layouts, includes, and imported C# sources contribute dependency change tokens.

Applications can inspect and invalidate logical templates without accessing provider internals:

```csharp
if (engine.IsTemplateCached("Reports/Summary.cshtml"))
{
    engine.InvalidateTemplate("Reports/Summary.cshtml");
}
```

Invalidation clears both compilation and page-factory entries. When caching is disabled,
`IsTemplateCached` returns `false` and `InvalidateTemplate` is a safe no-op. Custom caching providers
must support concurrent retrieval, insertion, replacement, and removal. See the complete
[caching contract](caching.md) for cache identities, precompiled providers, and process-local
limitations.

## Cancellation

All engine operations have `CancellationToken` overloads. Overloads without a token remain
available and behave as if `CancellationToken.None` was supplied:

```csharp
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

string result = await engine.CompileRenderAsync(
    "Reports/Summary.cshtml",
    model,
    timeout.Token);
```

The active render token is available inside a template as `CancellationToken`, so template-owned
asynchronous work can cooperate directly:

```razor
@{
    ReportData data = await repository.LoadAsync(Model.Id, CancellationToken);
}
```

The same token flows through layouts and `IncludeAsync`. Cancelling a wait for a shared compilation
does not cancel compilation still needed by another caller. See the full
[cancellation contract](cancellation.md) for project extensions, cache behavior, and synchronous
operation limitations.

## Dependency injection and page initialization

Engines resolved from Microsoft.Extensions.DependencyInjection support Razor's `@inject` directive:

```csharp
services.AddScoped<ReportClock>();
services.AddRazorLight()
    .UseFileSystemProject("C:/Templates")
    .SetOperatingAssembly(typeof(Program).Assembly);

IRazorLightEngine engine = services.BuildServiceProvider()
    .GetRequiredService<IRazorLightEngine>();
```

```razor
@inject MyApp.ReportClock Clock
Generated at @Clock.UtcNow
```

Each top-level render creates and disposes one service scope. The entry page, layouts, sections, and
includes share that scope, so scoped services retain one identity throughout a composed render.
Builder-created engines do not imply a service provider and leave `@inject` properties unset.

The engine implements `IDisposable` and `IAsyncDisposable`. Dispose manually built engines (normally
with `using` or `await using`) to release compiler caches, builder-created caching providers, and
file-system watchers. A builder owns projects and caches it creates through `UseNoProject`,
`UseFileSystemProject`, `UseEmbeddedResourcesProject`, or `UseMemoryCachingProvider`; objects passed
to `UseProject` or `UseCachingProvider` remain caller-owned. The dependency-injection container
disposes its singleton engine and the services it owns.

Code that needs to initialize every page without a service provider can register an initializer at
construction time. It runs exactly once for each page instance, including layouts and includes:

```csharp
var engine = new RazorLightEngineBuilder()
    .UseFileSystemProject("C:/Templates")
    .AddPageInitializer(page => InitializeTemplatePage(page))
    .Build();
```

RazorLight's dependency injection is standalone runtime service resolution. It does not provide MVC
request services, controllers, `ViewContext`, URL helpers, tag helpers, or other MVC activation.

## ViewBag behavior

Pass an `ExpandoObject` to a render overload when a template needs supplemental dynamic values.
Missing top-level members return `null`, so normal fallback and null-conditional expressions work:

```razor
@(ViewBag.OptionalTitle ?? "Untitled")
@(ViewBag.Missing?.Nested ?? "Fallback")
```

Existing nested objects retain their own dynamic behavior. Invalid method calls, conversions, and
index operations still throw a `RuntimeBinderException`; RazorLight does not hide programming
errors merely because missing member reads are null-tolerant.

## Output encoding

The core engine writes expression values as plain text. This is the correct default for generic
text, code, JSON assembled with explicit serializers, configuration, prompts, and other non-HTML
formats:

```csharp
string result = await engine.CompileRenderStringAsync(
    "plain-text",
    "Render @Model.Value",
    new { Value = "<text>&" });

// Render <text>&
```

### Optional HTML encoding

Install the optional package:

```shell
dotnet add package Dijgrid.RazorLight.Html --version 3.0.0-beta.1
```

Then opt in explicitly:

```csharp
using RazorLight.Html;

var htmlEngine = new RazorLightEngineBuilder()
    .UseNoProject()
    .UseHtmlEncoding()
    .Build();

string result = await htmlEngine.CompileRenderStringAsync(
    "html",
    "Render @Model.Tag",
    new { Tag = "<html>&" });

// Render &lt;html&gt;&amp;
```

The optional package supplies expression encoding, not MVC integration, tag helpers, or an HTML
sandbox. Use `Raw(value)` only when a specific trusted value must bypass the configured encoder.

Implement `IOutputEncoder` and register it with `UseOutputEncoder` for another output policy.
Template literals are never transformed.

## Compilation references

Default metadata discovery exposes application project assemblies, the operating assembly, and
RazorLight's required runtime closure. Unrelated host packages are not automatically visible to
templates.

Select dependency-context assemblies by exact name or add explicit Roslyn references:

```csharp
var reference = MetadataReference.CreateFromFile("path-to-your-assembly.dll");

var engine = new RazorLightEngineBuilder()
    .IncludeAssemblies("Application.TemplateContracts")
    .ExcludeAssemblies("Application.Internal.Data")
    .AddMetadataReferences(reference)
    .Build();
```

`UseAllDependencyContextMetadataReferences()` restores broad historical discovery when temporarily
needed for migration. Reference filtering controls compile-time convenience and dependency
isolation; it is not a security sandbox.

## Precompilation

Install the tool package using the version aligned with the core library:

```shell
dotnet tool install --global Dijgrid.RazorLight.Precompile --version 3.0.0-beta.1
```

Precompile a template:

```shell
razorlight-precompile precompile --template C:/Templates/Report.cshtml --base C:/Templates
```

The precompile path uses the same template generation and C# source composition pipeline as runtime
compilation. A template's `@compileSource` dependencies are compiled into its output assembly and are
not emitted as standalone template artifacts.

The default `FileHash` strategy uses a streamed SHA-256 identity. It includes the template key,
relevant Razor and C# source files under the project root, and compiler, runtime, reference, and
supported precompile-configuration markers. Missing source files are cache misses, including after
the original template has been deleted, and stale artifacts can still be removed by template key.
The compiler emits deterministic assemblies, so identical inputs produce identical cache paths and
assembly bytes.

`PrecompiledCachingProvider` exposes an immutable discovery map and diagnostics for assemblies that
could not be inspected or did not contain RazorLight metadata. Its lookup follows the normal cache
contract and returns `false` on a miss. The `render` command remains intentionally strict: every
template and include must have a matching precompiled assembly.

For the supported build/deploy workflow, artifact compatibility checks, and the runtime entry point
that constructs no compiler graph, see [Precompiled-only execution](precompiled-only.md).

Run `razorlight-precompile help` for the current command syntax. The tool also supports cache
directory and caching-strategy selection plus rendering with a JSON model.

## Deployment

Runtime compilation requires dynamic code and the metadata files represented by the entry
application's dependency context.

- Framework-dependent deployments are supported.
- Self-contained deployments are supported when compilation context is preserved.
- Single-file deployments require extraction with `IncludeAllContentForSelfExtract` so dependency
  files remain available to the compiler.
- Trimming and Native AOT cannot use runtime template compilation and produce `IL2026` or `IL3050`
  diagnostics at affected call sites.

See the [deployment compatibility matrix](deployment.md) for complete project properties, tested
Windows and Linux modes, Azure Functions coverage, and precompiled alternatives.

## IntelliSense

Editors commonly assume a Razor file is an ASP.NET MVC view. An explicit base class helps the editor
understand a RazorLight template:

```razor
@using RazorLight
@inherits TemplatePage<MyModel>

Generated text for @Model.Name
```

Editor support does not change runtime compilation or add MVC features.

## Troubleshooting

### A template works locally but cannot find compilation libraries after deployment

Set `PreserveCompilationContext` in the entry-point project, not only in a class library that wraps
RazorLight:

```xml
<PropertyGroup>
  <PreserveCompilationContext>true</PreserveCompilationContext>
</PropertyGroup>
```

For single-file publication, also use extraction as described in the
[deployment guide](deployment.md).

### A LINQ lambda fails against `Model`

The model is dynamic when no type is declared. Add `@model` or call the render overload that accepts
an explicit `Type`. Adding a namespace import does not make a dynamic receiver strongly typed.

### A template or C# source cannot be found

Enable detailed development diagnostics with `EnableDebugMode()`. For custom projects, implement
the appropriate exact lookup method and return `Exists = true` only for real content. Do not expose
detailed diagnostics to untrusted users because paths, keys, source-derived messages, and known-key
inventories may be sensitive.

### Can RazorLight safely execute user-authored templates?

Not in the application process. Razor templates and imported `.cs` files are executable code. Use a
separately secured process or service with its own identity, restricted filesystem and network
access, bounded resources, and a narrow serialized input/output contract. See
[template security](template-security.md).

## Further reference

- [Framework support](framework-support.md)
- [Template language compatibility](template-language-compatibility.md)
- [Caching contract](caching.md)
- [Deployment compatibility](deployment.md)
- [Template security](template-security.md)
- [Dependency policy](dependency-policy.md)
- [Public API design](api-design-3.0.md)
