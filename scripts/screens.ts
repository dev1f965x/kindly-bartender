import { mkdirSync, rmSync } from "node:fs";
import { chromium, type Page } from "@playwright/test";
import { createServer } from "vite";
import { type Shell, type Stores, tauriStandIn } from "../e2e/tauri";

/**
 * Photographs every state of the window, for design review and the README.
 *
 * The window is a web page drawn by WebView2, which is Chromium, so Edge renders it the
 * same way. Tauri's IPC is answered by the same stand-in the end-to-end tests use, which
 * lets each shot start from the settings and the watcher state it needs.
 *
 *   npm run screens        → screens/*.png
 */
const WINDOW = { width: 460, height: 680 };
const SMALLEST = { width: 400, height: 560 };
const PORT = 1430;
const OUT = "screens";

interface Shot {
  name: string;
  stores?: Stores;
  shell?: Shell;
  act?: (page: Page) => Promise<void>;
  viewport?: { width: number; height: number };
}

const report = (event: "status" | "moment", payload: string) => (page: Page) =>
  page.evaluate(
    ([event, payload]) =>
      (window as unknown as { __report: (event: string, payload: string) => void }).__report(
        event,
        payload,
      ),
    [event, payload],
  );

const watching = report("status", "watching");
const settled: Stores = { "settings.json": { settings: { playerName: "바텐더" } } };

const SHOTS: Shot[] = [
  { name: "waiting" },
  { name: "watching", stores: settled, act: watching },
  {
    name: "called",
    stores: settled,
    act: async (page) => {
      await watching(page);
      await report("moment", "combat-ended")(page);
    },
  },
  {
    name: "install-not-found",
    act: report("status", "install-not-found"),
  },
  {
    name: "restart-needed",
    shell: { loggingJustStarted: true },
  },
  {
    name: "install-chosen",
    stores: { "settings.json": { settings: { installPath: "D:\\Games\\Hearthstone" } } },
    act: watching,
  },
  {
    name: "tested",
    stores: settled,
    act: async (page) => {
      await watching(page);
      await page.getByRole("button", { name: "테스트" }).click();
    },
  },
  { name: "small-watching", stores: settled, act: watching, viewport: SMALLEST },
  { name: "small-waiting", viewport: SMALLEST },
];

async function main() {
  rmSync(OUT, { recursive: true, force: true });
  mkdirSync(OUT, { recursive: true });
  const server = await createServer({
    server: { port: PORT, strictPort: true },
    logLevel: "error",
  });
  await server.listen();
  const browser = await chromium.launch({ channel: "msedge" });

  try {
    for (const shot of SHOTS) {
      const page = await browser.newPage({
        viewport: shot.viewport ?? WINDOW,
        deviceScaleFactor: 2,
      });
      await page.addInitScript({ content: tauriStandIn(shot.stores ?? {}, shot.shell ?? {}) });
      await page.goto(`http://localhost:${PORT}`);
      await page.getByRole("heading", { name: "Kindly Bartender" }).waitFor();
      await shot.act?.(page);
      // Park the pointer where nothing reacts to it, so no hover state is photographed.
      await page.mouse.move(1, (shot.viewport ?? WINDOW).height - 1);
      await page.waitForTimeout(300);
      await page.screenshot({ path: `${OUT}/${shot.name}.png` });
      await page.close();
      console.log(`${OUT}/${shot.name}.png`);
    }
  } finally {
    await browser.close();
    await server.close();
  }
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
