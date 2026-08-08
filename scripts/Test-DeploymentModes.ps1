param(
    [string] $Configuration = "Release",
    [string] $RuntimeIdentifier
)

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repositoryRoot "tests/RazorLight.DeploymentProbe/RazorLight.DeploymentProbe.csproj"
$workerProject = Join-Path $repositoryRoot "tests/RazorLight.WorkerProbe/RazorLight.WorkerProbe.csproj"
$desktopProject = Join-Path $repositoryRoot "tests/RazorLight.DesktopProbe/RazorLight.DesktopProbe.csproj"
$outputRoot = Join-Path $repositoryRoot "artifacts/deployment"

if ([string]::IsNullOrWhiteSpace($RuntimeIdentifier)) {
    $architecture = switch ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture) {
        "X64" { "x64" }
        "Arm64" { "arm64" }
        default { throw "Unsupported deployment-probe architecture '$($_)'." }
    }

    if ($IsWindows) {
        $RuntimeIdentifier = "win-$architecture"
    }
    elseif ($IsLinux) {
        $RuntimeIdentifier = "linux-$architecture"
    }
    else {
        throw "The deployment matrix currently claims Windows and Linux support only."
    }
}

function Invoke-DotNetPublish {
    param(
        [string] $Project = $project,
        [string[]] $Arguments
    )

    & dotnet publish $Project --configuration $Configuration @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed: $($Arguments -join ' ')"
    }
}

function Assert-ProbeOutput {
    param([string[]] $Output)

    if ($LASTEXITCODE -ne 0 -or $Output -notcontains "RazorLight deployment probe passed.") {
        throw "The published RazorLight deployment probe did not complete successfully."
    }

    $Output
}

function Assert-NoAspNetCoreFramework {
    param([string] $RuntimeConfigPath)

    $runtimeConfig = Get-Content -LiteralPath $RuntimeConfigPath -Raw | ConvertFrom-Json
    $frameworkNames = @($runtimeConfig.runtimeOptions.frameworks | ForEach-Object { $_.name })
    if ($null -ne $runtimeConfig.runtimeOptions.framework) {
        $frameworkNames += $runtimeConfig.runtimeOptions.framework.name
    }

    if ($frameworkNames -contains "Microsoft.AspNetCore.App") {
        throw "The deployment probe unexpectedly requires Microsoft.AspNetCore.App: $RuntimeConfigPath"
    }
}

$frameworkOutput = Join-Path $outputRoot "framework-dependent"
Invoke-DotNetPublish -Arguments @(
    "--self-contained", "false",
    "--output", $frameworkOutput
)
$runtimeConfigPath = Join-Path $frameworkOutput "RazorLight.DeploymentProbe.runtimeconfig.json"
Assert-NoAspNetCoreFramework -RuntimeConfigPath $runtimeConfigPath
$probeOutput = & dotnet (Join-Path $frameworkOutput "RazorLight.DeploymentProbe.dll")
Assert-ProbeOutput -Output $probeOutput

$workerOutput = Join-Path $outputRoot "worker-framework-dependent"
Invoke-DotNetPublish -Project $workerProject -Arguments @(
    "--self-contained", "false",
    "--output", $workerOutput
)
Assert-NoAspNetCoreFramework -RuntimeConfigPath (Join-Path $workerOutput "RazorLight.WorkerProbe.runtimeconfig.json")
$workerProbeOutput = & dotnet (Join-Path $workerOutput "RazorLight.WorkerProbe.dll")
if ($LASTEXITCODE -ne 0 -or $workerProbeOutput -notcontains "RazorLight worker probe passed.") {
    throw "The published RazorLight worker probe did not complete successfully."
}
$workerProbeOutput

if ($IsWindows) {
    $desktopOutput = Join-Path $outputRoot "desktop-framework-dependent"
    Invoke-DotNetPublish -Project $desktopProject -Arguments @(
        "--self-contained", "false",
        "--output", $desktopOutput
    )
    Assert-NoAspNetCoreFramework -RuntimeConfigPath (Join-Path $desktopOutput "RazorLight.DesktopProbe.runtimeconfig.json")
    $desktopProbeOutput = & dotnet (Join-Path $desktopOutput "RazorLight.DesktopProbe.dll")
    if ($LASTEXITCODE -ne 0 -or $desktopProbeOutput -notcontains "RazorLight desktop probe passed.") {
        throw "The published RazorLight desktop probe did not complete successfully."
    }
    $desktopProbeOutput
}

$selfContainedOutput = Join-Path $outputRoot "self-contained/$RuntimeIdentifier"
Invoke-DotNetPublish -Arguments @(
    "--runtime", $RuntimeIdentifier,
    "--self-contained", "true",
    "--output", $selfContainedOutput
)
$executableName = if ($IsWindows) { "RazorLight.DeploymentProbe.exe" } else { "RazorLight.DeploymentProbe" }
$probeOutput = & (Join-Path $selfContainedOutput $executableName)
Assert-ProbeOutput -Output $probeOutput

$singleFileOutput = Join-Path $outputRoot "single-file/$RuntimeIdentifier"
Invoke-DotNetPublish -Arguments @(
    "--runtime", $RuntimeIdentifier,
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:IncludeAllContentForSelfExtract=true",
    "--output", $singleFileOutput
)
$probeOutput = & (Join-Path $singleFileOutput $executableName)
Assert-ProbeOutput -Output $probeOutput

Write-Host "RazorLight deployment modes passed for $RuntimeIdentifier."
