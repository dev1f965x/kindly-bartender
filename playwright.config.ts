import { defineConfig } from "@playwright/test";

const PORT = 1440;
const CI = Boolean(process.env.CI);

/**
 * End-to-end tests of the window's web layer in a real Chromium, with Tauri answered by a
 * stand-in (e2e/tauri.ts). Locally they drive the installed Edge; CI installs Chromium.
 * The native shell — tray, focus, install, update — is outside their reach and is checked
 * by hand before a release (ADR 9).
 */
export default defineConfig({
  testDir: "e2e",
  fullyParallel: true,
  forbidOnly: CI,
  retries: CI ? 1 : 0,
  reporter: CI ? "github" : "list",
  use: {
    baseURL: `http://localhost:${PORT}`,
    channel: CI ? undefined : "msedge",
    viewport: { width: 460, height: 680 },
    trace: "retain-on-failure",
  },
  webServer: {
    command: `npx vite --port ${PORT} --strictPort`,
    url: `http://localhost:${PORT}`,
    reuseExistingServer: !CI,
  },
});
