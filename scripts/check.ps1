#Requires -Version 7
# Runs every check that CI runs, in the same order. Usage: pwsh scripts/check.ps1
$ErrorActionPreference = 'Stop'
# The SDK and Microsoft.Testing.Platform send usage data unless told not to.
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:TESTINGPLATFORM_TELEMETRY_OPTOUT = '1'
$root = Split-Path $PSScriptRoot -Parent
$solution = Join-Path $root 'KindlyBartender.slnx'

function Invoke-Step([string]$Name, [scriptblock]$Command) {
    Write-Host "==> $Name"
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE"
    }
}

function Test-HasTestProject {
    $projects = dotnet sln $solution list | Where-Object { $_ -like '*.csproj' }
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet sln list failed with exit code $LASTEXITCODE"
    }
    foreach ($project in $projects) {
        if (Select-String -LiteralPath (Join-Path $root $project) -Pattern 'Include="xunit\.v3"' -Quiet) {
            return $true
        }
    }
    return $false
}

Push-Location $root
try {
    Invoke-Step 'Restore tools' { dotnet tool restore }
    Invoke-Step 'Format' { dotnet format $solution --verify-no-changes }
    # Restore runs NuGet audit; high and critical advisories fail here (Directory.Build.props).
    Invoke-Step 'Build' { dotnet build $solution --configuration Release }
    # Microsoft.Testing.Platform fails when a run finds no tests, so the step waits for the first test project.
    if (Test-HasTestProject) {
        Invoke-Step 'Test' { dotnet test --solution $solution --configuration Release --no-build }
    }
    # Test-only packages are held to the shipped-code list too, which is stricter than the handbook requires.
    Invoke-Step 'Licenses' {
        dotnet nuget-license --input $solution --include-transitive `
            --allowed-license-types (Join-Path $PSScriptRoot 'allowed-licenses.json')
    }
    Invoke-Step 'Game logs' { & (Join-Path $PSScriptRoot 'Test-NoGameLogs.ps1') }
}
finally {
    Pop-Location
}
