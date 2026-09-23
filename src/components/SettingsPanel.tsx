import { useId, useState } from "react";
import { INSTALL_FOUND, SETTINGS_LABELS } from "../domain/labels";
import type { Status } from "../domain/watching";
import type { Settings } from "../settings/settings";
import "./SettingsPanel.css";
import { Toggle } from "./Toggle";

interface Props {
  settings: Settings;
  status: Status;
  onChange: (changes: Partial<Settings>) => void;
  /** Left out while the notice above the panel is already offering to find it. */
  onFindInstall?: () => void;
  onTest: () => void;
}

/** How long the test button says it did something, before it offers to do it again. */
const TESTED_FOR_MS = 2000;

/** Everything the player can change, in the order they meet it: the call, then the details. */
export function SettingsPanel({ settings, status, onChange, onFindInstall, onTest }: Props) {
  const [tested, setTested] = useState(false);
  const playerId = useId();
  const installId = useId();

  const test = () => {
    onTest();
    setTested(true);
    setTimeout(() => setTested(false), TESTED_FOR_MS);
  };

  return (
    <section className="settings" aria-labelledby="settings-heading">
      <h2 className="visually-hidden" id="settings-heading">
        {SETTINGS_LABELS.heading}
      </h2>

      <fieldset className="settings__group">
        <div className="settings__heading">
          <legend className="settings__legend">{SETTINGS_LABELS.calls}</legend>
          <button type="button" className="settings__test" onClick={test} aria-live="polite">
            {tested ? SETTINGS_LABELS.tested : SETTINGS_LABELS.test}
          </button>
        </div>
        <Toggle
          label={SETTINGS_LABELS.sound}
          checked={settings.sound}
          onChange={(sound) => onChange({ sound })}
        />
        <Toggle
          label={SETTINGS_LABELS.notification}
          checked={settings.notification}
          onChange={(notification) => onChange({ notification })}
        />
        <Toggle
          label={SETTINGS_LABELS.focus}
          hint={SETTINGS_LABELS.focusHint}
          checked={settings.focus}
          onChange={(focus) => onChange({ focus })}
        />
      </fieldset>

      <div className="settings__field">
        <label className="settings__label" htmlFor={playerId}>
          {SETTINGS_LABELS.player}
        </label>
        <input
          id={playerId}
          className="settings__input"
          value={settings.playerName}
          placeholder={SETTINGS_LABELS.playerPlaceholder}
          onChange={(event) => onChange({ playerName: event.target.value })}
        />
        <p className="settings__hint">{SETTINGS_LABELS.playerHint}</p>
      </div>

      <div className="settings__field">
        <label className="settings__label" htmlFor={installId}>
          {SETTINGS_LABELS.install}
        </label>
        <div className="settings__row">
          <output
            id={installId}
            className="settings__path"
            data-chosen={Boolean(settings.installPath)}
          >
            {settings.installPath || INSTALL_FOUND[status]}
          </output>
          {settings.installPath ? (
            <button
              type="button"
              className="settings__button"
              onClick={() => onChange({ installPath: "" })}
            >
              {SETTINGS_LABELS.installClear}
            </button>
          ) : (
            onFindInstall && (
              <button type="button" className="settings__button" onClick={onFindInstall}>
                {SETTINGS_LABELS.installFind}
              </button>
            )
          )}
        </div>
      </div>

      <div className="settings__group">
        <Toggle
          label={SETTINGS_LABELS.autostart}
          hint={SETTINGS_LABELS.autostartHint}
          checked={settings.autostart}
          onChange={(autostart) => onChange({ autostart })}
        />
      </div>
    </section>
  );
}
