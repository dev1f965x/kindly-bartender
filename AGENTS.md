# AGENTS.md

Guidance for coding agents working in this repository. Humans should start with [README.md](README.md).

## Project

A Windows desktop app.

## Setup

<!-- Development environment (Dev Container for web and backend projects) and how to start it. -->

## Commands

The commands are listed in [README.md](README.md#development). Run every check (format and lint, type check, tests, build, end-to-end) before handing off any change. After a dependency change, run all of them even if the change looks unrelated.

## Structure

| Path | Contents |
| --- | --- |

## Conventions

- Comments explain why, not what.

## Git and pull requests

- Branch: `<type>/KBT-<n>-<short-description>`. Commits follow Conventional Commits and end with a `Refs: KBT-<n>` paragraph.
- `main` changes only through squash-merged pull requests.
- Do not push, open pull requests, or merge; the maintainer does that.
- When the working tree is shared, use a separate `git worktree` and do not switch the checked-out branch.
- Before handing a change over, have it reviewed by a separate agent or review tool that did not write it.
