param(
    [Parameter(Mandatory = $true)]
    [string] $PackageDirectory,

    [Parameter(Mandatory = $true)]
    [string] $Version
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression.FileSystem

$repositoryUrl = "https://github.com/dijgrid/razor-light"
$expectedPackages = @(
    @{
        Id = "Dijgrid.RazorLight"
        PrimaryEntries = @(
            "LICENSE",
            "README.md",
            "lib/net10.0/RazorLight.dll",
            "lib/net10.0/RazorLight.pdb",
            "lib/net10.0/RazorLight.xml"
        )
        SymbolEntries = @("lib/net10.0/RazorLight.pdb")
    },
    @{
        Id = "Dijgrid.RazorLight.Precompile"
        PrimaryEntries = @(
            "LICENSE",
            "README.md",
            "tools/net10.0/any/DotnetToolSettings.xml",
            "tools/net10.0/any/RazorLight.Precompile.dll",
            "tools/net10.0/any/RazorLight.Precompile.pdb"
        )
        SymbolEntries = @(
            "tools/net10.0/any/RazorLight.Precompile.pdb",
            "tools/net10.0/any/RazorLight.pdb"
        )
    }
)

function Get-ZipEntryText {
    param(
        [System.IO.Compression.ZipArchive] $Archive,
        [string] $EntryName
    )

    $entry = $Archive.GetEntry($EntryName)
    if ($null -eq $entry) {
        throw "Package entry '$EntryName' was not found."
    }

    $reader = [System.IO.StreamReader]::new($entry.Open())
    try {
        return $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }
}

function Get-NuspecValue {
    param(
        [xml] $Nuspec,
        [string] $ElementName
    )

    $node = $Nuspec.SelectSingleNode("//*[local-name()='$ElementName']")
    if ($null -eq $node) {
        throw "Nuspec element '$ElementName' was not found."
    }

    return $node.InnerText
}

function Assert-ArchiveEntries {
    param(
        [System.IO.Compression.ZipArchive] $Archive,
        [string[]] $ExpectedEntries
    )

    foreach ($entryName in $ExpectedEntries) {
        if ($null -eq $Archive.GetEntry($entryName)) {
            throw "Expected package entry '$entryName' was not found."
        }
    }
}

function Assert-Nuspec {
    param(
        [System.IO.Compression.ZipArchive] $Archive,
        [string] $PackageId,
        [string] $PackageVersion
    )

    $nuspecEntry = $Archive.Entries |
        Where-Object { $_.FullName.EndsWith(".nuspec", [StringComparison]::OrdinalIgnoreCase) } |
        Select-Object -First 1
    if ($null -eq $nuspecEntry) {
        throw "Package '$PackageId' does not contain a nuspec."
    }

    [xml] $nuspec = Get-ZipEntryText -Archive $Archive -EntryName $nuspecEntry.FullName

    if ((Get-NuspecValue -Nuspec $nuspec -ElementName "id") -ne $PackageId) {
        throw "Package ID mismatch for '$PackageId'."
    }

    if ((Get-NuspecValue -Nuspec $nuspec -ElementName "version") -ne $PackageVersion) {
        throw "Package version mismatch for '$PackageId'."
    }

    if ((Get-NuspecValue -Nuspec $nuspec -ElementName "authors") -notmatch "Dijgrid") {
        throw "Package '$PackageId' does not identify the independent maintainer."
    }

    if ((Get-NuspecValue -Nuspec $nuspec -ElementName "projectUrl") -ne $repositoryUrl) {
        throw "Package '$PackageId' has an unexpected project URL."
    }

    $repository = $nuspec.SelectSingleNode("//*[local-name()='repository']")
    if ($null -eq $repository -or $repository.url -ne $repositoryUrl) {
        throw "Package '$PackageId' has an unexpected repository URL."
    }

    if ($repository.commit -notmatch "^[0-9a-f]{40}$") {
        throw "Package '$PackageId' does not contain a full repository commit."
    }

    if ((Get-NuspecValue -Nuspec $nuspec -ElementName "license") -ne "LICENSE") {
        throw "Package '$PackageId' does not use the repository license file."
    }

    if ((Get-NuspecValue -Nuspec $nuspec -ElementName "readme") -ne "README.md") {
        throw "Package '$PackageId' does not use the repository README."
    }
}

$resolvedPackageDirectory = (Resolve-Path -LiteralPath $PackageDirectory).Path
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) (
    "razorlight-package-validation-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $temporaryRoot | Out-Null

try {
    foreach ($package in $expectedPackages) {
        $primaryPath = Join-Path $resolvedPackageDirectory "$($package.Id).$Version.nupkg"
        $symbolPath = Join-Path $resolvedPackageDirectory "$($package.Id).$Version.snupkg"

        foreach ($path in @($primaryPath, $symbolPath)) {
            if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
                throw "Expected package '$path' was not created."
            }
        }

        $primaryArchive = [System.IO.Compression.ZipFile]::OpenRead($primaryPath)
        try {
            Assert-ArchiveEntries -Archive $primaryArchive -ExpectedEntries $package.PrimaryEntries
            Assert-Nuspec -Archive $primaryArchive -PackageId $package.Id -PackageVersion $Version
        }
        finally {
            $primaryArchive.Dispose()
        }

        $symbolArchive = [System.IO.Compression.ZipFile]::OpenRead($symbolPath)
        try {
            Assert-ArchiveEntries -Archive $symbolArchive -ExpectedEntries $package.SymbolEntries

            foreach ($pdbEntryName in $package.SymbolEntries) {
                $pdbEntry = $symbolArchive.GetEntry($pdbEntryName)
                $pdbPath = Join-Path $temporaryRoot (
                    $package.Id + "-" + [System.IO.Path]::GetFileName($pdbEntryName))
                [System.IO.Compression.ZipFileExtensions]::ExtractToFile($pdbEntry, $pdbPath, $true)

                $sourceLinkJson = & dotnet sourcelink print-json $pdbPath
                if ($LASTEXITCODE -ne 0) {
                    throw "Source Link metadata could not be read from '$pdbEntryName'."
                }

                $sourceLink = $sourceLinkJson | ConvertFrom-Json
                $urls = @($sourceLink.documents.PSObject.Properties.Value)
                $invalidUrls = @($urls | Where-Object {
                    $_ -notmatch "^https://raw[.]githubusercontent[.]com/dijgrid/razor-light/[0-9a-f]{40}/[*]$"
                })
                if ($urls.Count -eq 0 -or $invalidUrls.Count -gt 0) {
                    throw "Source Link metadata in '$pdbEntryName' does not target an immutable repository commit."
                }
            }
        }
        finally {
            $symbolArchive.Dispose()
        }

        Write-Host "Validated package and symbols: $($package.Id) $Version"
    }
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}
