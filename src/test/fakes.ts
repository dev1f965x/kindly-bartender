import { vi } from "vitest";
import type { Moment, Status } from "../domain/watching";
import type { Settings } from "../settings/settings";
import type { Autostart, Bartender, SettingsMemory } from "../shell/ports";

/** A bartender the test drives: it reports what the test tells it to, and records calls. */
export function fakeBartender(loggingJustStarted = false) {
  let report: ((status: Status) => void) | undefined;
  let called: ((moment: Moment) => void) | undefined;

  const bartender: Bartender = {
    async watch(onStatus, onMoment) {
      report = onStatus;
      called = onMoment;
      return () => {
        report = undefined;
        called = undefined;
      };
    },
    apply: vi.fn(async () => {}),
    tryTheCall: vi.fn(async () => {}),
    loggingJustStarted: async () => loggingJustStarted,
    askForInstallFolder: vi.fn(async () => undefined as string | undefined),
  };

  return {
    bartender,
    says: (status: Status) => report?.(status),
    calls: (moment: Moment) => called?.(moment),
  };
}

export function fakeMemory(stored?: unknown): SettingsMemory & { saved: Settings[] } {
  const saved: Settings[] = [];
  return {
    saved,
    load: async () => stored,
    save: async (settings) => {
      saved.push(settings);
    },
  };
}

export function fakeAutostart(enabled = false): Autostart & { wanted: boolean[] } {
  const wanted: boolean[] = [];
  return {
    wanted,
    enabled: async () => enabled,
    set: async (value) => {
      wanted.push(value);
    },
  };
}
