import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App";
import { storeSettings, tauriBartender, windowsAutostart } from "./shell/tauri";
import { tauriUpdateSource } from "./update/tauri";
import { useUpdate } from "./update/useUpdate";

function Window() {
  const { update, install } = useUpdate(tauriUpdateSource);

  return (
    <App
      bartender={tauriBartender}
      memory={storeSettings}
      autostart={windowsAutostart}
      update={update}
      onInstallUpdate={() => void install()}
    />
  );
}

const root = document.getElementById("root");
if (!root) throw new Error("index.html is missing #root");

ReactDOM.createRoot(root).render(
  <React.StrictMode>
    <Window />
  </React.StrictMode>,
);
