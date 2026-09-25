import "./design/base.css";
import "./App.css";
import { BrandMark } from "./components/BrandMark";
import { Notice } from "./components/Notice";
import { SettingsPanel } from "./components/SettingsPanel";
import { StatusCard } from "./components/StatusCard";
import { UpdateButton } from "./components/UpdateButton";
import {
  APP_NAME,
  INSTALL_NOTICE,
  RESTART_NOTICE,
  SETTINGS_LABELS,
  WAITING_WHILE_RUNNING,
  WINDOW_LABELS,
} from "./domain/labels";
import { useSettings } from "./settings/useSettings";
import { useNow } from "./shell/clock";
import type { Autostart, Bartender, SettingsMemory } from "./shell/ports";
import { useWatching } from "./shell/useWatching";
import type { UpdateState } from "./update/useUpdate";

export interface AppProps {
  bartender: Bartender;
  memory: SettingsMemory;
  autostart: Autostart;
  update?: UpdateState;
  onInstallUpdate?: () => void;
  /** Fixed by tests; the window reads a ticking clock. */
  now?: Date;
}

/**
 * The whole window: the app's name, what it is doing, what it needs from the player, and
 * the settings.
 *
 * It is open only while something is being changed or checked; the watching happens with
 * it closed (ADR 6).
 */
export default function App({
  bartender,
  memory,
  autostart,
  update = { status: "current" },
  onInstallUpdate = () => {},
  now,
}: AppProps) {
  const clock = useNow();
  const { status, lastCall, restartNeeded, gameRunning } = useWatching(bartender);
  const { settings, change } = useSettings(memory, bartender, autostart);

  // Nothing else the window asks for matters until the game is found.
  const lost = status === "install-not-found";

  const findInstall = () => {
    void bartender.askForInstallFolder().then((folder) => {
      if (folder) change({ installPath: folder });
    });
  };

  return (
    <div className="app">
      <header className="app__bar">
        <BrandMark />
        <div className="app__name">
          <h1 className="app__title">{APP_NAME}</h1>
          <span className="app__version">v{__APP_VERSION__}</span>
        </div>
        <UpdateButton update={update} onInstall={onInstallUpdate} />
      </header>

      <main className="app__main">
        <StatusCard
          status={status}
          detail={status === "waiting-for-game" && gameRunning ? WAITING_WHILE_RUNNING : undefined}
          lastCall={lastCall}
          now={now ?? clock}
        />

        {lost ? (
          <Notice
            title={INSTALL_NOTICE.title}
            detail={INSTALL_NOTICE.detail}
            tone="lost"
            action={{ label: SETTINGS_LABELS.installFind, onAction: findInstall }}
          />
        ) : (
          restartNeeded && (
            <Notice
              title={RESTART_NOTICE[gameRunning ? "running" : "closed"].title}
              detail={RESTART_NOTICE[gameRunning ? "running" : "closed"].detail}
            />
          )
        )}

        <SettingsPanel
          settings={settings}
          status={status}
          onChange={change}
          onFindInstall={findInstall}
          onTest={() => void bartender.tryTheCall()}
        />
      </main>

      <footer className="app__footer">{WINDOW_LABELS.closeHint}</footer>
    </div>
  );
}
