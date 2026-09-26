import type { Moment, Status } from "./watching";

export const APP_NAME = "Kindly Bartender";

export const STATUS_LABELS: Record<Status, { title: string; detail: string }> = {
  watching: {
    title: "감시 중",
    detail: "전투가 끝나면 호출합니다",
  },
  "waiting-for-game": {
    title: "하스스톤 대기 중",
    detail: "게임이 실행되면 감시를 시작합니다",
  },
  "install-not-found": {
    title: "하스스톤을 찾지 못했습니다",
    detail: "설치 위치를 지정하십시오",
  },
};

/** The game is running but nothing is arriving, meaning it started before logging was on. */
export const WAITING_WHILE_RUNNING = "하스스톤을 재시작해야 기록이 시작됩니다";

/** The game reads the logging setting once, at start, so what to ask depends on whether
 * it is running. */
export const RESTART_NOTICE = {
  running: {
    title: "하스스톤 재시작 필요",
    detail: "기록 설정을 적용했습니다. 재시작 후 기록이 시작됩니다",
  },
  closed: {
    title: "기록 설정 적용됨",
    detail: "다음 실행부터 감시합니다",
  },
};

export const INSTALL_NOTICE = {
  title: "설치 폴더 지정 필요",
  detail: "Hearthstone.exe가 있는 폴더입니다",
};

export const SETTINGS_LABELS = {
  heading: "설정",
  calls: "호출 방식",
  sound: "소리",
  notification: "알림",
  focus: "하스스톤 창 전환",
  focusHint: "보던 화면에서 게임으로 전환합니다",
  test: "호출 시험",
  tested: "호출했습니다",
  testScope: "소리와 알림만 동작합니다",
  player: "배틀태그 이름 (듀오 전용)",
  playerHint: "듀오에서 파트너가 보낸 골드도 호출합니다. 배틀태그의 # 앞 이름만 입력하십시오",
  playerPlaceholder: "바텐더",
  install: "하스스톤 위치",
  installFind: "찾아보기",
  installAgain: "다시 지정",
  installClear: "자동 탐색으로",
  autostart: "윈도우 시작 시 실행",
  autostartHint: "창 없이 트레이에서 시작합니다",
};

/** Where the app is looking, when the player has not chosen a folder. */
export const INSTALL_FOUND: Record<Status, string> = {
  watching: "자동 탐색됨",
  "waiting-for-game": "자동 탐색 중",
  "install-not-found": "탐색 실패",
};

export const WINDOW_LABELS = {
  closeHint: "창을 닫아도 트레이에서 감시합니다",
  /** The card's line about the last call, written as a sentence. */
  lastCall: (moment: Moment, when: string) =>
    moment === "combat-ended" ? `${when} 전투 종료로 호출` : `${when} 골드 수신으로 호출`,
  noCallYet: "호출 기록 없음",
};

export const UPDATE_LABELS = {
  available: (version: string) => `${version} 사용 가능`,
  install: "업데이트",
  downloading: (version: string) => `${version} 내려받는 중`,
  installing: "내려받는 중",
  progress: (progress: number | null) =>
    progress === null ? "…" : ` ${Math.round(progress * 100)}%`,
  failed: "업데이트 실패",
  retry: "다시 시도",
};

/** How long ago, rounded to the unit it reads best in. */
export function formatSince(moment: Date, now: Date): string {
  const seconds = Math.max(0, Math.floor((now.getTime() - moment.getTime()) / 1000));
  if (seconds < 10) return "방금";
  if (seconds < 60) return `${seconds}초 전`;

  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}분 전`;

  const hours = Math.floor(minutes / 60);
  return `${hours}시간 전`;
}
