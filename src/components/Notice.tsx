import "./Notice.css";

interface Props {
  title: string;
  detail: string;
  action?: { label: string; onAction: () => void };
}

/** Something the app needs from the player before it can do its job. */
export function Notice({ title, detail, action }: Props) {
  return (
    <aside className="notice" role="status">
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
