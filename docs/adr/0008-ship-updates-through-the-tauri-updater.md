# 8. Ship updates through the Tauri updater

Status: accepted
Date: 2026-09-23

## Context

A log format can change with a game patch, and a fix has to reach installed copies without
the player going looking for it.

## Options

- **Tell people to download the new installer.** Free, and most copies stay on the old
  version forever.
- **A store.** Review queues for an app that watches a log file of another game.
- **Tauri's updater.** Reads a manifest from the GitHub release, verifies a signature
  against a public key inside the app, installs in place.

## Decision

Tauri's updater, with the manifest published as `latest.json` on the GitHub release. The
private key lives outside the repository and in GitHub Secrets; the release workflow signs
the installer.

## Consequences

- An update that is not signed by that key is refused, so a compromised release cannot
  install anything.
- Losing the private key means no installed copy can ever be updated again.
- The app checks on launch and every six hours, and offers rather than forces the update.
