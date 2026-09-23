# Kindly Bartender — product definition

## Problem

In Hearthstone Battlegrounds the combat phase plays itself. Players alt-tab to a browser
or a video while it runs, and come back late to the shop, where every second is gold
spent, minions rolled, and a board built. The game gives no sound or flash when the shop
reopens, and a minimised window gives nothing at all.

## Who it is for

Someone who plays Battlegrounds on a Windows PC and does something else during combat.
They want the game to come and get them, and nothing else.

## 1.0.0 scope

A tray app that watches the game and calls the player back when the shop reopens.

- Watches Hearthstone's `Power.log` and recognises two moments:
  - **Combat ended** — the shop is open again. Solo and Duos.
  - **Gold arrived (Duos)** — a teammate's gold reached the player mid-shop.
- On a moment: plays a sound, shows a Windows notification, and brings Hearthstone to the
  front. Each of the three can be turned off on its own.
- Turns the game's logging on by writing `log.config`, and lifts its 10MB cap by writing
  `client.config`, since a Battlegrounds match reaches the cap in about ten minutes.
- Finds the install through the running process, a saved path, or the usual folders; the
  path can be set by hand when none of those work.
- A window that says what it is doing right now — watching, waiting for the game, or
  install not found — and holds the settings. Closing it leaves the app in the tray.
- Starts with Windows, off by default.
- Updates itself from a signed release.

## Acceptance criteria

- With the game running and a Battlegrounds match in progress, the moment combat ends the
  sound plays, a notification appears, and Hearthstone is in front, inside a second.
- Nothing fires while the player is already in the shop, in a menu, or watching a replay.
- Turning off "bring to front" leaves the sound and the notification, and the game stays
  where it is.
- With Hearthstone closed, the window says it is waiting, and nothing is announced.
- On a first run the window explains that Hearthstone has to restart once before logging
  starts, and stops saying so once a log is being read.
- With the game installed somewhere unusual, the window says it cannot find it and takes
  a path.
- Closing the window keeps the app running in the tray; quitting from the tray menu ends it.
- Settings survive a restart.
- A published release reaches an installed copy without a manual download.

## Non-goals

- Nothing is read from the game's memory and no input is sent to it. The app reads a log
  file the game itself writes, and brings a window to the front, which is what a person
  does with Alt+Tab. It never plays for the player.
- No deck tracking, no overlay on the game, no statistics, no accounts, no telemetry.
- Nothing about other game modes. Constructed has its own turn timer and rope.

## Non-functional requirements

- A moment in the log reaches the player within a second.
- Idle cost is a poll of one file twice a second; no game process is touched beyond
  finding its path and its window.
- The window is usable with the keyboard alone, and every state it shows is text.
- Nothing about the player leaves the machine. The app makes one kind of request, for the
  release manifest and the installer it points to, both on GitHub.

## Roadmap

| Version | Adds |
|---|---|
| 1.0.0 | Combat ended, Duos gold, sound, notification, focus, settings, updates |
| 1.1.0 | A teammate's card passed in Duos, once confirmed against a real log |
| later | A per-moment sound, and a quiet mode while the window is already in front |

The Duos card pass is held back because the previous build recognised it from a guess at
the log line, and it was never confirmed in a real game.
