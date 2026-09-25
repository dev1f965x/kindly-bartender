mod game;
mod platform;

use std::sync::Mutex;

use serde::{Deserialize, Serialize};
use tauri::menu::{Menu, MenuItem};
use tauri::tray::{MouseButton, MouseButtonState, TrayIconBuilder, TrayIconEvent};
use tauri::{AppHandle, Emitter, Manager, State, WindowEvent};
use tauri_plugin_notification::NotificationExt;

use game::moments::Moment;
use game::watcher::{Report, Status, Watcher};

/// What the player chose, mirrored from the window, which owns it (ADR 7).
#[derive(Debug, Clone, Default, Serialize, Deserialize)]
#[serde(rename_all = "camelCase", default)]
pub struct Settings {
    /// BattleTag name without its `#NNNN`, which the gold signal needs to find the player.
    pub player_name: Option<String>,
    /// Set when the game is somewhere the app cannot find on its own.
    pub install_path: Option<String>,
    pub sound: bool,
    pub notification: bool,
    pub focus: bool,
}

struct AppState {
    settings: Mutex<Settings>,
    watcher: Watcher,
    /// Whether this run is the one that turned the game's logging on, which means the game
    /// has to restart before it writes anything.
    logging_just_started: bool,
}

/// Takes the settings the window holds and puts them to work.
#[tauri::command]
fn apply_settings(settings: Settings, state: State<'_, AppState>) {
    state.watcher.follow_player(settings.player_name.clone());
    state.watcher.look_in(settings.install_path.clone());
    *state.settings.lock().expect("settings") = settings;
}

/// Restores the game's window and puts it in front.
#[tauri::command]
fn bring_game_to_front() -> bool {
    platform::bring_game_to_front()
}

/// Whether the game is running, so the window can say what it is waiting for.
#[tauri::command]
fn game_is_running() -> bool {
    platform::game_is_running()
}

#[tauri::command]
fn logging_just_started(state: State<'_, AppState>) -> bool {
    state.logging_just_started
}

/// Plays the call the player would get, without bringing the game forward, which would
/// bury this window.
#[tauri::command]
fn try_the_call(app: AppHandle, state: State<'_, AppState>) {
    let settings = Settings {
        focus: false,
        ..state.settings.lock().expect("settings").clone()
    };
    call_the_player(&app, &settings, Moment::CombatEnded);
}

fn call_the_player(app: &AppHandle, settings: &Settings, moment: Moment) {
    if settings.sound {
        platform::play_alert();
    }
    if settings.notification {
        let _ = app
            .notification()
            .builder()
            .title(match moment {
                Moment::CombatEnded => "전투가 끝났어요",
                Moment::GoldArrived => "골드가 들어왔어요",
            })
            .body("상점으로 돌아가세요")
            .show();
    }
    if settings.focus {
        platform::bring_game_to_front();
    }
    let _ = app.emit("moment", moment);
}

fn build_tray(app: &AppHandle) -> tauri::Result<()> {
    let open = MenuItem::with_id(app, "open", "창 열기", true, None::<&str>)?;
    let quit = MenuItem::with_id(app, "quit", "종료", true, None::<&str>)?;
    let menu = Menu::with_items(app, &[&open, &quit])?;

    TrayIconBuilder::with_id("tray")
        .icon(app.default_window_icon().expect("window icon").clone())
        .tooltip("Kindly Bartender")
        .menu(&menu)
        .show_menu_on_left_click(false)
        .on_menu_event(|app, event| match event.id.as_ref() {
            "open" => show_window(app),
            "quit" => app.exit(0),
            _ => {}
        })
        .on_tray_icon_event(|tray, event| {
            if let TrayIconEvent::Click {
                button: MouseButton::Left,
                button_state: MouseButtonState::Up,
                ..
            } = event
            {
                show_window(tray.app_handle());
            }
        })
        .build(app)?;

    Ok(())
}

fn show_window(app: &AppHandle) {
    if let Some(window) = app.get_webview_window("main") {
        let _ = window.show();
        let _ = window.unminimize();
        let _ = window.set_focus();
    }
}

/// The tray's tooltip is all the app says while the window is closed.
fn describe(status: Status) -> &'static str {
    match status {
        Status::Watching => "Kindly Bartender — 지켜보는 중",
        Status::WaitingForGame => "Kindly Bartender — 하스스톤을 기다리는 중",
        Status::InstallNotFound => "Kindly Bartender — 하스스톤을 못 찾았어요",
    }
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    let mut builder = tauri::Builder::default()
        .plugin(tauri_plugin_store::Builder::new().build())
        .plugin(tauri_plugin_notification::init())
        .plugin(tauri_plugin_dialog::init());

    #[cfg(desktop)]
    {
        builder = builder
            .plugin(tauri_plugin_process::init())
            .plugin(tauri_plugin_updater::Builder::new().build())
            .plugin(tauri_plugin_autostart::init(
                tauri_plugin_autostart::MacosLauncher::LaunchAgent,
                Some(vec!["--hidden"]),
            ));
    }

    builder
        .invoke_handler(tauri::generate_handler![
            apply_settings,
            bring_game_to_front,
            game_is_running,
            logging_just_started,
            try_the_call
        ])
        .setup(|app| {
            let handle = app.handle().clone();
            let watcher = Watcher::start(move |report| match report {
                Report::Status(status) => {
                    if let Some(tray) = handle.tray_by_id("tray") {
                        let _ = tray.set_tooltip(Some(describe(status)));
                    }
                    let _ = handle.emit("status", status);
                }
                Report::Moment(moment) => {
                    let settings = handle
                        .state::<AppState>()
                        .settings
                        .lock()
                        .expect("settings")
                        .clone();
                    call_the_player(&handle, &settings, moment);
                }
            });

            app.manage(AppState {
                settings: Mutex::new(Settings::default()),
                watcher,
                // The game writes nothing until this is set, and only after a restart.
                logging_just_started: game::logging::start_logging(),
            });

            build_tray(app.handle())?;

            // The window is configured hidden, so Windows starting the app shows nothing.
            // A player starting it wants to see it.
            if !std::env::args().any(|argument| argument == "--hidden") {
                show_window(app.handle());
            }

            Ok(())
        })
        .on_window_event(|window, event| {
            // Closing the window leaves the app in the tray (ADR 6).
            if let WindowEvent::CloseRequested { api, .. } = event {
                api.prevent_close();
                let _ = window.hide();
            }
        })
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
