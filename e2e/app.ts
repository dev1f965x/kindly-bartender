import type { Page } from "@playwright/test";
import { type Shell, type Stores, tauriStandIn } from "./tauri";

/** Opens the window with the store holding `stores` and the shell answering `shell`. */
export async function openWindow(page: Page, stores: Stores = {}, shell: Shell = {}) {
  await page.addInitScript({ content: tauriStandIn(stores, shell) });
  await page.goto("/");
  await page.getByRole("heading", { name: "Kindly Bartender" }).waitFor();
}

/** Plays one of the watcher's events back into the page. */
export function report(page: Page, event: "status" | "moment", payload: string) {
  return page.evaluate(
    ([event, payload]) =>
      (window as unknown as { __report: (event: string, payload: string) => void }).__report(
        event,
        payload,
      ),
    [event, payload],
  );
}

/** What the app wrote under `key` in the store file `file`. */
export function stored(page: Page, file: string, key: string) {
  return page.evaluate(
    ([file, key]) =>
      (window as unknown as { __stores: Record<string, Record<string, unknown>> }).__stores[file]?.[
        key
      ],
    [file, key],
  );
}

/** Every command the app invoked, oldest first. */
export function invoked(page: Page, command: string) {
  return page.evaluate(
    (command) =>
      (window as unknown as { __invokes: { command: string; args: unknown }[] }).__invokes.filter(
        (call) => call.command === command,
      ),
    command,
  );
}
