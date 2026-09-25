import { useEffect, useState } from "react";

/** A second, which is as fine as the relative times shown ever need to be. */
const TICK_MS = 1_000;

/**
 * The current time, re-read on a timer, so a relative time updates in a window left open.
 */
export function useNow(interval: number = TICK_MS): Date {
  const [now, setNow] = useState(() => new Date());

  useEffect(() => {
    const id = setInterval(() => setNow(new Date()), interval);
    return () => clearInterval(id);
  }, [interval]);

  return now;
}
