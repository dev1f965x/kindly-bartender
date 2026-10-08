#Requires -Version 7
# Builds the files for a GitHub release: a self-contained win-x64 build packed by Velopack into a setup program and
# a portable zip, with third-party notices and SHA-256 checksums. Used by the release workflow and for trial installs.
param(
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version,
    [string]$OutputDirectory = 'artifacts'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$packId = 'KindlyBartender'

function Invoke-Native([string]$Name, [scriptblock]$Command) {
    Write-Host "==> $Name"
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE"
    }
}

# The app's notification ID is velopack.<packId>; a different packId would split the app's identity in Windows.
$identity = Get-Content (Join-Path $root 'src/KindlyBartender.App/Windows/AppIdentity.cs') -Raw
if ($identity -notmatch "PackId = ""$packId"";") {
    throw "AppIdentity.PackId does not match the packId '$packId' used here."
}

$output = Join-Path $root $OutputDirectory
$publish = Join-Path $output 'publish'
$release = Join-Path $output 'release'
if (Test-Path $output) {
    Remove-Item -Recurse -Force $output
}

Push-Location $root
try {
    Invoke-Native 'Restore tools' { dotnet tool restore }
    # No debug files, XML docs, or build paths in the shipped files; local paths contain the Windows user name.
    Invoke-Native 'Publish' {
        dotnet publish src/KindlyBartender.App/KindlyBartender.App.csproj --configuration Release --runtime win-x64 `
            --self-contained true -p:Version=$Version `
            -p:ContinuousIntegrationBuild=true -p:DebugType=none -p:PublishDocumentationFile=false `
            -p:AllowedReferenceRelatedFileExtensions=.none --output $publish
    }
    & (Join-Path $PSScriptRoot 'New-ThirdPartyNotices.ps1') -OutputPath (Join-Path $publish 'THIRD-PARTY-NOTICES.txt') -PublishDirectory $publish
    # Start menu only: a desktop shortcut the player did not ask for is clutter.
    Invoke-Native 'Pack' {
        dotnet vpk pack --packId $packId --packVersion $Version --packDir $publish --mainExe KindlyBartender.exe `
            --packTitle 'Kindly Bartender' --runtime win-x64 --shortcuts StartMenuRoot --icon src/KindlyBartender.App/Assets/KindlyBartender.ico --outputDir $release
    }

    # Only the setup program and the portable zip are published; the app does not use Velopack's update feed.
    $files = Get-ChildItem $release -File | Where-Object { $_.Name -like '*-Setup.exe' -or $_.Name -like '*-Portable.zip' }
    if ($files.Count -ne 2) {
        throw "Expected a setup program and a portable zip in $release."
    }
    $sums = $files | ForEach-Object { "$((Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant())  $($_.Name)" }
    [IO.File]::WriteAllLines((Join-Path $release 'SHA256SUMS.txt'), $sums)
    Write-Host "Release files are in $release"
}
finally {
    Pop-Location
}
