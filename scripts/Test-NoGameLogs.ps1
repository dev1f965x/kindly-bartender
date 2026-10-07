#Requires -Version 7
# Hearthstone logs contain BattleTags and account IDs, so they must never reach the repository.
# Fails on tracked log files and on text shaped like a BattleTag (a name, "#", and four or five digits) not listed in battletag-allowlist.txt.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$allowListPath = Join-Path $PSScriptRoot 'battletag-allowlist.txt'
$allowed = @(Get-Content $allowListPath | Where-Object { $_ -and -not $_.StartsWith('#') })
$pattern = '(?<![\w#])[\p{L}\p{N}_]{2,24}#\d{4,5}(?!\d)'
$binaryExtensions = '.png', '.ico', '.jpg', '.gif', '.zip', '.exe', '.dll', '.nupkg'

$files = git -C $root ls-files
if ($LASTEXITCODE -ne 0) {
    throw "git ls-files failed with exit code $LASTEXITCODE"
}

$problems = foreach ($file in $files) {
    if ($file -match '(^|/)Logs/' -or $file -like '*.log') {
        "$file is a log file"
        continue
    }
    if ([IO.Path]::GetExtension($file) -in $binaryExtensions) {
        continue
    }
    $path = Join-Path $root $file
    foreach ($match in Select-String -LiteralPath $path -Pattern $pattern -AllMatches) {
        foreach ($value in $match.Matches.Value) {
            if ("${file}:$value" -notin $allowed) {
                "${file}:$($match.LineNumber) looks like a BattleTag"
            }
        }
    }
}

if ($problems) {
    $problems | ForEach-Object { Write-Host $_ }
    Write-Host "Remove the data, or add '<file>:<text>' to scripts/battletag-allowlist.txt if it is not an account name."
    exit 1
}
Write-Host 'No game logs or BattleTags found.'
exit 0
