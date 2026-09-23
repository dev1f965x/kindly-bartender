import { act, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import App from "./App";
import { fakeAutostart, fakeBartender, fakeMemory } from "./test/fakes";

const now = new Date("2026-09-23T21:00:00+09:00");

interface Options {
  loggingJustStarted?: boolean;
  /** Set where a test cares how long ago something happened. */
  ticking?: boolean;
}

function open({ loggingJustStarted = false, ticking = false }: Options = {}) {
  const fixed = ticking ? undefined : now;
  const { bartender, says, calls } = fakeBartender(loggingJustStarted);
  const memory = fakeMemory();
  const autostart = fakeAutostart();
  render(<App bartender={bartender} memory={memory} autostart={autostart} now={fixed} />);
  return { bartender, memory, autostart, says, calls };
}

describe("App", () => {
  it("waits for the game until the watcher says otherwise", async () => {
    open();

    expect(await screen.findByText("하스스톤을 기다리는 중")).toBeInTheDocument();
    expect(screen.getByText("아직 부른 적 없어요")).toBeInTheDocument();
  });

  it("says it is watching once a log is being read", async () => {
    const { says } = open();
    await screen.findByText("하스스톤을 기다리는 중");

    act(() => says("watching"));

    expect(screen.getByText("지켜보는 중")).toBeInTheDocument();
    expect(screen.getByText("전투가 끝나면 불러 드릴게요")).toBeInTheDocument();
  });

  it("names the last call and when it happened", async () => {
    const { says, calls } = open({ ticking: true });
    act(() => says("watching"));

    act(() => calls("gold-arrived"));

    expect(screen.getByText("방금 골드가 들어와서 불렀어요")).toBeInTheDocument();
  });

  it("asks for the folder when the game cannot be found", async () => {
    const { says, bartender } = open();

    act(() => says("install-not-found"));

    expect(screen.getByText("하스스톤을 못 찾았어요")).toBeInTheDocument();
    await userEvent.click(screen.getAllByRole("button", { name: "찾아보기" })[0]);
    expect(bartender.askForInstallFolder).toHaveBeenCalled();
  });

  it("says what to do about the logging it just turned on", async () => {
    const { says } = open({ loggingJustStarted: true });

    expect(await screen.findByText("기록 설정을 켰어요")).toBeInTheDocument();

    act(() => says("install-not-found"));
    expect(screen.getByText("하스스톤을 한 번 껐다 켜 주세요")).toBeInTheDocument();

    act(() => says("watching"));
    expect(screen.queryByText("하스스톤을 한 번 껐다 켜 주세요")).not.toBeInTheDocument();
  });

  it("saves a switch and hands it to the watcher at once", async () => {
    const { memory, bartender } = open();
    await screen.findByText("하스스톤을 기다리는 중");

    await userEvent.click(screen.getByRole("switch", { name: /소리/ }));

    expect(memory.saved.at(-1)?.sound).toBe(false);
    expect(bartender.apply).toHaveBeenLastCalledWith(expect.objectContaining({ sound: false }));
  });

  it("tries the call out on request", async () => {
    const { bartender } = open();

    await userEvent.click(screen.getByRole("button", { name: "테스트" }));

    expect(bartender.tryTheCall).toHaveBeenCalledOnce();
    expect(screen.getByRole("button", { name: "불러 봤어요" })).toBeInTheDocument();
  });

  it("keeps the player's name for the gold signal", async () => {
    const { memory } = open();
    await screen.findByText("하스스톤을 기다리는 중");

    await userEvent.type(screen.getByLabelText("배틀태그 이름"), "바텐더");

    expect(memory.saved.at(-1)?.playerName).toBe("바텐더");
  });

  it("says the window can be closed", () => {
    open();

    expect(screen.getByText("창을 닫아도 트레이에서 계속 지켜봐요")).toBeInTheDocument();
  });
});
