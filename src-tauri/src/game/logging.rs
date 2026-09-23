use std::fs::{create_dir_all, read_to_string, write};
use std::path::{Path, PathBuf};

/// What `log.config` needs for the game to write Power.log at all.
const POWER_LOGGING: &str = "[Power]\nLogLevel=1\nFilePrinting=true\nConsolePrinting=false\nScreenPrinting=false\nVerbose=false\n";

/// Without this the game stops logging for the rest of the session once Power.log reaches
/// about 10MB, which a Battlegrounds match passes in roughly ten minutes. `log.config` has
/// no setting for it; `client.config` in the install folder does.
const NO_SIZE_LIMIT: &str = "[Log]\nFileSizeLimit.Int=-1\n";

fn log_config() -> Option<PathBuf> {
    dirs_local_app_data().map(|local| {
        local
            .join("Blizzard")
            .join("Hearthstone")
            .join("log.config")
    })
}

#[cfg(windows)]
fn dirs_local_app_data() -> Option<PathBuf> {
    std::env::var_os("LOCALAPPDATA").map(PathBuf::from)
}

#[cfg(not(windows))]
fn dirs_local_app_data() -> Option<PathBuf> {
    std::env::var_os("XDG_DATA_HOME").map(PathBuf::from)
}

/// Turns the game's power logging on.
///
/// Returns whether the file changed, which means Hearthstone has to restart before it
/// writes anything.
pub fn start_logging() -> bool {
    let Some(path) = log_config() else {
        return false;
    };
    if let Some(parent) = path.parent() {
        let _ = create_dir_all(parent);
    }
    write_if_different(&path, POWER_LOGGING)
}

/// Lifts the log's size cap for an install.
pub fn lift_size_limit(install: &Path) -> bool {
    write_if_different(&install.join("client.config"), NO_SIZE_LIMIT)
}

fn write_if_different(path: &Path, content: &str) -> bool {
    if read_to_string(path).is_ok_and(|current| current.trim() == content.trim()) {
        return false;
    }
    write(path, content).is_ok()
}

#[cfg(test)]
mod tests {
    use super::*;

    fn temp_file(name: &str) -> PathBuf {
        let path = std::env::temp_dir().join(format!("kindly-bartender-{name}.config"));
        let _ = std::fs::remove_file(&path);
        path
    }

    #[test]
    fn writes_a_missing_file() {
        let path = temp_file("missing");

        assert!(write_if_different(&path, NO_SIZE_LIMIT));
        assert_eq!(read_to_string(&path).unwrap(), NO_SIZE_LIMIT);
    }

    #[test]
    fn leaves_a_file_that_already_says_it() {
        let path = temp_file("same");
        write(&path, NO_SIZE_LIMIT).unwrap();

        assert!(!write_if_different(&path, NO_SIZE_LIMIT));
    }

    #[test]
    fn replaces_a_file_that_says_something_else() {
        let path = temp_file("other");
        write(&path, "[Log]\nFileSizeLimit.Int=10485760\n").unwrap();

        assert!(write_if_different(&path, NO_SIZE_LIMIT));
        assert_eq!(read_to_string(&path).unwrap(), NO_SIZE_LIMIT);
    }
}
