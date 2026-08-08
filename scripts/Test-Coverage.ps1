param(
    [string] $Configuration = "Release",
    [string] $OutputDirectory = "artifacts/TestResults/Coverage",
    [switch] $NoBuild
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$baselinePath = Join-Path $repositoryRoot "eng/coverage-baseline.json"
$baseline = Get-Content $baselinePath -Raw | ConvertFrom-Json
$runDirectory = Join-Path $repositoryRoot (Join-Path $OutputDirectory ([Guid]::NewGuid().ToString("N")))
New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null

$summaries = foreach ($suite in $baseline.suites) {
    $suiteDirectory = Join-Path $runDirectory $suite.name
    $arguments = @(
        "test",
        (Join-Path $repositoryRoot $suite.project),
        "--configuration", $Configuration,
        "--collect:XPlat Code Coverage;Format=cobertura",
        "--results-directory", $suiteDirectory,
        "--logger", "console;verbosity=normal"
    )
    if ($suite.framework) {
        $arguments += @("--framework", $suite.framework)
    }
    if ($NoBuild) {
        $arguments += "--no-build"
    }

    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Coverage test suite '$($suite.name)' failed with exit code $LASTEXITCODE."
    }

    $report = Get-ChildItem $suiteDirectory -Recurse -Filter coverage.cobertura.xml |
        Select-Object -First 1
    if ($null -eq $report) {
        throw "Coverage report for '$($suite.name)' was not produced."
    }

    [xml] $coverage = Get-Content $report.FullName
    $package = @($coverage.coverage.packages.package) |
        Where-Object name -EQ $suite.package |
        Select-Object -First 1
    if ($null -eq $package) {
        throw "Package '$($suite.package)' was not found in '$($report.FullName)'."
    }

    $rawLinePercent = ([double] $package.'line-rate') * 100
    $rawBranchPercent = ([double] $package.'branch-rate') * 100
    $linePercent = [Math]::Round($rawLinePercent, 2)
    $branchPercent = [Math]::Round($rawBranchPercent, 2)
    if ($rawLinePercent -lt [double] $suite.minimumLinePercent) {
        throw "$($suite.name) line coverage $linePercent% is below the $($suite.minimumLinePercent)% floor."
    }
    if ($rawBranchPercent -lt [double] $suite.minimumBranchPercent) {
        throw "$($suite.name) branch coverage $branchPercent% is below the $($suite.minimumBranchPercent)% floor."
    }

    [pscustomobject] @{
        Suite = $suite.name
        Lines = "$linePercent%"
        LineFloor = "$($suite.minimumLinePercent)%"
        Branches = "$branchPercent%"
        BranchFloor = "$($suite.minimumBranchPercent)%"
        Report = $report.FullName
    }
}

$summaries | Format-Table -AutoSize
$summaryPath = Join-Path $runDirectory "coverage-summary.json"
$summaries | ConvertTo-Json | Set-Content $summaryPath
Write-Host "Coverage floors passed. Summary: $summaryPath"
