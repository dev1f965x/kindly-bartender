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

export const MOMENT_LABELS: Record<Moment, string> = {
  "combat-ended": "전투 종료",
  "gold-arrived": "골드 도착",
};

export const RESTART_NOTICE = {
  title: "하스스톤을 한 번 껐다 켜 주세요",
  detail: "방금 게임의 기록 설정을 켰어요. 재시작해야 기록이 시작돼요",
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
  test: "테스트",
  tested: "불러 봤어요",
  player: "배틀태그 이름",
  playerHint: "# 앞부분만요. 듀오에서 받은 골드를 알아챌 때 써요",
  playerPlaceholder: "예: 바텐더",
  install: "하스스톤 위치",
  installFind: "찾아보기",
  installClear: "지우기",
  installAuto: "자동으로 찾는 중",
  autostart: "윈도우 시작할 때 함께 켜기",
  autostartHint: "트레이에만 조용히 떠요",
};

export const WINDOW_LABELS = {
  close: "닫기",
  closeHint: "창을 닫아도 트레이에서 계속 지켜봐요",
  lastCall: (when: string) => `마지막 호출 ${when}`,
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

/** How long ago, in the words a person would use for a moment that just passed. */
export function formatSince(moment: Date, now: Date): string {
  const seconds = Math.max(0, Math.floor((now.getTime() - moment.getTime()) / 1000));
  if (seconds < 10) return "방금";
  if (seconds < 60) return `${seconds}초 전`;

  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}분 전`;

  const hours = Math.floor(minutes / 60);
  return `${hours}시간 전`;
}
