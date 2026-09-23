import "./Notice.css";

interface Props {
  title: string;
  detail: string;
  /** "waiting" for something the player will get to, "lost" for something in the way. */
  tone?: "waiting" | "lost";
  action?: { label: string; onAction: () => void };
}

/** Something the app needs from the player before it can do its job. */
export function Notice({ title, detail, tone = "waiting", action }: Props) {
  return (
    <aside className="notice" data-tone={tone} role="status">
      <div>
        <p className="notice__title">{title}</p>
        <p className="notice__detail">{detail}</p>
      </div>
      {action && (
        <button type="button" className="notice__action" onClick={action.onAction}>
          {action.label}
        </button>
      )}
    </aside>
  );
}
