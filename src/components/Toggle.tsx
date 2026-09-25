import { useId } from "react";
import "./Toggle.css";

interface Props {
  label: string;
  hint?: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
}

/** One setting that is either on or off, with room for a line about what it does. */
export function Toggle({ label, hint, checked, onChange }: Props) {
  const id = useId();

  return (
    <div className="toggle">
      <label className="toggle__label" htmlFor={id}>
        {label}
        {hint && <span className="toggle__hint">{hint}</span>}
      </label>
      <input
        id={id}
        className="toggle__input"
        type="checkbox"
        role="switch"
        aria-checked={checked}
        checked={checked}
        onChange={(event) => onChange(event.target.checked)}
      />
    </div>
  );
}
