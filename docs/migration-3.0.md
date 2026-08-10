# Migrating from RazorLight 2.3.1 to 3.0

RazorLight 3.0 is an independently maintained, intentionally breaking release. It targets .NET 10,
uses new NuGet package IDs, treats Razor as a generic text-template language by default, and removes
inherited APIs that depended on obsolete ASP.NET-era contracts. Test representative templates and
deployment modes before replacing `RazorLight` 2.3.1 in production.

## Package and framework changes

Replace the historical package and target .NET 10:

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Dijgrid.RazorLight" Version="3.0.0" />
</ItemGroup>
```

The CLR namespace remains `RazorLight`; most source files therefore keep their existing `using`
directives. Applications that cannot target .NET 10 must remain on the upstream 2.3.1 line.

## Text output and optional HTML

Core expression output is now plain text. A value such as `<strong>text</strong>` is written unchanged,
which is appropriate for source code, configuration, messages, prompts, and other non-HTML output.

HTML-producing applications must add the optional package and opt in explicitly:

```xml
<PackageReference Include="Dijgrid.RazorLight.Html" Version="3.0.0" />
```

```csharp
using RazorLight.Html;

var engine = new RazorLightEngineBuilder()
    .UseFileSystemProject(templateRoot)
    .UseMemoryCachingProvider()
    .UseHtmlEncoding()
    .Build();
```

MVC tag helpers and the inherited ASP.NET HTML-content contracts were removed. RazorLight is not an
MVC view engine; replace tag-helper-dependent templates with ordinary Razor/C# output or keep that
work in ASP.NET MVC.

## Engine construction and removed APIs

Construct engines through `RazorLightEngineBuilder`. `Build()` returns `IRazorLightEngine`, and the
engine should be disposed when its lifetime ends:

```csharp
using IRazorLightEngine engine = new RazorLightEngineBuilder()
    .UseFileSystemProject(templateRoot)
    .UseMemoryCachingProvider()
    .Build();
```

The obsolete factories, error-only overloads, public handler/options graph, pre-render callback
spelling, and .NET Framework assembly-path workaround were removed. Use:

- `IsTemplateCached` and `InvalidateTemplate` for supported cache administration;
- `AddPageInitializer` instead of the old pre-render callback;
- generic render overloads or explicit model-type overloads;
- `CompileReusableTemplateAsync` for compile-once/render-many scenarios;
- structured `TemplateCompilationException.CompilationDiagnostics` for compiler failures.

Consult [the 3.0 API design record](api-design-3.0.md) for source-level replacement examples and the
machine-reviewed compatibility inventory.

## Models, templates, and caching

- Generic rendering without `@model` remains dynamic. Use `@model` or an explicit model-type overload
  when LINQ lambdas and other strongly typed expressions require compile-time type information.
- String templates now receive the same default imports and configured namespaces as project-backed
  templates.
- Reusing a string key with different content, model type, or imports replaces the cached compilation.
- Layouts, includes, imported C# sources, and project change tokens participate in invalidation.
- A raw compiled page is single-use. Use the reusable-template abstraction when rendering repeatedly
  or concurrently.

## C# source composition

Trusted ordinary `.cs` files can be compiled with templates through `AddCSharpSource` or the
`@compileSource` directive. Source keys are logical project paths and cannot escape a configured file
root. Imported code executes with the host process's permissions.

## Cancellation and dependency injection

Public asynchronous lookup, compilation, and rendering APIs have cancellation-token overloads.
Cancellation propagates through layouts and includes without disposing shared compilation work.

Dependency-injection engines create one service scope per top-level render and share it with layouts
and includes. Confirm that injected services are registered with lifetimes compatible with that
scope. Missing ViewBag members return `null`; unrelated dynamic binding failures are still reported.

## Precompilation and deployment

The tool package is now `Dijgrid.RazorLight.Precompile`. A supported precompiled-only engine can
render reviewed artifacts without constructing Roslyn or silently falling back to runtime
compilation. See [precompiled-only execution](precompiled-only.md) for the deterministic workflow.

Runtime compilation requires dynamic code and a preserved compilation context. It is unsupported in
trimmed and Native AOT deployments. The tested precompiled-only path supports trimmed,
self-contained, single-file deployment; Native AOT is not yet claimed. Review the
[deployment guide](deployment.md) before changing publish settings.

## Security and diagnostics

Templates and imported C# are executable trusted code, not sandboxed content. Isolate untrusted
authors in a separate restricted process. Production diagnostics redact template-derived paths,
messages, and missing-key inventories unless debug mode is explicitly enabled. See the
[template security guide](template-security.md).

## Recommended upgrade sequence

1. Move the application to .NET 10 while still inventorying its existing RazorLight usage.
2. Replace the package ID and compile against 3.0.0.
3. Choose plain text or add the HTML package and `UseHtmlEncoding()` explicitly.
4. Replace removed construction, cache, callback, and rendering APIs using the mappings above.
5. Validate model typing, layouts, includes, imported sources, DI lifetimes, and cache invalidation.
6. Run deployment probes for the application's actual publish mode.
7. Treat beta feedback as part of the migration and pin the exact prerelease version until a later
   beta or stable 3.0 release is deliberately adopted.
