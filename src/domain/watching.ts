/** What the app is doing, as the watcher in Rust reports it. */
export type Status = "watching" | "waiting-for-game" | "install-not-found";

/** A moment the app calls the player back for. */
export type Moment = "combat-ended" | "gold-arrived";

/** The colour a status carries, as a token name. */
export const STATUS_TONE: Record<Status, string> = {
  watching: "watching",
  "waiting-for-game": "waiting",
  "install-not-found": "lost",
};
