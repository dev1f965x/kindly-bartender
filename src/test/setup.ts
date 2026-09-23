import "@testing-library/jest-dom/vitest";
import { vi } from "vitest";

/**
 * The window API only exists inside a running Tauri shell. Components reach it through
 * `src/shell/window.ts`, which is stubbed here so rendering never touches the real one.
 */
vi.mock("@tauri-apps/api/window", () => ({
  getCurrentWindow: () => ({
    hide: vi.fn(),
    minimize: vi.fn(),
    close: vi.fn(),
  }),
}));
