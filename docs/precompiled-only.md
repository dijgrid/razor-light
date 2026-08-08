# Precompiled-only execution

Precompiled-only execution renders trusted assemblies produced by the matching
`Dijgrid.RazorLight.Precompile` tool without constructing the Razor language engine, Roslyn compiler,
metadata-reference discovery, or runtime compiler cache. Missing templates and runtime source
strings fail immediately; this mode never falls back to compilation.

## Deterministic CLI build workflow

Install the tool version that exactly matches the runtime package, start from an empty artifact
directory, and precompile every deployable template with the default `FileHash` strategy:

```powershell
$templateRoot = Resolve-Path ./Templates
$artifactRoot = New-Item -ItemType Directory -Force ./artifacts/razorlight

Get-ChildItem $templateRoot -Recurse -Filter *.cshtml | ForEach-Object {
  razorlight-precompile precompile `
    --base $templateRoot `
    --template $_.FullName `
    --cache $artifactRoot `
    --strategy FileHash
}
```

Publish the resulting DLL and PDB files together. The hash identity includes the project Razor/C#
sources and supported compiler configuration, and identical inputs produce byte-identical outputs.
Always replace the artifact directory as one deployment unit; mixing outputs from separate builds can
retain stale templates.

Set the deployment project property below when that process uses precompiled-only execution
exclusively. The package's transitive build target removes the Razor language and Roslyn runtime
assets from publish output; using any runtime-compilation API in that deployment is then an explicit
configuration error.

```xml
<PropertyGroup>
  <RazorLightPrecompiledOnly>true</RazorLightPrecompiledOnly>
</PropertyGroup>
```

At startup, discover the deployed assemblies, create `PrecompiledCachingProvider`, and select the
static precompiled entry point:

```csharp
using var templates = new PrecompiledCachingProvider(
    Directory.EnumerateFiles("artifacts/razorlight", "*.dll"),
    log: null);
using var engine = RazorLightEngineBuilder.CreatePrecompiled(templates);

string result = await engine.CompileRenderAsync("Reports/Summary.cshtml", model);
```

The loader validates the normalized template key, template metadata format, compiler compatibility,
declared model contract metadata, and source checksum. Strong model assignability is checked when the
page is rendered. Legacy or incompatible artifacts produce a recompile diagnostic rather than being
loaded. PDBs retain the generated `#line` mappings back to the original template.

## Deployment support

The precompiled entry point has an executable framework-dependent and trimmed, self-contained,
single-file probe. The probe verifies its output, verifies that compiler assemblies are not loaded,
and rejects loose Roslyn or Razor-language DLLs in the publish directory. This is the supported path
for deployments that do not permit runtime compilation.

Native AOT is not currently claimed. Generated Razor pages and some optional features still use
runtime facilities that require a separate executable AOT proof before support can be advertised.
Build-time compilation remains a trusted-code operation; precompiled assemblies are executable code
and must come from the application's reviewed build pipeline.
