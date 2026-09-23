use regex::Regex;
use serde::Serialize;

/// A moment worth pulling the player back for.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize)]
#[serde(rename_all = "kebab-case")]
pub enum Moment {
    /// Combat finished and the shop is open again. Solo and Duos.
    CombatEnded,
    /// Duos only: the teammate's gold reached the player mid-shop.
    GoldArrived,
}

/// Reads `Power.log` line by line and says when one of the [`Moment`]s happened.
///
/// The game prints the board's visual state as it swaps between shop and combat, which is
/// the one signal that holds for solo and Duos alike. Gold needs the player's own entity,
/// so it is recognised only once a name is configured.
pub struct Detector {
    board_state: Regex,
    duos_marker: Regex,
    spent_gold: Option<Regex>,
    in_combat: bool,
    duos: bool,
    spent: Option<u32>,
}

/// The shop is on screen.
const BOARD_SHOP: &str = "1";
/// Combat is on screen.
const BOARD_COMBAT: &str = "2";

impl Detector {
    pub fn new(player_name: Option<&str>) -> Self {
        Self {
            board_state: Regex::new(
                r"PowerTaskList\.DebugPrintPower\(\).*TAG_CHANGE Entity=GameEntity tag=BOARD_VISUAL_STATE value=(\d)",
            )
            .expect("board state pattern"),
            // Printed only once a teammate is assigned.
            duos_marker: Regex::new(r"tag=BACON_DUO_TEAM_ID value=\d").expect("duos pattern"),
            spent_gold: player_name
                .map(str::trim)
                .filter(|name| !name.is_empty())
                .map(|name| {
                    // Entities print as `<BattleTag>#NNNN`; the discriminator can change,
                    // so any of them matches.
                    Regex::new(&format!(
                        r"TAG_CHANGE Entity={}#\d+ tag=RESOURCES_USED value=(\d+)",
                        regex::escape(name)
                    ))
                    .expect("gold pattern")
                }),
            in_combat: false,
            duos: false,
            spent: None,
        }
    }

    /// The moment this line completes, if it completes one.
    pub fn read(&mut self, line: &str) -> Option<Moment> {
        if !self.duos && self.duos_marker.is_match(line) {
            self.duos = true;
        }

        if let Some(state) = self.board_state.captures(line) {
            match &state[1] {
                BOARD_COMBAT => self.in_combat = true,
                BOARD_SHOP if self.in_combat => {
                    self.in_combat = false;
                    return Some(Moment::CombatEnded);
                }
                _ => {}
            }
        }

        // Spent gold falling mid-shop means gold arrived: in Duos a teammate can hand over
        // what they did not spend.
        if self.duos {
            if let Some(spent) = self
                .spent_gold
                .as_ref()
                .and_then(|pattern| pattern.captures(line))
                .and_then(|caught| caught[1].parse::<u32>().ok())
            {
                let fell = self.spent.is_some_and(|previous| spent < previous);
                self.spent = Some(spent);
                if fell {
                    return Some(Moment::GoldArrived);
                }
            }
        }

        None
    }

    /// Forgets the match in progress, for when a new log file starts.
    pub fn forget(&mut self) {
        self.in_combat = false;
        self.duos = false;
        self.spent = None;
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    const COMBAT: &str = "D 21:03:11.0 PowerTaskList.DebugPrintPower() -     TAG_CHANGE Entity=GameEntity tag=BOARD_VISUAL_STATE value=2";
    const SHOP: &str = "D 21:03:41.0 PowerTaskList.DebugPrintPower() -     TAG_CHANGE Entity=GameEntity tag=BOARD_VISUAL_STATE value=1";
    const DUOS: &str = "D 21:00:02.0 PowerTaskList.DebugPrintPower() -     TAG_CHANGE Entity=Player tag=BACON_DUO_TEAM_ID value=1";

    fn spent(gold: u32) -> String {
        format!(
            "D 21:03:45.0 PowerTaskList.DebugPrintPower() -     TAG_CHANGE Entity=바텐더#1234 tag=RESOURCES_USED value={gold}"
        )
    }

    #[test]
    fn announces_the_shop_only_after_combat() {
        let mut detector = Detector::new(None);

        assert_eq!(detector.read(SHOP), None, "a shop without combat first");
        assert_eq!(detector.read(COMBAT), None);
        assert_eq!(detector.read(SHOP), Some(Moment::CombatEnded));
        assert_eq!(detector.read(SHOP), None, "the same shop again");
    }

    #[test]
    fn announces_every_round() {
        let mut detector = Detector::new(None);

        for _ in 0..3 {
            detector.read(COMBAT);
            assert_eq!(detector.read(SHOP), Some(Moment::CombatEnded));
        }
    }

    #[test]
    fn gold_arriving_needs_duos_and_a_name() {
        let mut solo = Detector::new(Some("바텐더"));
        solo.read(&spent(5));
        assert_eq!(
            solo.read(&spent(2)),
            None,
            "solo has no teammate to give gold"
        );

        let mut nameless = Detector::new(None);
        nameless.read(DUOS);
        nameless.read(&spent(5));
        assert_eq!(
            nameless.read(&spent(2)),
            None,
            "without a name, no entity to follow"
        );
    }

    #[test]
    fn announces_gold_when_what_is_spent_falls() {
        let mut detector = Detector::new(Some("바텐더"));
        detector.read(DUOS);

        assert_eq!(
            detector.read(&spent(0)),
            None,
            "the first reading only sets the mark"
        );
        assert_eq!(
            detector.read(&spent(6)),
            None,
            "spending more is the player buying"
        );
        assert_eq!(detector.read(&spent(2)), Some(Moment::GoldArrived));
    }

    #[test]
    fn follows_only_the_player_named() {
        let mut detector = Detector::new(Some("바텐더"));
        detector.read(DUOS);
        detector.read(&spent(6));

        let teammate = "D 21:03:46.0 PowerTaskList.DebugPrintPower() -     TAG_CHANGE Entity=상대#9999 tag=RESOURCES_USED value=0";
        assert_eq!(detector.read(teammate), None);
    }

    #[test]
    fn forgetting_starts_the_next_match_clean() {
        let mut detector = Detector::new(None);
        detector.read(COMBAT);
        detector.forget();

        assert_eq!(detector.read(SHOP), None);
    }
}
