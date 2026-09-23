import { useCallback, useEffect, useRef, useState } from "react";
import type { Autostart, Bartender, SettingsMemory } from "../shell/ports";
import { DEFAULT_SETTINGS, type Settings, settingsFrom } from "./settings";

/**
 * The settings the window holds, saved as they change and handed straight to the watcher,
 * so a switch takes effect before the player looks away.
 */
export function useSettings(memory: SettingsMemory, bartender: Bartender, autostart: Autostart) {
  const [settings, setSettings] = useState<Settings>(DEFAULT_SETTINGS);
  const loaded = useRef(false);

  useEffect(() => {
    let cancelled = false;

    void (async () => {
      const [stored, startsWithWindows] = await Promise.all([
        memory.load(),
        autostart.enabled().catch(() => DEFAULT_SETTINGS.autostart),
      ]);
      if (cancelled) return;

      // Windows itself is the truth about starting with Windows; the stored flag only
      // remembers what the switch looked like.
      const settings = { ...settingsFrom(stored), autostart: startsWithWindows };
      loaded.current = true;
      setSettings(settings);
      void bartender.apply(settings);
    })();

    return () => {
      cancelled = true;
    };
  }, [memory, bartender, autostart]);

  const change = useCallback(
    (changes: Partial<Settings>) => {
      setSettings((previous) => {
        const next = { ...previous, ...changes };
        void memory.save(next);
        void bartender.apply(next);
        if (changes.autostart !== undefined) void autostart.set(changes.autostart);
        return next;
      });
    },
    [memory, bartender, autostart],
  );

  return { settings, change };
}
