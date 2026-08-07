param(
    [string] $Configuration = "Release",
    [string] $RuntimeIdentifier
)

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repositoryRoot "tests/RazorLight.DeploymentProbe/RazorLight.DeploymentProbe.csproj"
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
    param([string[]] $Arguments)

    & dotnet publish $project --configuration $Configuration @Arguments
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

$frameworkOutput = Join-Path $outputRoot "framework-dependent"
Invoke-DotNetPublish -Arguments @(
    "--self-contained", "false",
    "--output", $frameworkOutput
)
$probeOutput = & dotnet (Join-Path $frameworkOutput "RazorLight.DeploymentProbe.dll")
Assert-ProbeOutput -Output $probeOutput

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
