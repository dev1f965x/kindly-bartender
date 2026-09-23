use std::fs::File;
use std::io::{BufRead, BufReader, Seek, SeekFrom};
use std::path::PathBuf;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::{Arc, Mutex};
use std::time::Duration;

use serde::Serialize;

use super::moments::{Detector, Moment};
use super::{install, logging};
use crate::platform;

/// How long between reads of the log. The product asks for a moment to reach the player
/// within a second, and a read of a few kilobytes twice a second costs nothing.
const POLL: Duration = Duration::from_millis(500);

/// What the app is doing, as the window shows it.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize)]
#[serde(rename_all = "kebab-case")]
pub enum Status {
    /// No install found; the player has to point at one.
    InstallNotFound,
    /// An install, but no log yet: the game is closed, or has not restarted since logging
    /// was turned on.
    WaitingForGame,
    /// Reading a live log.
    Watching,
}

/// What the watcher reports to whoever started it.
pub enum Report {
    Status(Status),
    Moment(Moment),
}

/// Tails the game's log and reports the moments in it (ADR 5).
pub struct Watcher {
    player_name: Arc<Mutex<Option<String>>>,
    saved_install: Arc<Mutex<Option<String>>>,
    stopped: Arc<AtomicBool>,
}

impl Watcher {
    /// Starts a thread that polls until the returned watcher is dropped.
    pub fn start(report: impl Fn(Report) + Send + 'static) -> Self {
        let watcher = Self {
            player_name: Arc::new(Mutex::new(None)),
            saved_install: Arc::new(Mutex::new(None)),
            stopped: Arc::new(AtomicBool::new(false)),
        };

        let mut tail = Tail::new(watcher.player_name.clone(), watcher.saved_install.clone());
        let stopped = watcher.stopped.clone();
        std::thread::spawn(move || {
            while !stopped.load(Ordering::Relaxed) {
                tail.once(&report);
                std::thread::sleep(POLL);
            }
        });

        watcher
    }

    /// The name the gold signal follows. Changing it starts the next match clean.
    pub fn follow_player(&self, name: Option<String>) {
        *self.player_name.lock().expect("player name") = name;
    }

    /// Where to look for the game when it is not running.
    pub fn look_in(&self, install: Option<String>) {
        *self.saved_install.lock().expect("saved install") = install;
    }
}

impl Drop for Watcher {
    fn drop(&mut self) {
        self.stopped.store(true, Ordering::Relaxed);
    }
}

/// One log file, read from where the last poll stopped.
struct Tail {
    player_name: Arc<Mutex<Option<String>>>,
    saved_install: Arc<Mutex<Option<String>>>,
    detector: Detector,
    following: Option<String>,
    log: Option<PathBuf>,
    offset: u64,
    limit_lifted_for: Option<PathBuf>,
    status: Option<Status>,
}

impl Tail {
    fn new(
        player_name: Arc<Mutex<Option<String>>>,
        saved_install: Arc<Mutex<Option<String>>>,
    ) -> Self {
        Self {
            player_name,
            saved_install,
            detector: Detector::new(None),
            following: None,
            log: None,
            offset: 0,
            limit_lifted_for: None,
            status: None,
        }
    }

    fn once(&mut self, report: &impl Fn(Report)) {
        self.follow_the_configured_player();

        let status = match self.read(report) {
            Some(status) => status,
            None => Status::WaitingForGame,
        };

        if self.status != Some(status) {
            self.status = Some(status);
            report(Report::Status(status));
        }
    }

    /// Reads whatever the game has appended, and says what state that leaves the app in.
    fn read(&mut self, report: &impl Fn(Report)) -> Option<Status> {
        let saved = self.saved_install.lock().expect("saved install").clone();
        let Some(install) = install::find(saved.as_deref(), platform::running_install()) else {
            return Some(Status::InstallNotFound);
        };

        if self.limit_lifted_for.as_ref() != Some(&install) {
            logging::lift_size_limit(&install);
            self.limit_lifted_for = Some(install.clone());
        }

        let log = install::newest_log(&install)?;
        if self.log.as_ref() != Some(&log) {
            self.start_on(log);
            return Some(Status::Watching);
        }

        self.read_new_lines(report);
        Some(Status::Watching)
    }

    /// A session already in progress is picked up at its end, so a combat that finished
    /// before the app was watching is never announced.
    fn start_on(&mut self, log: PathBuf) {
        self.offset = log.metadata().map(|file| file.len()).unwrap_or(0);
        self.log = Some(log);
        self.detector.forget();
    }

    fn read_new_lines(&mut self, report: &impl Fn(Report)) {
        let Some(log) = self.log.clone() else {
            return;
        };
        // The game holds the file open and keeps writing to it.
        let Ok(mut file) = File::open(&log) else {
            return;
        };

        let length = file.metadata().map(|file| file.len()).unwrap_or(0);
        if length < self.offset {
            // A new session took the same name.
            self.offset = 0;
            self.detector.forget();
        }
        if file.seek(SeekFrom::Start(self.offset)).is_err() {
            return;
        }

        let mut reader = BufReader::new(file);
        let mut line = String::new();
        loop {
            line.clear();
            match reader.read_line(&mut line) {
                Ok(0) | Err(_) => break,
                Ok(read) => {
                    self.offset += read as u64;
                    if let Some(moment) = self.detector.read(&line) {
                        report(Report::Moment(moment));
                    }
                }
            }
        }
    }

    fn follow_the_configured_player(&mut self) {
        let name = self.player_name.lock().expect("player name").clone();
        if name != self.following {
            self.detector = Detector::new(name.as_deref());
            self.following = name;
        }
    }
}
