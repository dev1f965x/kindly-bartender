# Kindly Bartender

English | [한국어](README.ko.md)

A Windows tray app for Hearthstone Battlegrounds. When a Recruit phase starts while you are in another window, it tells you, so you get back before the turn timer runs out.

Kindly Bartender is in development and has no release yet.

Kindly Bartender is an unofficial fan project. It is not affiliated with or endorsed by Blizzard Entertainment. Hearthstone is a trademark of Blizzard Entertainment, Inc.

## Install

Requires Windows 10 version 2004 or later, or Windows 11.

1. Download `KindlyBartender-win-Setup.exe` from [Releases](https://github.com/dev1f965x/kindly-bartender/releases). It installs for your user only and needs no administrator rights.
2. The files are not code-signed yet, so SmartScreen may say the app is unrecognized. Compare the file's SHA-256 with `SHA256SUMS.txt` (`Get-FileHash .\KindlyBartender-win-Setup.exe` in PowerShell), then select **More info** and **Run anyway**.
3. Open Kindly Bartender from the Start menu and follow the Log settings window. It changes two Hearthstone settings files only after you agree; restart Hearthstone if it was running.

To run it without installing, extract `KindlyBartender-win-Portable.zip` and run `Kindly Bartender.exe`. The portable copy keeps its settings and diagnostic log in its own folder. To remove it, turn off Start with Windows in Settings, exit it from the tray, and delete the folder.

## Uninstall

Uninstall it from **Settings > Apps > Installed apps** (Windows 11) or **Settings > Apps > Apps & features** (Windows 10). This removes the app, its settings and diagnostic log, and the start-with-Windows entry. Hearthstone's log settings stay, because other tools such as deck trackers may use them. To undo them, delete the `[Power]` section from `%LOCALAPPDATA%\Blizzard\Hearthstone\log.config` and the `FileSizeLimit.Int` line from `client.config` in the Hearthstone folder.

## Privacy

Kindly Bartender collects no personal data and has no telemetry.

- At each start it asks GitHub (`api.github.com`) whether a newer release exists. GitHub sees your IP address and the app version, as with any web request. Nothing is downloaded or installed.
- It keeps a diagnostic log in `%LOCALAPPDATA%\KindlyBartender\data\logs`: one file per day, at most seven. Entries are fixed event names, numbers, and error types, never paths, account names, or game text. You can attach it to a problem report; it is never sent anywhere by the app.

## Development

Requirements: Windows 11 and the .NET SDK version in [global.json](global.json).

| Command | Purpose |
| --- | --- |
| `pwsh scripts/check.ps1` | Every check CI runs: format, build with analyzers and the NuGet vulnerability audit, tests, dependency licenses, the game log check, and UI string checks |
| `pwsh scripts/Sync-Strings.ps1` | Regenerates the string resources from CONTENT.md |
| `pwsh scripts/New-Release.ps1 -Version <x.y.z>` | Builds the setup program, portable zip, notices, and checksums into `artifacts/release`; the release workflow runs it on a version tag |
| `dotnet build KindlyBartender.slnx` | Build |
| `dotnet test --solution KindlyBartender.slnx` | Run the tests |
| `dotnet format KindlyBartender.slnx` | Fix formatting |

## License

[MIT](LICENSE)
