import { describe, expect, it } from "vitest";
import { DEFAULT_SETTINGS, forWatcher, settingsFrom } from "./settings";

describe("settingsFrom", () => {
  it("starts a fresh install with the calls on", () => {
    expect(settingsFrom(undefined)).toEqual(DEFAULT_SETTINGS);
    expect(DEFAULT_SETTINGS.sound && DEFAULT_SETTINGS.notification && DEFAULT_SETTINGS.focus).toBe(
      true,
    );
  });

  it("keeps what was stored", () => {
    const stored = { playerName: "바텐더", installPath: "D:/Hearthstone", focus: false };

    expect(settingsFrom(stored)).toEqual({
      ...DEFAULT_SETTINGS,
      playerName: "바텐더",
      installPath: "D:/Hearthstone",
      focus: false,
    });
  });

  it("falls back to the default for anything it cannot read", () => {
    const edited = { playerName: 42, sound: "yes", nonsense: true };

    expect(settingsFrom(edited)).toEqual(DEFAULT_SETTINGS);
  });
});

describe("forWatcher", () => {
  it("passes nothing where the player left a field empty", () => {
    const watcher = forWatcher({ ...DEFAULT_SETTINGS, playerName: "  ", installPath: "" });

    expect(watcher.playerName).toBeNull();
    expect(watcher.installPath).toBeNull();
  });

  it("carries the name and path that were set", () => {
    const watcher = forWatcher({
      ...DEFAULT_SETTINGS,
      playerName: "바텐더",
      installPath: "D:/Hearthstone",
    });

    expect(watcher).toEqual({
      playerName: "바텐더",
      installPath: "D:/Hearthstone",
      sound: true,
      notification: true,
      focus: true,
    });
  });
});
