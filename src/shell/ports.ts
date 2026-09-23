import type { Moment, Status } from "../domain/watching";
import type { Settings } from "../settings/settings";

/** What the window needs from the app around it, so a test can stand in for all of it. */
export interface Bartender {
  /** Follows the watcher until the returned function is called. */
  watch(
    onStatus: (status: Status) => void,
    onMoment: (moment: Moment) => void,
  ): Promise<() => void>;
  /** Hands the settings to the watcher and the caller. */
  apply(settings: Settings): Promise<void>;
  /** Calls the player back as a real moment would, for trying the settings out. */
  tryTheCall(): Promise<void>;
  /** Whether this run turned the game's logging on, which needs the game restarted. */
  loggingJustStarted(): Promise<boolean>;
  /** Asks for the install folder, and gives back what was chosen. */
  askForInstallFolder(): Promise<string | undefined>;
}

/** Where the settings are kept between runs (ADR 7). */
export interface SettingsMemory {
  load(): Promise<unknown>;
  save(settings: Settings): Promise<void>;
}

/** Windows' own list of what starts with it. */
export interface Autostart {
  enabled(): Promise<boolean>;
  set(enabled: boolean): Promise<void>;
}
