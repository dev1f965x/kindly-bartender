import { describe, expect, it } from "vitest";
import { formatSince } from "./labels";

describe("formatSince", () => {
  const now = new Date("2026-09-23T21:00:00+09:00");
  const ago = (seconds: number) => new Date(now.getTime() - seconds * 1000);

  it("calls the last few seconds just now", () => {
    expect(formatSince(ago(0), now)).toBe("방금");
    expect(formatSince(ago(9), now)).toBe("방금");
  });

  it("counts seconds, then minutes, then hours", () => {
    expect(formatSince(ago(10), now)).toBe("10초 전");
    expect(formatSince(ago(59), now)).toBe("59초 전");
    expect(formatSince(ago(60), now)).toBe("1분 전");
    expect(formatSince(ago(59 * 60), now)).toBe("59분 전");
    expect(formatSince(ago(60 * 60), now)).toBe("1시간 전");
  });

  it("never counts backwards when the clock jumps", () => {
    expect(formatSince(new Date(now.getTime() + 5000), now)).toBe("방금");
  });
});
