# 2. Read the game's log, and nothing else

Status: accepted
Date: 2026-09-23

## Context

The app has to know the instant a Battlegrounds combat ends. Hearthstone offers no API
and no plugin interface, and Blizzard's end user agreement forbids reading or changing
the game's memory and automating play.

## Options

- **Read the game's memory.** Exact and immediate, and the line every deck tracker used
  to cross. It is also what the agreement names, and a ban risk carried by the player.
- **Watch the screen.** No files, no memory, but a frame grab several times a second, a
  matcher per resolution and language, and a guess for an answer.
- **Read `Power.log`.** The game writes it when `log.config` asks it to. Deck trackers
  have used it for years, it names the moments in plain text, and it is the same file a
  player could open in Notepad.

## Decision

`Power.log`, tailed as the game appends to it. The app writes `log.config` to turn the
logging on and `client.config` to lift the 10MB cap; it reads nothing else of the game's,
sends it no input, and never acts in the player's place.

## Consequences

- The player has to restart Hearthstone once, after the first run, for logging to start.
- A patch can rename a tag and stop a signal from being recognised. The detector is a
  small, tested unit for exactly that reason, and the window says when nothing is arriving.
- Bringing the window to the front is the app's only touch on the game, and it is what
  Alt+Tab does.
