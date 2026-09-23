//! The two things this app needs from Windows itself: where the running game lives, and
//! how to put it back in front of the player.

#[cfg(windows)]
mod windows_impl {
    use std::path::PathBuf;

    use windows::core::{w, PWSTR};
    use windows::Win32::Foundation::{CloseHandle, HWND, MAX_PATH};
    use windows::Win32::System::Diagnostics::Debug::MessageBeep;
    use windows::Win32::System::Diagnostics::ToolHelp::{
        CreateToolhelp32Snapshot, Process32FirstW, Process32NextW, PROCESSENTRY32W,
        TH32CS_SNAPPROCESS,
    };
    use windows::Win32::System::Threading::{
        OpenProcess, QueryFullProcessImageNameW, PROCESS_NAME_FORMAT,
        PROCESS_QUERY_LIMITED_INFORMATION,
    };
    use windows::Win32::UI::WindowsAndMessaging::{
        FindWindowW, SetForegroundWindow, ShowWindow, MB_ICONEXCLAMATION, SW_RESTORE,
    };

    const GAME_EXE: &str = "Hearthstone.exe";
    /// The game's window carries its name as both class and title.
    const GAME_WINDOW: windows::core::PCWSTR = w!("Hearthstone");

    /// The folder the running game was started from, if it is running.
    pub fn running_install() -> Option<PathBuf> {
        let process = find_process(GAME_EXE)?;
        let handle =
            unsafe { OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, process) }.ok()?;

        let mut buffer = [0u16; MAX_PATH as usize];
        let mut length = buffer.len() as u32;
        let queried = unsafe {
            QueryFullProcessImageNameW(
                handle,
                PROCESS_NAME_FORMAT(0),
                PWSTR(buffer.as_mut_ptr()),
                &mut length,
            )
        };
        unsafe { CloseHandle(handle) }.ok()?;
        queried.ok()?;

        let exe = PathBuf::from(String::from_utf16_lossy(&buffer[..length as usize]));
        exe.parent().map(PathBuf::from)
    }

    pub fn game_is_running() -> bool {
        find_process(GAME_EXE).is_some()
    }

    /// Restores the game's window and puts it in front, the way Alt+Tab would.
    pub fn bring_game_to_front() -> bool {
        let Ok(window) = (unsafe { FindWindowW(GAME_WINDOW, None) }) else {
            return false;
        };
        if window == HWND(std::ptr::null_mut()) {
            return false;
        }

        unsafe {
            let _ = ShowWindow(window, SW_RESTORE);
            SetForegroundWindow(window).as_bool()
        }
    }

    /// The system's exclamation sound, so the app carries no audio of its own.
    pub fn play_alert() {
        unsafe {
            let _ = MessageBeep(MB_ICONEXCLAMATION);
        }
    }

    fn find_process(exe: &str) -> Option<u32> {
        let snapshot = unsafe { CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0) }.ok()?;
        let mut entry = PROCESSENTRY32W {
            dwSize: std::mem::size_of::<PROCESSENTRY32W>() as u32,
            ..Default::default()
        };

        let mut found = None;
        if unsafe { Process32FirstW(snapshot, &mut entry) }.is_ok() {
            loop {
                let name = String::from_utf16_lossy(&entry.szExeFile);
                if name.trim_end_matches('\0').eq_ignore_ascii_case(exe) {
                    found = Some(entry.th32ProcessID);
                    break;
                }
                if unsafe { Process32NextW(snapshot, &mut entry) }.is_err() {
                    break;
                }
            }
        }
        unsafe { CloseHandle(snapshot) }.ok()?;
        found
    }
}

#[cfg(not(windows))]
mod windows_impl {
    use std::path::PathBuf;

    pub fn running_install() -> Option<PathBuf> {
        None
    }

    pub fn game_is_running() -> bool {
        false
    }

    pub fn bring_game_to_front() -> bool {
        false
    }

    pub fn play_alert() {}
}

pub use windows_impl::{bring_game_to_front, game_is_running, play_alert, running_install};
