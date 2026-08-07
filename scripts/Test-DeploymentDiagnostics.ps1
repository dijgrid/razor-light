param(
    [string] $Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$libraryProject = Join-Path $repositoryRoot "src/RazorLight/RazorLight.csproj"
$probeProject = Join-Path $repositoryRoot "tests/RazorLight.DeploymentProbe/RazorLight.DeploymentProbe.csproj"
$documentedTrimWarnings = @(
    "IL2026",
    "IL2055",
    "IL2060",
    "IL2067",
    "IL2070",
    "IL2072",
    "IL2075"
)

$analysisOutput = & dotnet build $libraryProject `
    --configuration $Configuration `
    --target Rebuild `
    -p:IsTrimmable=true `
    -v:minimal 2>&1
if ($LASTEXITCODE -ne 0) {
    $analysisOutput
    throw "RazorLight trim analysis failed to complete."
}

$observedTrimWarnings = [regex]::Matches(
    ($analysisOutput -join "`n"),
    "warning (IL[0-9]{4})") |
    ForEach-Object { $_.Groups[1].Value } |
    Sort-Object -Unique
$unexpectedTrimWarnings = $observedTrimWarnings |
    Where-Object { $_ -notin $documentedTrimWarnings }
if ($unexpectedTrimWarnings) {
    $analysisOutput
    throw "Undocumented RazorLight trim diagnostics: $($unexpectedTrimWarnings -join ', ')"
}

function Assert-ConsumerDiagnostic {
    param(
        [string] $Property,
        [string] $Diagnostic,
        [string] $Message
    )

    $output = & dotnet build $probeProject `
        --configuration $Configuration `
        --target Rebuild `
        "-p:$Property=true" `
        -p:TreatWarningsAsErrors=true `
        -v:minimal 2>&1
    $exitCode = $LASTEXITCODE
    $text = $output -join "`n"

    if ($exitCode -eq 0) {
        throw "The $Property consumer build unexpectedly succeeded."
    }

    if ($text -notmatch $Diagnostic -or $text -notmatch [regex]::Escape($Message)) {
        $output
        throw "The $Property consumer build did not report the expected $Diagnostic RazorLight diagnostic."
    }

    $output | Where-Object { $_ -match $Diagnostic } | Select-Object -First 1
    $global:LASTEXITCODE = 0
}

Assert-ConsumerDiagnostic `
    -Property "PublishTrimmed" `
    -Diagnostic "IL2026" `
    -Message "Runtime Razor compilation discovers and loads assemblies dynamically"
Assert-ConsumerDiagnostic `
    -Property "PublishAot" `
    -Diagnostic "IL3050" `
    -Message "Runtime Razor compilation requires dynamic code generation"

Write-Host "RazorLight deployment diagnostics are present and the trim-warning inventory is unchanged."
