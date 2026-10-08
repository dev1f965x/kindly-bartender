#Requires -Version 7
# Hearthstone logs contain BattleTags and account IDs, so they must never reach the repository.
# Fails on tracked log files, on text shaped like a BattleTag (a name, "#", and four or five digits),
# and on account ID pairs, unless the match is listed in battletag-allowlist.txt.
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$root = Split-Path $PSScriptRoot -Parent
$allowListPath = Join-Path $PSScriptRoot 'battletag-allowlist.txt'
$allowed = @(Get-Content $allowListPath | Where-Object { $_ -and -not $_.StartsWith('#') })
# BattleTag names are 3 to 12 characters and start with a letter.
$battleTag = '(?<![\w#-])\p{L}[\p{L}\p{N}]{2,11}#\d{4,5}(?!\d)'
$accountId = 'hi=\d{6,} lo=\d{6,}'
$binaryExtensions = '.png', '.ico', '.jpg', '.gif', '.zip', '.exe', '.dll', '.nupkg'

$output = git -C $root -c core.quotepath=off ls-files -z
if ($LASTEXITCODE -ne 0) {
    throw "git ls-files failed with exit code $LASTEXITCODE"
}
$files = ($output -join '') -split "`0" | Where-Object { $_ }

$problems = foreach ($file in $files) {
    if ($file -cmatch '(^|/)Logs/' -or $file -like '*.log') {
        "$file is a log file"
        continue
    }
    if ([IO.Path]::GetExtension($file) -in $binaryExtensions) {
        continue
    }
    $path = Join-Path $root $file
    foreach ($match in Select-String -LiteralPath $path -Pattern $battleTag, $accountId -AllMatches) {
        foreach ($value in $match.Matches.Value) {
            if ("${file}:$value" -notin $allowed) {
                "${file}:$($match.LineNumber) looks like a BattleTag or account ID"
            }
        }
    }
}

if ($problems) {
    $problems | ForEach-Object { Write-Host $_ }
    Write-Host "Remove the data, or add '<file>:<text>' to scripts/battletag-allowlist.txt if it is not account data."
    exit 1
}
Write-Host 'No game logs, BattleTags, or account IDs found.'
exit 0
