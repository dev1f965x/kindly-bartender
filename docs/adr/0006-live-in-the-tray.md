# 6. Live in the tray, and let the window close

Status: accepted
Date: 2026-09-23

## Context

The app is useful only while the player is in a game and looking somewhere else. A window
on the taskbar the whole time is noise, and an app that quits when its window closes is
useless at the moment it matters.

## Options

- **A window like any other app.** Familiar, and quitting it stops the watching.
- **The tray only, with settings in a menu.** No room to say what the app is doing.
- **A tray icon with a window that can be closed.** Closing hides; quitting is a tray menu
  item; the icon's tooltip carries the current state.

## Decision

A tray icon owns the app. Its left click shows the window, its menu has settings and quit.
Closing the window hides it. The app starts hidden when Windows starts it, and shows the
window when the player starts it.

## Consequences

- Nothing on the taskbar while a match runs.
- The tray icon and its tooltip are the app's only permanent presence.
- Quit has to be somewhere obvious in the menu, or the app cannot be stopped.
