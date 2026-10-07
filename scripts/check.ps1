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

Push-Location $root
try {
    Invoke-Step 'Restore tools' { dotnet tool restore }
    Invoke-Step 'Format' { dotnet format $solution --verify-no-changes }
    # Restore runs NuGet audit; high and critical advisories fail here (Directory.Build.props).
    Invoke-Step 'Build' { dotnet build $solution --configuration Release }
    Invoke-Step 'Test' { dotnet test --solution $solution --configuration Release --no-build }
    # Test-only packages are held to the shipped-code list too.
    Invoke-Step 'Licenses' {
        dotnet nuget-license --input $solution --include-transitive `
            --allowed-license-types (Join-Path $PSScriptRoot 'allowed-licenses.json')
    }
    Invoke-Step 'Game logs' { & (Join-Path $PSScriptRoot 'Test-NoGameLogs.ps1') }
    Invoke-Step 'UI strings' { & (Join-Path $PSScriptRoot 'Test-Content.ps1') }
    Invoke-Step 'String resources' { & (Join-Path $PSScriptRoot 'Sync-Strings.ps1') -Check }
}
finally {
    Pop-Location
}
