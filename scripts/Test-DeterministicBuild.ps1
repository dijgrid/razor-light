param(
    [string] $Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$outputs = @(
    "src/RazorLight/bin/$Configuration/net10.0/RazorLight.dll",
    "src/RazorLight/bin/$Configuration/net10.0/RazorLight.pdb",
    "src/RazorLight.Precompile/bin/$Configuration/net10.0/RazorLight.Precompile.dll",
    "src/RazorLight.Precompile/bin/$Configuration/net10.0/RazorLight.Precompile.pdb"
)

function Invoke-CleanBuild {
    & dotnet build RazorLight.sln `
        --configuration $Configuration `
        --no-restore `
        --no-incremental `
        --warnaserror `
        -p:ContinuousIntegrationBuild=true

    if ($LASTEXITCODE -ne 0) {
        throw "The deterministic-build verification build failed."
    }
}

function Get-OutputHashes {
    $hashes = @{}
    foreach ($output in $outputs) {
        if (-not (Test-Path -LiteralPath $output -PathType Leaf)) {
            throw "Expected deterministic-build output '$output' was not created."
        }

        $hashes[$output] = (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash
    }

    return $hashes
}

Invoke-CleanBuild
$firstBuild = Get-OutputHashes

Invoke-CleanBuild
$secondBuild = Get-OutputHashes

foreach ($output in $outputs) {
    if ($firstBuild[$output] -ne $secondBuild[$output]) {
        throw "Deterministic-build verification failed for '$output'."
    }

    Write-Host "Verified deterministic output: $output"
}
