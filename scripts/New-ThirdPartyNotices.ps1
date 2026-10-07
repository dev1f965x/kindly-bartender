#Requires -Version 7
# Writes THIRD-PARTY-NOTICES.txt for the shipped app: every NuGet package the app uses, with its license text,
# and the .NET runtime that a self-contained build carries. Run by the release workflow.
param([Parameter(Mandatory)][string]$OutputPath)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'src/KindlyBartender.App/KindlyBartender.App.csproj'
$work = Join-Path ([IO.Path]::GetTempPath()) ("kb-notices-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null

$mit = @'
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
documentation files (the "Software"), to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of
the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO
THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
'@

try {
    $json = Join-Path $work 'packages.json'
    $texts = Join-Path $work 'texts'
    dotnet nuget-license --input $project --include-transitive --output Json --file-output $json --license-information-download-location $texts
    if ($LASTEXITCODE -ne 0) {
        throw "nuget-license failed with exit code $LASTEXITCODE"
    }

    $builder = [Text.StringBuilder]::new()
    [void]$builder.AppendLine('Kindly Bartender includes the following third-party software.')
    [void]$builder.AppendLine()

    [void]$builder.AppendLine('.NET runtime, WPF, and Windows Forms')
    [void]$builder.AppendLine('License: MIT. Copyright (c) .NET Foundation and Contributors.')
    [void]$builder.AppendLine('Their own third-party notices: https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT,')
    [void]$builder.AppendLine('https://github.com/dotnet/wpf/blob/main/THIRD-PARTY-NOTICES.TXT, https://github.com/dotnet/winforms/blob/main/THIRD-PARTY-NOTICES.TXT')
    [void]$builder.AppendLine()
    [void]$builder.AppendLine($mit)
    [void]$builder.AppendLine()

    foreach ($package in (Get-Content $json -Raw | ConvertFrom-Json) | Sort-Object PackageId) {
        [void]$builder.AppendLine(('=' * 78))
        [void]$builder.AppendLine("$($package.PackageId) $($package.PackageVersion)")
        [void]$builder.AppendLine("License: $($package.License)")
        if ($package.Copyright) {
            [void]$builder.AppendLine($package.Copyright)
        }
        if ($package.PackageProjectUrl) {
            [void]$builder.AppendLine($package.PackageProjectUrl)
        }
        $text = Get-ChildItem -Path $texts -Filter "$($package.PackageId)__$($package.PackageVersion).*" -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($text -and $text.Extension -eq '.txt') {
            [void]$builder.AppendLine()
            [void]$builder.AppendLine((Get-Content $text.FullName -Raw).Trim())
        }
        elseif ($package.License -eq 'MIT') {
            [void]$builder.AppendLine()
            [void]$builder.AppendLine($mit)
        }
        else {
            throw "No license text for $($package.PackageId); add it before releasing."
        }
        [void]$builder.AppendLine()
    }

    [IO.File]::WriteAllText($OutputPath, $builder.ToString(), [Text.UTF8Encoding]::new($false))
    Write-Host "Wrote $OutputPath"
}
finally {
    Remove-Item -Recurse -Force $work
}
