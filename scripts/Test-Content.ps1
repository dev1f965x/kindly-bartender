#Requires -Version 7
# Enforces the content rules in CONTENT.md on every UI string: the string tables in CONTENT.md and the
# values in .resx resource files. Fails on forbidden words, exclamation marks, and straight quotes.
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$root = Split-Path $PSScriptRoot -Parent

$forbiddenEnglish = 'seamless', 'effortless', 'simply', 'just', 'easily', 'powerful', 'robust', 'leverage',
    'unlock', 'successfully', 'please', 'oops', 'uh-oh', 'above', 'below', 'on the right'
$forbiddenKorean = '해 드릴게요', '하실 수 있어요', '을 통해', '를 통해', '에 대한', '해당', '하는 것이 가능합니다',
    '되어집니다', '에 있어서', '성공적으로', '정상적으로', '손쉽게', '간편하게', '다양한', '효율적으로', '스마트하게',
    '해 주세요', '하시기 바랍니다', '^^'

function Get-Strings {
    $content = Join-Path $root 'CONTENT.md'
    $inStrings = $false
    foreach ($line in Get-Content $content) {
        if ($line -match '^## ') {
            $inStrings = $line -eq '## Strings'
            continue
        }
        if ($inStrings -and $line -match '^\| (?!ID |---)([^|]+)\|([^|]+)\|([^|]+)\|') {
            [pscustomobject]@{ Source = "CONTENT.md $($Matches[1].Trim())"; Text = $Matches[2].Trim() }
            [pscustomobject]@{ Source = "CONTENT.md $($Matches[1].Trim())"; Text = $Matches[3].Trim() }
        }
    }

    foreach ($resx in Get-ChildItem -Path (Join-Path $root 'src') -Filter '*.resx' -Recurse) {
        [xml]$xml = Get-Content $resx.FullName -Raw
        foreach ($data in $xml.root.data) {
            [pscustomobject]@{ Source = "$($resx.Name) $($data.name)"; Text = [string]$data.value }
        }
    }
}

$problems = foreach ($string in Get-Strings) {
    $text = $string.Text
    if ($text.Contains('!')) {
        "$($string.Source): exclamation mark"
    }
    if ($text -match "['""]") {
        "$($string.Source): straight quote; use curly quotes and apostrophes"
    }
    foreach ($word in $forbiddenEnglish) {
        if ($text -match "(?i)\b$([regex]::Escape($word))\b") {
            "$($string.Source): '$word'"
        }
    }
    foreach ($phrase in $forbiddenKorean) {
        if ($text.Contains($phrase)) {
            "$($string.Source): '$phrase'"
        }
    }
    # Korean sentences use 합니다체; a sentence ending in 요 is 해요체, except the -세요 request form.
    # Only sentence endings are checked, so nouns such as 필요 or 중요 do not match.
    if ($text -match '(?<!세)요[.?](\s|$)') {
        "$($string.Source): 해요체 ending; use 합니다체"
    }
}

if ($problems) {
    $problems | ForEach-Object { Write-Host $_ }
    exit 1
}
Write-Host 'UI strings follow the content rules.'
exit 0
