# 5. Poll the log on a timer

Status: accepted
Date: 2026-09-23

## Context

Hearthstone keeps `Power.log` open and appends to it, and starts a new file per session,
sometimes in a new folder. The app has to notice new lines within a second.

## Options

- **`FileSystemWatcher`-style notifications.** The OS reports a change only when metadata
  is flushed, which for a file held open by another process can be seconds late or absent.
- **A poll on a timer.** Read from the last offset twice a second, and look for a newer
  `Power.log` at the same time.

## Decision

A poll every 500ms: find the newest `Power.log`, read from the offset reached last time,
feed each new line to the detector. A file that shrank means a new session, so the offset
goes back to zero.

## Consequences

- The delay is bounded by the poll interval, and the cost is one read of a few kilobytes.
- A session that starts while the app runs is picked up without a restart.
- A new session is read from its end, not its beginning, so the app never announces a
  combat that ended before it was watching.
