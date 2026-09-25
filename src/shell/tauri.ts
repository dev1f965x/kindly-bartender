import { invoke } from "@tauri-apps/api/core";
import { listen } from "@tauri-apps/api/event";
import { disable, enable, isEnabled } from "@tauri-apps/plugin-autostart";
import { open } from "@tauri-apps/plugin-dialog";
import { load } from "@tauri-apps/plugin-store";
import type { Moment, Status } from "../domain/watching";
import { forWatcher, type Settings } from "../settings/settings";
import type { Autostart, Bartender, SettingsMemory } from "./ports";

const SETTINGS_FILE = "settings.json";
const SETTINGS_KEY = "settings";

/** The running app: the Rust watcher, the dialog, and the notifications it sends. */
export const tauriBartender: Bartender = {
  async watch(onStatus, onMoment) {
    const unlisten = await Promise.all([
      listen<Status>("status", (event) => onStatus(event.payload)),
      listen<Moment>("moment", (event) => onMoment(event.payload)),
    ]);
    return () => {
      for (const stop of unlisten) stop();
    };
  },

  async apply(settings: Settings) {
    await invoke("apply_settings", { settings: forWatcher(settings) });
  },

  async tryTheCall() {
    await invoke("try_the_call");
  },

  loggingJustStarted() {
    return invoke<boolean>("logging_just_started");
  },

  gameIsRunning() {
    return invoke<boolean>("game_is_running");
  },

  async askForInstallFolder() {
    const chosen = await open({ directory: true, title: "하스스톤 폴더 고르기" });
    return chosen ?? undefined;
  },
};

/** Settings in the app data folder. */
export const storeSettings: SettingsMemory = {
  async load() {
    const store = await load(SETTINGS_FILE, { autoSave: false });
    return store.get(SETTINGS_KEY);
  },

  async save(settings) {
    const store = await load(SETTINGS_FILE, { autoSave: false });
    await store.set(SETTINGS_KEY, settings);
    await store.save();
  },
};

export const windowsAutostart: Autostart = {
  enabled: isEnabled,
  set: async (wanted) => {
    if (wanted) await enable();
    else await disable();
  },
};
