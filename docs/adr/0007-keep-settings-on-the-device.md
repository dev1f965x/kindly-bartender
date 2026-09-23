# 7. Keep settings on the device

Status: accepted
Date: 2026-09-23

## Context

The app holds a BattleTag name, an install path, and four switches. None of it is worth
an account.

## Decision

Settings live in `settings.json` in the app data folder through Tauri's store plugin, and
are never sent anywhere. The BattleTag name is kept without its `#NNNN` discriminator,
which players can change.

## Consequences

- Nothing about the player leaves the machine, so the privacy line in the product
  definition holds without qualification.
- Settings do not follow the player to a second PC, which needs an account to solve.
- A settings file edited by hand or left over from an older version is read defensively:
  anything unrecognised falls back to the default.
