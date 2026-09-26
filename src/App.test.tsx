import { act, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import App from "./App";
import { fakeAutostart, fakeBartender, fakeMemory } from "./test/fakes";

const now = new Date("2026-09-23T21:00:00+09:00");

interface Options {
  loggingJustStarted?: boolean;
  gameRunning?: boolean;
  /** Set where a test cares how long ago something happened. */
  ticking?: boolean;
}

function open({ loggingJustStarted = false, gameRunning = false, ticking = false }: Options = {}) {
  const fixed = ticking ? undefined : now;
  const { bartender, says, calls } = fakeBartender(loggingJustStarted, gameRunning);
  const memory = fakeMemory();
  const autostart = fakeAutostart();
  render(<App bartender={bartender} memory={memory} autostart={autostart} now={fixed} />);
  return { bartender, memory, autostart, says, calls };
}

describe("App", () => {
  it("waits for the game until the watcher says otherwise", async () => {
    open();

    expect(await screen.findByText("하스스톤 대기 중")).toBeInTheDocument();
    expect(screen.getByText("호출 기록 없음")).toBeInTheDocument();
  });

  it("says it is watching once a log is being read", async () => {
    const { says } = open();
    await screen.findByText("하스스톤 대기 중");

    act(() => says("watching"));

    expect(screen.getByText("감시 중")).toBeInTheDocument();
    expect(screen.getByText("전투가 끝나면 호출합니다")).toBeInTheDocument();
  });

  it("names the last call and when it happened", async () => {
    const { says, calls } = open({ ticking: true });
    act(() => says("watching"));

    act(() => calls("gold-arrived"));

    expect(screen.getByText("방금 골드 수신으로 호출")).toBeInTheDocument();
  });

  it("asks for the folder when the game cannot be found", async () => {
    const { says, bartender } = open();

    act(() => says("install-not-found"));

    expect(screen.getByText("하스스톤을 찾지 못했습니다")).toBeInTheDocument();
    await userEvent.click(screen.getAllByRole("button", { name: "찾아보기" })[0]);
    expect(bartender.askForInstallFolder).toHaveBeenCalled();
  });

  it("tells a closed game to start and a running one to restart", async () => {
    const { says } = open({ loggingJustStarted: true });

    expect(await screen.findByText("기록 설정 적용됨")).toBeInTheDocument();

    act(() => says("watching"));
    expect(screen.queryByText("기록 설정 적용됨")).not.toBeInTheDocument();
  });

  it("asks a running game for a restart, since it read the setting at launch", async () => {
    open({ loggingJustStarted: true, gameRunning: true });

    expect(await screen.findByText("하스스톤 재시작 필요")).toBeInTheDocument();
    expect(screen.getByText("하스스톤을 재시작해야 기록이 시작됩니다")).toBeInTheDocument();
  });

  it("puts finding the game before anything else it might ask", async () => {
    const { says } = open({ loggingJustStarted: true, gameRunning: true });
    await screen.findByText("하스스톤 재시작 필요");

    act(() => says("install-not-found"));

    expect(screen.getByText("설치 폴더 지정 필요")).toBeInTheDocument();
    expect(screen.queryByText("하스스톤 재시작 필요")).not.toBeInTheDocument();
  });

  it("saves a switch and hands it to the watcher at once", async () => {
    const { memory, bartender } = open();
    await screen.findByText("하스스톤 대기 중");

    await userEvent.click(screen.getByRole("switch", { name: /소리/ }));

    expect(memory.saved.at(-1)?.sound).toBe(false);
    expect(bartender.apply).toHaveBeenLastCalledWith(expect.objectContaining({ sound: false }));
  });

  it("tries the call out on request", async () => {
    const { bartender } = open();

    await userEvent.click(screen.getByRole("button", { name: "호출 시험" }));

    expect(bartender.tryTheCall).toHaveBeenCalledOnce();
    expect(screen.getByRole("button", { name: "호출했습니다" })).toBeInTheDocument();
  });

  it("keeps the player's name for the gold signal", async () => {
    const { memory } = open();
    await screen.findByText("하스스톤 대기 중");

    await userEvent.type(screen.getByLabelText(/배틀태그 이름/), "바텐더");

    expect(memory.saved.at(-1)?.playerName).toBe("바텐더");
  });

  it("says the window can be closed", () => {
    open();

    expect(screen.getByText("창을 닫아도 트레이에서 감시합니다")).toBeInTheDocument();
  });
});
