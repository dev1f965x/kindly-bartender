#Requires -Version 7
# Renders index.html to wireframes.png with headless Edge, for reviews where the HTML cannot be opened.
$ErrorActionPreference = 'Stop'
$edge = Join-Path ${env:ProgramFiles(x86)} 'Microsoft\Edge\Application\msedge.exe'
$page = Join-Path $PSScriptRoot 'index.html'
$output = Join-Path $PSScriptRoot 'wireframes.png'
$profileDir = Join-Path ([IO.Path]::GetTempPath()) 'kindly-bartender-edge-capture'
& $edge --headless=new --disable-gpu --hide-scrollbars "--user-data-dir=$profileDir" "--screenshot=$output" `
    --window-size=1700,1560 ([Uri]$page).AbsoluteUri | Out-Null
Write-Host "Wrote $output"
