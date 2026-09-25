import { useEffect, useState } from "react";
import type { Moment, Status } from "../domain/watching";
import type { Bartender } from "./ports";

export interface Called {
  moment: Moment;
  at: Date;
}

/** How often the window re-asks whether the game is up, while it is not being read. */
const ASK_AGAIN_MS = 5_000;

/** The watcher's state, and the last time it called the player back. */
export function useWatching(bartender: Bartender) {
  const [status, setStatus] = useState<Status>("waiting-for-game");
  const [lastCall, setLastCall] = useState<Called>();
  const [restartNeeded, setRestartNeeded] = useState(false);
  const [gameRunning, setGameRunning] = useState(false);

  useEffect(() => {
    let stop: (() => void) | undefined;
    let cancelled = false;

    void bartender
      .watch(setStatus, (moment) => setLastCall({ moment, at: new Date() }))
      .then((unwatch) => {
        if (cancelled) unwatch();
        else stop = unwatch;
      });
    void bartender.loggingJustStarted().then((started) => {
      if (!cancelled) setRestartNeeded(started);
    });

    return () => {
      cancelled = true;
      stop?.();
    };
  }, [bartender]);

  // Once a log is being read, the restart it asked for has happened.
  useEffect(() => {
    if (status === "watching") setRestartNeeded(false);
  }, [status]);

  // A game running while nothing is being read has to restart, so the question is asked
  // again until it does.
  useEffect(() => {
    if (status === "watching") return;

    let cancelled = false;
    const ask = () => {
      void bartender.gameIsRunning().then((running) => {
        if (!cancelled) setGameRunning(running);
      });
    };

    ask();
    const timer = setInterval(ask, ASK_AGAIN_MS);
    return () => {
      cancelled = true;
      clearInterval(timer);
    };
  }, [bartender, status]);

  return { status, lastCall, restartNeeded, gameRunning };
}
