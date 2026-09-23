import { formatSince, MOMENT_LABELS, STATUS_LABELS, WINDOW_LABELS } from "../domain/labels";
import { STATUS_TONE, type Status } from "../domain/watching";
import type { Called } from "../shell/useWatching";
import "./StatusCard.css";

interface Props {
  status: Status;
  lastCall?: Called;
  now: Date;
}

/** What the app is doing, in the one line the player opens the window to read. */
export function StatusCard({ status, lastCall, now }: Props) {
  const { title, detail } = STATUS_LABELS[status];

  return (
    <section className="status" data-tone={STATUS_TONE[status]} aria-live="polite">
      <p className="status__state">
        <span className="status__dot" aria-hidden="true" />
        {title}
      </p>
      <p className="status__detail">{detail}</p>
      <p className="status__last">
        {lastCall
          ? `${MOMENT_LABELS[lastCall.moment]} · ${WINDOW_LABELS.lastCall(formatSince(lastCall.at, now))}`
          : WINDOW_LABELS.noCallYet}
      </p>
    </section>
  );
}
