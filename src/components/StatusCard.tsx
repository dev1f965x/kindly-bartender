import { formatSince, STATUS_LABELS, WINDOW_LABELS } from "../domain/labels";
import { STATUS_TONE, type Status } from "../domain/watching";
import type { Called } from "../shell/useWatching";
import "./StatusCard.css";

interface Props {
  status: Status;
  /** Replaces the status's own line where the window knows something more precise. */
  detail?: string;
  lastCall?: Called;
  now: Date;
}

/** What the app is doing, in one line. */
export function StatusCard({ status, detail, lastCall, now }: Props) {
  const { title, detail: fallback } = STATUS_LABELS[status];

  return (
    <section className="status" data-tone={STATUS_TONE[status]} aria-live="polite">
      <p className="status__state">
        <span className="status__dot" aria-hidden="true" />
        {title}
      </p>
      <p className="status__detail">{detail ?? fallback}</p>
      <p className="status__last">
        {lastCall
          ? WINDOW_LABELS.lastCall(lastCall.moment, formatSince(lastCall.at, now))
          : WINDOW_LABELS.noCallYet}
      </p>
    </section>
  );
}
