param(
    [Parameter(Mandatory = $true)]
    [string] $PackageDirectory,

    [Parameter(Mandatory = $true)]
    [string] $Version
)

$ErrorActionPreference = "Stop"
$resolvedPackageDirectory = (Resolve-Path -LiteralPath $PackageDirectory).Path
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) (
    "razorlight-package-consumer-" + [Guid]::NewGuid().ToString("N"))
$consumerRoot = Join-Path $temporaryRoot "consumer"
$toolRoot = Join-Path $temporaryRoot "tool"

try {
    New-Item -ItemType Directory -Path $consumerRoot -Force | Out-Null
    & dotnet new console --framework net10.0 --output $consumerRoot --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Could not create the clean consumer project." }

    $project = Join-Path $consumerRoot "consumer.csproj"
    [xml] $projectXml = Get-Content $project
    $propertyGroup = $projectXml.Project.PropertyGroup | Select-Object -First 1
    $preserveCompilationContext = $projectXml.CreateElement("PreserveCompilationContext")
    $preserveCompilationContext.InnerText = "true"
    $propertyGroup.AppendChild($preserveCompilationContext) | Out-Null
    $projectXml.Save($project)

    & dotnet add $project package Dijgrid.RazorLight --version $Version --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Could not add Dijgrid.RazorLight $Version." }
    & dotnet add $project package Dijgrid.RazorLight.Html --version $Version --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Could not add Dijgrid.RazorLight.Html $Version." }

    @'
using RazorLight;
using RazorLight.Html;

using IRazorLightEngine textEngine = new RazorLightEngineBuilder()
    .UseNoProject()
    .UseMemoryCachingProvider()
    .Build();
string plain = await textEngine.CompileRenderStringAsync("plain", "@Model", "<strong>text</strong>");

using IRazorLightEngine htmlEngine = new RazorLightEngineBuilder()
    .UseNoProject()
    .UseMemoryCachingProvider()
    .UseHtmlEncoding()
    .Build();
string encoded = await htmlEngine.CompileRenderStringAsync("html", "@Model", "<strong>text</strong>");

if (plain != "<strong>text</strong>" || encoded != "&lt;strong&gt;text&lt;/strong&gt;")
{
    throw new InvalidOperationException($"Unexpected package output: plain='{plain}', html='{encoded}'.");
}

Console.WriteLine("RazorLight package consumer smoke test passed.");
'@ | Set-Content (Join-Path $consumerRoot "Program.cs")

    & dotnet restore $project --source $resolvedPackageDirectory --source "https://api.nuget.org/v3/index.json"
    if ($LASTEXITCODE -ne 0) { throw "Could not restore the clean consumer project." }
    & dotnet run --project $project --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "The clean consumer smoke test failed." }

    & dotnet tool install Dijgrid.RazorLight.Precompile `
        --tool-path $toolRoot `
        --version $Version `
        --add-source $resolvedPackageDirectory
    if ($LASTEXITCODE -ne 0) { throw "Could not install Dijgrid.RazorLight.Precompile $Version." }

    $toolName = if ($IsWindows) { "razorlight-precompile.exe" } else { "razorlight-precompile" }
    & (Join-Path $toolRoot $toolName) --help
    if ($LASTEXITCODE -ne 0) { throw "The installed precompile tool did not start successfully." }
    Write-Host "Validated clean consumer and tool installation for RazorLight $Version."
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}
