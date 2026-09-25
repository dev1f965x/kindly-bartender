use std::path::{Path, PathBuf};

/// Where Battle.net puts the game unless the player says otherwise.
const USUAL_PATHS: [&str; 2] = [
    r"C:\Program Files (x86)\Hearthstone",
    r"C:\Program Files\Hearthstone",
];

/// The folder a Hearthstone install is recognised by.
pub fn looks_like_install(path: &Path) -> bool {
    path.join("Hearthstone.exe").is_file()
}

/// The install to watch: what the player set, then where the game is running from, then
/// the usual folders. The first that holds a game is used.
pub fn find(saved: Option<&str>, running: Option<PathBuf>) -> Option<PathBuf> {
    let mut candidates = saved
        .map(PathBuf::from)
        .into_iter()
        .chain(running)
        .chain(USUAL_PATHS.iter().map(PathBuf::from));

    candidates.find(|path| looks_like_install(path))
}

/// The newest `Power.log` under the install's `Logs`, which the game keeps per session and,
/// depending on the version, in a folder of its own.
pub fn newest_log(install: &Path) -> Option<PathBuf> {
    let logs = install.join("Logs");
    let mut newest: Option<(std::time::SystemTime, PathBuf)> = None;

    for entry in walk(&logs) {
        if entry.file_name().is_some_and(|name| name == "Power.log") {
            let written = entry.metadata().ok()?.modified().ok()?;
            if newest.as_ref().is_none_or(|(latest, _)| written > *latest) {
                newest = Some((written, entry));
            }
        }
    }

    newest.map(|(_, path)| path)
}

/// Every file under `root`, one level of folders deep, which is as deep as the game goes.
fn walk(root: &Path) -> Vec<PathBuf> {
    let mut files = Vec::new();
    let Ok(entries) = root.read_dir() else {
        return files;
    };

    for entry in entries.flatten() {
        let path = entry.path();
        if path.is_dir() {
            files.extend(
                path.read_dir()
                    .into_iter()
                    .flatten()
                    .flatten()
                    .map(|inner| inner.path()),
            );
        } else {
            files.push(path);
        }
    }
    files
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::fs::{create_dir_all, write};

    fn fake_install(root: &Path, name: &str) -> PathBuf {
        let install = root.join(name);
        create_dir_all(&install).unwrap();
        write(install.join("Hearthstone.exe"), "").unwrap();
        install
    }

    fn temp_dir(name: &str) -> PathBuf {
        let dir = std::env::temp_dir().join(format!("kindly-bartender-{name}"));
        let _ = std::fs::remove_dir_all(&dir);
        create_dir_all(&dir).unwrap();
        dir
    }

    #[test]
    fn prefers_what_the_player_set() {
        let root = temp_dir("saved");
        let saved = fake_install(&root, "chosen");
        let running = fake_install(&root, "running");

        let found = find(Some(saved.to_str().unwrap()), Some(running));

        assert_eq!(found, Some(saved));
    }

    #[test]
    fn falls_back_to_where_the_game_runs_from() {
        let root = temp_dir("running");
        let running = fake_install(&root, "running");

        let found = find(Some(r"D:\nothing here"), Some(running.clone()));

        assert_eq!(found, Some(running));
    }

    #[test]
    fn a_folder_without_the_game_is_not_an_install() {
        let root = temp_dir("empty");
        create_dir_all(root.join("empty")).unwrap();

        assert!(!looks_like_install(&root.join("empty")));
        assert_eq!(find(Some(root.join("empty").to_str().unwrap()), None), None);
    }

    #[test]
    fn takes_the_log_written_last() {
        let root = temp_dir("logs");
        let install = fake_install(&root, "game");
        let session = install.join("Logs").join("Hearthstone_2026_09_23_21_00_00");
        create_dir_all(&session).unwrap();
        write(install.join("Logs").join("Power.log"), "old").unwrap();
        std::thread::sleep(std::time::Duration::from_millis(20));
        write(session.join("Power.log"), "new").unwrap();

        assert_eq!(newest_log(&install), Some(session.join("Power.log")));
    }

    #[test]
    fn no_logs_yet_is_not_an_error() {
        let root = temp_dir("nologs");
        let install = fake_install(&root, "game");

        assert_eq!(newest_log(&install), None);
    }
}
