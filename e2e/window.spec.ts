import { expect, test } from "@playwright/test";
import { invoked, openWindow, report, stored } from "./app";

test("the window waits for the game, then says it is watching", async ({ page }) => {
  await openWindow(page);
  await expect(page.getByText("하스스톤 대기 중")).toBeVisible();

  await report(page, "status", "watching");

  await expect(page.getByText("감시 중")).toBeVisible();
  await expect(page.getByText("호출 기록 없음")).toBeVisible();
});

test("a moment is named with the time it happened", async ({ page }) => {
  await openWindow(page);
  await report(page, "status", "watching");

  await report(page, "moment", "combat-ended");

  await expect(page.getByText("방금 전투 종료로 호출")).toBeVisible();
});

test("a switch is saved and handed to the watcher at once", async ({ page }) => {
  await openWindow(page);

  await page.getByRole("switch", { name: /하스스톤 창 전환/ }).click();

  await expect
    .poll(() => stored(page, "settings.json", "settings"))
    .toMatchObject({ focus: false });
  await expect
    .poll(async () => (await invoked(page, "apply_settings")).at(-1))
    .toMatchObject({ args: { settings: { focus: false } } });
});

test("settings from last time are there on launch", async ({ page }) => {
  await openWindow(page, {
    "settings.json": { settings: { playerName: "바텐더", sound: false } },
  });

  await expect(page.getByLabel(/배틀태그 이름/)).toHaveValue("바텐더");
  await expect(page.getByRole("switch", { name: /소리/ })).not.toBeChecked();
});

test("the folder picker fills the install path in", async ({ page }) => {
  await openWindow(page, {}, { installFolder: "D:GamesHearthstone" });
  await report(page, "status", "install-not-found");

  await page.getByRole("button", { name: "찾아보기" }).first().click();

  await expect(page.getByText("D:GamesHearthstone")).toBeVisible();
  await expect(page.getByRole("button", { name: "자동 탐색으로" })).toBeVisible();
});

test("the test button calls the player back", async ({ page }) => {
  await openWindow(page);

  await page.getByRole("button", { name: "호출 시험" }).click();

  await expect(page.getByRole("button", { name: "호출했습니다" })).toBeVisible();
  expect(await invoked(page, "try_the_call")).toHaveLength(1);
});
