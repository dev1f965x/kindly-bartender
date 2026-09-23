# 9. Testing strategy

Status: accepted
Date: 2026-09-23

## Context

The risky parts are the detector, which turns log lines into moments, and the states the
window shows. Neither needs a running Hearthstone to be tested, and neither is covered by
clicking around.

## Decision

- **Rust unit tests** for the detector and the log finder, fed by recorded log excerpts
  kept as fixtures, including a Duos match and a solo match.
- **Vitest and Testing Library** for the window's states and its settings.
- **Playwright** against the real page, with Tauri's calls answered by a stand-in, for the
  flows that cross components.
- Every one of them runs in CI on each pull request.

## Consequences

- A game patch that changes a tag is caught by a failing detector test as soon as a new
  log is recorded, not by a player missing a shop.
- Nothing tests the Win32 focus call or the tray, which are thin and have to be checked by
  hand on a real machine before a release.
