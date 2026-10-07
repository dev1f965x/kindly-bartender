#Requires -Version 7
# Fails when any package, direct or transitive, has a high or critical advisory.
param([Parameter(Mandatory)][string]$Solution)
$ErrorActionPreference = 'Stop'

$json = dotnet list $Solution package --vulnerable --include-transitive --format json
if ($LASTEXITCODE -ne 0) {
    throw "dotnet list package failed with exit code $LASTEXITCODE"
}

$report = $json | ConvertFrom-Json
$findings = foreach ($project in $report.projects) {
    foreach ($framework in @($project.frameworks)) {
        foreach ($package in @($framework.topLevelPackages) + @($framework.transitivePackages)) {
            foreach ($vulnerability in @($package.vulnerabilities)) {
                if ($vulnerability.severity -in 'High', 'Critical') {
                    "$($package.id) $($package.resolvedVersion): $($vulnerability.severity) $($vulnerability.advisoryurl)"
                }
            }
        }
    }
}

if ($findings) {
    $findings | Sort-Object -Unique | ForEach-Object { Write-Host $_ }
    exit 1
}
Write-Host 'No high or critical advisories.'
exit 0
