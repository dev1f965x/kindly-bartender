import { mkdirSync } from "node:fs";
import { chromium } from "@playwright/test";

/**
 * Draws the app's icon from the same mark the window shows, at the size Tauri's icon
 * generator wants, and leaves it for `npx tauri icon` to cut into every format.
 *
 *   npm run art:icon   → src-tauri/icons/source.png
 */
const SIZE = 1024;
const OUT = "src-tauri/icons";

const mark = `
<!doctype html>
<html>
  <body style="margin:0">
    <svg xmlns="http://www.w3.org/2000/svg" width="${SIZE}" height="${SIZE}" viewBox="0 0 40 40">
      <defs>
        <linearGradient id="wood" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stop-color="#241d19" />
          <stop offset="1" stop-color="#14110f" />
        </linearGradient>
      </defs>
      <rect width="40" height="40" rx="9" fill="url(#wood)" />
      <g fill="none" stroke="#e8b06a" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
        <path d="M11 13h18l-9 9z" />
        <path d="M20 22v6" />
        <path d="M15.5 28h9" />
      </g>
      <circle cx="23" cy="15.2" r="1.8" fill="#9bbf6a" />
    </svg>
  </body>
</html>`;

mkdirSync(OUT, { recursive: true });

const browser = await chromium.launch({ channel: "msedge" });
const page = await browser.newPage({ viewport: { width: SIZE, height: SIZE } });
await page.setContent(mark);
await page.locator("svg").screenshot({ path: `${OUT}/source.png`, omitBackground: true });
await browser.close();

console.log(`${OUT}/source.png`);
