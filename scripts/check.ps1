#Requires -Version 7
# Runs every check that CI runs, in the same order. Usage: pwsh scripts/check.ps1
$ErrorActionPreference = 'Stop'
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
    Invoke-Step 'Build' { dotnet build $solution --configuration Release }
    # Test projects are added with the first tests; until then there is nothing to run.
    if (Get-ChildItem -Path (Join-Path $root 'tests') -Filter '*.Tests.csproj' -Recurse -ErrorAction Ignore) {
        Invoke-Step 'Test' { dotnet test --solution $solution --configuration Release --no-build }
    }
    Invoke-Step 'Licenses' {
        dotnet nuget-license --input $solution --include-transitive `
            --allowed-license-types (Join-Path $PSScriptRoot 'allowed-licenses.json')
    }
    Invoke-Step 'Vulnerable packages' { & (Join-Path $PSScriptRoot 'Test-VulnerablePackages.ps1') -Solution $solution }
    Invoke-Step 'Game logs' { & (Join-Path $PSScriptRoot 'Test-NoGameLogs.ps1') }
}
finally {
    Pop-Location
}
