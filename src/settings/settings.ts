/** Everything the player can change, and what a fresh install starts with. */
export interface Settings {
  playerName: string;
  installPath: string;
  sound: boolean;
  notification: boolean;
  focus: boolean;
  autostart: boolean;
}

export const DEFAULT_SETTINGS: Settings = {
  playerName: "",
  installPath: "",
  sound: true,
  notification: true,
  focus: true,
  autostart: false,
};

/**
 * Reads whatever was stored, keeping only what this build understands.
 *
 * A file from an older version, or edited by hand, falls back to the defaults per field
 * rather than failing the read (ADR 7).
 */
export function settingsFrom(stored: unknown): Settings {
  if (typeof stored !== "object" || stored === null) return DEFAULT_SETTINGS;
  const raw = stored as Record<string, unknown>;

  return {
    playerName: text(raw.playerName, DEFAULT_SETTINGS.playerName),
    installPath: text(raw.installPath, DEFAULT_SETTINGS.installPath),
    sound: flag(raw.sound, DEFAULT_SETTINGS.sound),
    notification: flag(raw.notification, DEFAULT_SETTINGS.notification),
    focus: flag(raw.focus, DEFAULT_SETTINGS.focus),
    autostart: flag(raw.autostart, DEFAULT_SETTINGS.autostart),
  };
}

/** What the Rust side needs: the two fields that steer the watcher, and the three calls. */
export function forWatcher(settings: Settings) {
  return {
    playerName: settings.playerName.trim() || null,
    installPath: settings.installPath.trim() || null,
    sound: settings.sound,
    notification: settings.notification,
    focus: settings.focus,
  };
}

const text = (value: unknown, fallback: string) => (typeof value === "string" ? value : fallback);
const flag = (value: unknown, fallback: boolean) => (typeof value === "boolean" ? value : fallback);
