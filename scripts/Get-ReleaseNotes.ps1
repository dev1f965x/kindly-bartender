#Requires -Version 7
# Writes the release notes for a version from its CHANGELOG.md section, and fails if the version being released
# differs from the one the app is built with, so About and the release always agree.
param(
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$OutputPath
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

$props = [xml](Get-Content (Join-Path $root 'Directory.Build.props') -Raw)
$built = ($props.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ }) | Select-Object -First 1
if ($built -ne $Version) {
    throw "The tag is $Version but Directory.Build.props builds $built."
}

$lines = Get-Content (Join-Path $root 'CHANGELOG.md') -Encoding utf8
$start = [Array]::FindIndex($lines, [Predicate[string]] { param($l) $l -match "^## \[$([regex]::Escape($Version))\]" })
if ($start -lt 0) {
    throw "CHANGELOG.md has no section for $Version."
}
$end = [Array]::FindIndex($lines, $start + 1, [Predicate[string]] { param($l) $l -match '^## ' })
$section = if ($end -lt 0) { $lines[($start + 1)..($lines.Length - 1)] } else { $lines[($start + 1)..($end - 1)] }

$notes = @(
    ($section -join "`n").Trim()
    ''
    'Download `KindlyBartender-win-Setup.exe` to install, or `KindlyBartender-win-Portable.zip` to run without installing. `SHA256SUMS.txt` lists the checksums.'
    ''
    'The files are not code-signed yet, so Windows SmartScreen may warn that the app is unrecognized. Select **More info**, then **Run anyway**, if the checksum matches.'
)
[IO.File]::WriteAllText($OutputPath, ($notes -join "`n") + "`n", [Text.UTF8Encoding]::new($false))
Write-Host "Wrote $OutputPath"
