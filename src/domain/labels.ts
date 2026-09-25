import type { Moment, Status } from "./watching";

export const APP_NAME = "Kindly Bartender";

export const STATUS_LABELS: Record<Status, { title: string; detail: string }> = {
  watching: {
    title: "지켜보는 중",
    detail: "전투가 끝나면 불러 드릴게요",
  },
  "waiting-for-game": {
    title: "하스스톤을 기다리는 중",
    detail: "게임을 켜면 바로 지켜봐요",
  },
  "install-not-found": {
    title: "하스스톤을 못 찾았어요",
    detail: "설치 위치를 직접 골라 주세요",
  },
};

/** The game is running but nothing is arriving, meaning it started before logging was on. */
export const WAITING_WHILE_RUNNING = "하스스톤을 껐다 켜면 기록이 시작돼요";

/** The game reads the logging setting once, at start, so what to ask depends on whether
 * it is running. */
export const RESTART_NOTICE = {
  running: {
    title: "하스스톤을 한 번 껐다 켜 주세요",
    detail: "방금 기록 설정을 켰어요. 재시작해야 기록이 시작돼요",
  },
  closed: {
    title: "기록 설정을 켰어요",
    detail: "다음에 하스스톤을 켜면 바로 지켜볼게요",
  },
};

export const INSTALL_NOTICE = {
  title: "설치 폴더를 알려 주세요",
  detail: "Hearthstone.exe가 들어 있는 폴더예요",
};

export const SETTINGS_LABELS = {
  heading: "설정",
  calls: "불러 주는 방법",
  sound: "소리",
  notification: "알림",
  focus: "하스스톤 창 앞으로",
  focusHint: "보던 화면에서 게임으로 바로 넘어가요",
  test: "불러 보기",
  tested: "불러 봤어요",
  testScope: "소리와 알림만 들려 드려요",
  player: "배틀태그 이름 (듀오에서만 써요)",
  playerHint: "듀오에서 파트너가 준 골드까지 알려 드려요. 배틀태그의 # 앞 이름만 적어 주세요",
  playerPlaceholder: "예: 바텐더",
  install: "하스스톤 위치",
  installFind: "찾아보기",
  installAgain: "다시 고르기",
  installClear: "자동으로 되돌리기",
  autostart: "윈도우 시작할 때 함께 켜기",
  autostartHint: "창 없이 트레이에서 시작해요",
};

/** Where the app is looking, when the player has not chosen a folder. */
export const INSTALL_FOUND: Record<Status, string> = {
  watching: "자동으로 찾았어요",
  "waiting-for-game": "자동으로 찾는 중",
  "install-not-found": "아직 못 찾았어요",
};

export const WINDOW_LABELS = {
  closeHint: "창을 닫아도 트레이에서 계속 지켜봐요",
  /** The card's line about the last call, written as a sentence. */
  lastCall: (moment: Moment, when: string) =>
    moment === "combat-ended"
      ? `${when} 전투가 끝나서 불렀어요`
      : `${when} 골드가 들어와서 불렀어요`,
  noCallYet: "아직 부른 적 없어요",
};

export const UPDATE_LABELS = {
  available: (version: string) => `${version} 버전이 나왔어요`,
  install: "업데이트",
  downloading: (version: string) => `${version} 버전을 받고 있어요`,
  installing: "받는 중",
  progress: (progress: number | null) =>
    progress === null ? "…" : ` ${Math.round(progress * 100)}%`,
  failed: "업데이트하지 못했어요",
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
