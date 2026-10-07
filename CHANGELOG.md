# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Solution layout, analyzers, the check script, and CI.
- Power.log parser for game creation, game entity tags, spectating, and the log size cap.
- Game tracker that reports hero selection and each Recruit phase, and the tray status priorities.
- Log monitor that finds the running Hearthstone session's log folder, follows Power.log as it grows, and rebuilds the current game silently when attaching mid-game.
- Setup of Hearthstone's log.config and client.config that keeps other tools' settings, backs up the originals, and asks for administrator rights only when a write is denied.
- UI words (CONTENT.md), visual rules (DESIGN.md), wireframes, and a check for forbidden words and punctuation in UI strings.
- Windows notifications, sound, taskbar flashing, showing Hearthstone in front without taking focus, and a Do not disturb check.
- Tray icon and menu, one copy per user, settings file, start with Windows, Korean and English UI text generated from CONTENT.md, and the notification rules.
