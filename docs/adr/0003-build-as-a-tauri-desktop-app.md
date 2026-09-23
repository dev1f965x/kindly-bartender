# 3. Build as a Tauri desktop app

Status: accepted
Date: 2026-09-23

## Context

The first build of this app was C# with WinForms. It worked, and its window looked like a
dialog from 2005: controls at fixed pixel offsets, no layout, nothing that could be called
a design. This rewrite keeps the approach to the log and starts the rest again.

## Options

- **Keep WinForms.** Nothing new to learn, and the same window.
- **WPF.** A real layout and styling in the same language, so the log code moves over as
  it is. Its own updater and installer would have to be built.
- **Tauri 2 with a web interface.** The window is drawn with the tools that are actually
  good at drawing windows, the updater and the signed installer come with the framework,
  and the design can be reviewed from screenshots the way 4GHz's is. The log tail and the
  Win32 calls have to be written again in Rust.

## Decision

Tauri 2. The interface is a web page, the log watcher and the Windows calls are Rust, and
the two talk over Tauri's events.

## Consequences

- The window can be designed, and every state of it photographed by a script.
- The updater, the signed installer, and the tray are the framework's, not this app's.
- The log watcher is rewritten in Rust, which is the real cost of this decision.
- The install is a few megabytes and uses the WebView2 runtime that Windows 10 and 11
  already carry.
