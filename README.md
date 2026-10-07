# Kindly Bartender

English | [한국어](README.ko.md)

A Windows tray app for Hearthstone Battlegrounds. When a Recruit phase starts while you are in another window, it tells you, so you get back before the turn timer runs out.

Kindly Bartender is in development and has no release yet.

Kindly Bartender is an unofficial fan project. It is not affiliated with or endorsed by Blizzard Entertainment. Hearthstone is a trademark of Blizzard Entertainment, Inc.

## Development

Requirements: Windows 11 and the .NET SDK version in [global.json](global.json).

| Command | Purpose |
| --- | --- |
| `pwsh scripts/check.ps1` | Every check CI runs: format, build with analyzers, tests, dependency licenses, vulnerable packages, and the game log check |
| `dotnet build KindlyBartender.slnx` | Build |
| `dotnet test --solution KindlyBartender.slnx` | Run the tests |
| `dotnet format KindlyBartender.slnx` | Fix formatting |

## License

[MIT](LICENSE)
