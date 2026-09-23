import { useEffect, useState } from "react";
import type { Moment, Status } from "../domain/watching";
import type { Bartender } from "./ports";

export interface Called {
  moment: Moment;
  at: Date;
}

/** The watcher's state, and the last time it called the player back. */
export function useWatching(bartender: Bartender) {
  const [status, setStatus] = useState<Status>("waiting-for-game");
  const [lastCall, setLastCall] = useState<Called>();
  const [restartNeeded, setRestartNeeded] = useState(false);

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

  return { status, lastCall, restartNeeded };
}
