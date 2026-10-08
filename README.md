<a id="readme-top"></a>

[![CI][ci-shield]][ci-url]
[![Issues][issues-shield]][issues-url]
[![License][license-shield]][license-url]

<br />
<div align="center">

<h3 align="center">Kindly Bartender</h3>

  <p align="center">
    A Windows tray app that tells you when a Hearthstone Battlegrounds phase starts while you are in another window.
    <br />
    English | <a href="README.ko.md">한국어</a>
    <br />
    <br />
    <a href="https://github.com/dev1f965x/kindly-bartender/releases"><strong>Download »</strong></a>
    <br />
    <br />
    <a href="https://github.com/dev1f965x/kindly-bartender/issues/new?template=bug_report.yml">Report a bug</a>
    &middot;
    <a href="https://github.com/dev1f965x/kindly-bartender/issues/new?template=feature_request.yml">Request a feature</a>
  </p>
</div>

<details>
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project</a>
      <ul>
        <li><a href="#built-with">Built With</a></li>
      </ul>
    </li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#prerequisites">Prerequisites</a></li>
        <li><a href="#installation">Installation</a></li>
        <li><a href="#uninstall">Uninstall</a></li>
      </ul>
    </li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#roadmap">Roadmap</a></li>
    <li><a href="#privacy">Privacy</a></li>
    <li><a href="#development">Development</a></li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#contact">Contact</a></li>
    <li><a href="#acknowledgments">Acknowledgments</a></li>
  </ol>
</details>

## About The Project

![The Settings window with the options for what happens when a phase starts][product-screenshot]

A Windows tray app for Hearthstone Battlegrounds. When a Recruit phase starts while you are in another window, it tells you, so you get back before the turn timer runs out.

Kindly Bartender is in development and has no release yet.

Kindly Bartender is an unofficial fan project. It is not affiliated with or endorsed by Blizzard Entertainment. Hearthstone is a trademark of Blizzard Entertainment, Inc.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

### Built With

* [![.NET][dotnet-shield]][dotnet-url]
* [![WPF][wpf-shield]][wpf-url]
* [![Velopack][velopack-shield]][velopack-url]
* [![xUnit.net][xunit-shield]][xunit-url]

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Getting Started

### Prerequisites

Windows 10 version 2004 or later, or Windows 11.

### Installation

1. Download `KindlyBartender-win-Setup.exe` from [Releases](https://github.com/dev1f965x/kindly-bartender/releases). It installs for your user only and needs no administrator rights.
2. The files are not code-signed yet, so SmartScreen may say the app is unrecognized. Compare the file's SHA-256 with `SHA256SUMS.txt` (`Get-FileHash .\KindlyBartender-win-Setup.exe` in PowerShell), then select **More info** and **Run anyway**.
3. Open Kindly Bartender from the Start menu and follow the Log settings window. It changes two Hearthstone settings files only after you agree; restart Hearthstone if it was running.

To run it without installing, extract `KindlyBartender-win-Portable.zip` and run `Kindly Bartender.exe`. The portable copy keeps its settings and diagnostic log in its own folder. To remove it, turn off Start with Windows in Settings, exit it from the tray, and delete the folder.

### Uninstall

Uninstall it from **Settings > Apps > Installed apps** (Windows 11) or **Settings > Apps > Apps & features** (Windows 10). This removes the app, its settings and diagnostic log, and the start-with-Windows entry. Hearthstone's log settings stay, because other tools such as deck trackers may use them. To undo them, delete the `[Power]` section from `%LOCALAPPDATA%\Blizzard\Hearthstone\log.config` and the `FileSizeLimit.Int` line from `client.config` in the Hearthstone folder.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Usage

Kindly Bartender runs in the tray. Play Hearthstone as usual. When hero selection or a Recruit phase starts in solo Battlegrounds while another window is active, it does what you chose under **When a phase starts** in **Settings**:

- Show a notification. Select it to return to Hearthstone.
- Play a sound.
- Flash Hearthstone on the taskbar.
- Bring Hearthstone to the front. Your typing stays in the window you are using.

Nothing happens while Hearthstone is the active window. To stop notifications for a while, select **Pause notifications** in the tray menu. When Windows Do not disturb is on, notifications may be hidden; to let them through, add Kindly Bartender to priority notifications in Windows settings.

The app reads Hearthstone's log. Blizzard's End User License Agreement forbids programs it hasn't authorized from reading game data, and Blizzard could take action against your account. Use Kindly Bartender at your own risk.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Roadmap

- [ ] Acceptance testing
- [ ] First release (0.1.0)

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Privacy

Kindly Bartender collects no personal data and has no telemetry.

- At each start it asks GitHub (`api.github.com`) whether a newer release exists. GitHub sees your IP address and the app version, as with any web request. Nothing is downloaded or installed.
- It keeps a diagnostic log in `%LOCALAPPDATA%\KindlyBartender\data\logs`: one file per day, at most seven. Entries are fixed event names, numbers, and error types, never paths, account names, or game text. You can attach it to a problem report; it is never sent anywhere by the app.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

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

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Contributing

Bug reports and feature requests go to [GitHub Issues](https://github.com/dev1f965x/kindly-bartender/issues/new/choose), which has a template for each. Report security issues privately as described in [SECURITY.md](SECURITY.md).

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## License

Distributed under the [MIT License](LICENSE). Each release includes the third-party notices.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Contact

Project link: <https://github.com/dev1f965x/kindly-bartender>

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Acknowledgments

* [Best-README-Template](https://github.com/othneildrew/Best-README-Template), the layout of this README
* [Velopack](https://velopack.io), the installer

<p align="right">(<a href="#readme-top">back to top</a>)</p>

[ci-shield]: https://img.shields.io/github/actions/workflow/status/dev1f965x/kindly-bartender/ci.yml?branch=main&style=for-the-badge&label=CI
[ci-url]: https://github.com/dev1f965x/kindly-bartender/actions/workflows/ci.yml
[issues-shield]: https://img.shields.io/github/issues/dev1f965x/kindly-bartender?style=for-the-badge
[issues-url]: https://github.com/dev1f965x/kindly-bartender/issues
[license-shield]: https://img.shields.io/github/license/dev1f965x/kindly-bartender?style=for-the-badge
[license-url]: LICENSE
[product-screenshot]: design/screenshots/settings-en-light.png
[dotnet-shield]: https://img.shields.io/badge/.NET_10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white
[dotnet-url]: https://dotnet.microsoft.com/
[wpf-shield]: https://img.shields.io/badge/WPF-512BD4?style=for-the-badge
[wpf-url]: https://github.com/dotnet/wpf
[velopack-shield]: https://img.shields.io/badge/Velopack-1F2937?style=for-the-badge
[velopack-url]: https://velopack.io/
[xunit-shield]: https://img.shields.io/badge/xUnit.net-5E2750?style=for-the-badge
[xunit-url]: https://xunit.net/
