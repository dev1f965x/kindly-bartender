/** Store files by name, each a map of keys to values, as the store plugin keeps them. */
export type Stores = Record<string, Record<string, unknown>>;

/** What the stand-in answers for the commands this app calls. */
export interface Shell {
  /** Whether this run turned the game's logging on. */
  loggingJustStarted?: boolean;
  /** Whether Windows already starts the app. */
  autostart?: boolean;
  /** Whether Hearthstone is running. */
  gameRunning?: boolean;
  /** What the folder picker gives back. */
  installFolder?: string;
}

/**
 * A stand-in for Tauri's IPC, as script text to run in the page before the app loads.
 *
 * The store lives in memory, seeded from `stores`, and the Rust commands answer from
 * `shell`. Every call is kept on `window.__invokes` and the files on `window.__stores`,
 * and `window.__report(event, payload)` plays the watcher's events back into the page,
 * which is how a test puts the window into a state.
 */
export function tauriStandIn(stores: Stores = {}, shell: Shell = {}): string {
  return `(() => {
    const data = ${JSON.stringify(stores)};
    const shell = ${JSON.stringify(shell)};
    const files = new Map();
    const invokes = [];
    const listeners = new Map();
    let next = 1;

    const invoke = async (command, args = {}) => {
      invokes.push({ command, args });

      if (command === "plugin:event|listen") {
        listeners.set(args.event, [...(listeners.get(args.event) ?? []), args.handler]);
        return listeners.get(args.event).length;
      }
      if (command === "plugin:store|load") {
        data[args.path] ??= {};
        files.set(next, args.path);
        return next++;
      }
      if (command === "plugin:store|get") {
        const file = data[files.get(args.rid)] ?? {};
        return [file[args.key] ?? null, args.key in file];
      }
      if (command === "plugin:store|set") {
        const file = data[files.get(args.rid)] ?? {};
        file[args.key] = args.value;
        return null;
      }
      if (command === "plugin:dialog|open") return shell.installFolder ?? null;
      if (command === "plugin:autostart|is_enabled") return Boolean(shell.autostart);
      if (command === "plugin:autostart|enable") { shell.autostart = true; return null; }
      if (command === "plugin:autostart|disable") { shell.autostart = false; return null; }
      if (command === "logging_just_started") return Boolean(shell.loggingJustStarted);
      if (command === "game_is_running") return Boolean(shell.gameRunning);
      return null;
    };

    window.__invokes = invokes;
    window.__stores = data;
    window.__report = (event, payload) => {
      for (const handler of listeners.get(event) ?? []) {
        window[\`_\${handler}\`]?.({ event, id: handler, payload });
      }
    };
    // The event plugin unhooks its listeners through this when a component goes away.
    window.__TAURI_EVENT_PLUGIN_INTERNALS__ = { unregisterListener: () => {} };
    window.__TAURI_INTERNALS__ = {
      invoke,
      transformCallback: (callback) => {
        const id = next++;
        window[\`_\${id}\`] = callback;
        return id;
      },
      metadata: { currentWindow: { label: "main" }, currentWebview: { label: "main" } },
    };
  })();`;
}
