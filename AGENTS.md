# AGENTS.md

Guidance for coding agents working in this repository. Humans should start with [README.md](README.md).

## Project

A Windows tray app that notifies a Hearthstone Battlegrounds player when a Recruit phase starts while Hearthstone is in the background. It reads Hearthstone's Power.log. Product documents (Problem Brief, PRD, Design Doc) are in the KBT Confluence space.

## Setup

Windows native: the .NET SDK pinned in `global.json` and VS Code with C# Dev Kit. There is no Dev Container, because WPF and the Windows APIs need Windows.

## Commands

The commands are listed in [README.md](README.md#development). Run every check (format and lint, type check, tests, build, end-to-end) before handing off any change. After a dependency change, run all of them even if the change looks unrelated.

## Structure

| Path | Contents |
| --- | --- |
| `src/KindlyBartender.Core` | Log parsing, game tracking, settings, configuration file editing. No Windows APIs. |
| `src/KindlyBartender.App` | WPF tray app and Windows adapters (`net10.0-windows10.0.22621.0`). |
| `tests/` | xUnit v3 test projects, run through Microsoft.Testing.Platform. |
| `scripts/` | The check script and the checks it runs. |
| `design/wireframes/` | Low-fidelity wireframes (`index.html`) and their capture (`capture.ps1`). |
| `CONTENT.md`, `DESIGN.md` | UI words and visual rules; `scripts/Test-Content.ps1` checks the strings. Edit UI words only in CONTENT.md, then run `scripts/Sync-Strings.ps1` to regenerate `src/KindlyBartender.App/Resources/Strings*.resx`. |

## Conventions

- Comments explain why, not what.
- Never commit Hearthstone logs or text from them; they contain BattleTags and account IDs. Test fixtures are written by hand. `scripts/Test-NoGameLogs.ps1` enforces this.

## Git and pull requests

- Branch: `<type>/KBT-<n>-<short-description>`. Commits follow Conventional Commits and end with a `Refs: KBT-<n>` paragraph.
- `main` changes only through squash-merged pull requests.
- Do not push, open pull requests, or merge; the maintainer does that.
- When the working tree is shared, use a separate `git worktree` and do not switch the checked-out branch.
- Before handing a change over, have it reviewed by a separate agent or review tool that did not write it.
