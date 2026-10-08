# Kindly Bartender

English | [한국어](README.ko.md)

A Windows tray app for Hearthstone Battlegrounds. When a Recruit phase starts while you are in another window, it tells you, so you get back before the turn timer runs out.

Kindly Bartender is in development and has no release yet.

Kindly Bartender is an unofficial fan project. It is not affiliated with or endorsed by Blizzard Entertainment. Hearthstone is a trademark of Blizzard Entertainment, Inc.

## Privacy

Kindly Bartender collects no personal data and has no telemetry.

- At each start it asks GitHub (`api.github.com`) whether a newer release exists. GitHub sees your IP address and the app version, as with any web request. Nothing is downloaded or installed.
- It keeps a diagnostic log in `%LOCALAPPDATA%KindlyBartenderdataogs`: one file per day, at most seven. Entries are fixed event names, numbers, and error types, never paths, account names, or game text. You can attach it to a problem report; it is never sent anywhere by the app.

## Development

Requirements: Windows 11 and the .NET SDK version in [global.json](global.json).

| Command | Purpose |
| --- | --- |
| `pwsh scripts/check.ps1` | Every check CI runs: format, build with analyzers and the NuGet vulnerability audit, tests, dependency licenses, the game log check, and UI string checks |
| `pwsh scripts/Sync-Strings.ps1` | Regenerates the string resources from CONTENT.md |
| `dotnet build KindlyBartender.slnx` | Build |
| `dotnet test --solution KindlyBartender.slnx` | Run the tests |
| `dotnet format KindlyBartender.slnx` | Fix formatting |

## License

[MIT](LICENSE)
