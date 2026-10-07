# CONTENT.md

The words Kindly Bartender shows: UI strings, notifications, and the README's product description. It applies the general content guide to this product. `scripts/Test-Content.ps1` enforces the forbidden words and punctuation, and `src/KindlyBartender.App/Resources` holds the strings in this file.

## Voice

Plain, short, calm. Say what happened and what to do. The app is a background utility; it speaks only when the player needs to act or to know something.

- English uses sentence case and addresses the reader as "you". Korean uses 합니다체 and leaves out an obvious subject.
- No exclamation marks, emoji, or jokes. Notifications are as short as the message allows.
- Never describe what the game shows beyond the phase that started, such as boards, opponents, or advice.

## Terms

Use only these words for these concepts. Add a term here before using a new one in the UI.

| Concept | English | Korean | Notes |
| --- | --- | --- | --- |
| The product | Kindly Bartender | Kindly Bartender | Never translated. |
| The game | Hearthstone | 하스스톤 | A Blizzard trademark; used only to name the game. |
| The mode | Battlegrounds | 전장 | |
| The phase to buy minions | Recruit phase | 상점 단계 | Players say "상점"; the UI says 상점 단계 when a phase is meant. |
| Choosing a hero | hero selection | 영웅 선택 | |
| A Windows toast | notification | 알림 | |
| Turning notifications off for a while | pause / resume | 일시 중지 / 다시 시작 | |
| The two Hearthstone files | log settings | 로그 설정 | Never "config" in the UI. |
| Writing the log settings | set up | 설정하기 | The window is 로그 설정 (Log settings). |
| The app's options | Settings | 설정 | |
| The app's information window | About | 정보 | |
| Showing the game window in front | bring Hearthstone to the front | 하스스톤 화면을 앞으로 가져오기 | Never "focus". |
| The diagnostic log | diagnostic log | 진단 기록 | Distinct from Hearthstone's logs. |

## Patterns

- Buttons: English starts with a verb ("Set up", "Close"); Korean is a noun form ("설정하기", "닫기").
- A status in the tray menu is a short sentence without a final period.
- Errors say what happened, then what to do. Warnings put the result in the title and the cause and fix in the body.
- File paths and setting names appear in code font in windows and as plain text in notifications.

## Strings

IDs match the resource keys. `{0}` is a value filled in by the app.

### Notifications

| ID | English | Korean |
| --- | --- | --- |
| Toast.HeroSelection.Title | Hero selection started | 영웅 선택이 시작되었습니다 |
| Toast.Recruit.Title | The Recruit phase started | 상점 단계가 시작되었습니다 |
| Toast.Phase.Body | Select to return to Hearthstone. | 선택하면 하스스톤으로 돌아갑니다. |
| Toast.NotWorking.Title | Notifications may not work | 알림이 작동하지 않을 수 있습니다 |
| Toast.NotWorking.LogCapped | Hearthstone stopped writing its log. Restart Hearthstone. | 하스스톤이 로그 기록을 멈췄습니다. 하스스톤을 다시 시작하세요. |
| Toast.NotWorking.NoSignal | No Recruit phase was found in this game. Kindly Bartender may need an update. | 이 게임에서 상점 단계를 찾지 못했습니다. Kindly Bartender 업데이트가 필요할 수 있습니다. |
| Toast.SetupNeeded.Title | Log settings are missing | 로그 설정이 없습니다 |
| Toast.SetupNeeded.Body | Hearthstone’s log settings were removed. Open Kindly Bartender to set them up again. | 하스스톤의 로그 설정이 지워졌습니다. Kindly Bartender를 열어 다시 설정하세요. |

### Tray

| ID | English | Korean |
| --- | --- | --- |
| Tray.Status.SetupNeeded | Log settings needed | 로그 설정 필요 |
| Tray.Status.RestartNeeded | Restart Hearthstone to start notifications | 알림을 받으려면 하스스톤을 다시 시작하세요 |
| Tray.Status.NotWorking | Notifications may not work | 알림이 작동하지 않을 수 있음 |
| Tray.Status.Paused | Notifications paused | 알림 일시 중지됨 |
| Tray.Status.Waiting | Waiting for Hearthstone | 하스스톤 실행 대기 중 |
| Tray.Status.Ready | Ready | 준비됨 |
| Tray.Status.DoNotDisturb | Do not disturb is on; notifications may be hidden | 방해 금지가 켜져 있어 알림이 숨겨질 수 있음 |
| Tray.Menu.SetUp | Set up log settings | 로그 설정하기 |
| Tray.Menu.Settings | Settings | 설정 |
| Tray.Menu.Pause | Pause notifications | 알림 일시 중지 |
| Tray.Menu.Resume | Resume notifications | 알림 다시 시작 |
| Tray.Menu.About | About | 정보 |
| Tray.Menu.Exit | Exit | 종료 |

### Log settings window

| ID | English | Korean |
| --- | --- | --- |
| Setup.Title | Log settings | 로그 설정 |
| Setup.Intro | Kindly Bartender tells you when hero selection or a Recruit phase starts while you’re in another window. It reads Hearthstone’s log, which Hearthstone writes only after two of its settings files are changed. | Kindly Bartender는 다른 창을 보고 있을 때 영웅 선택이나 상점 단계가 시작되면 알려 줍니다. 하스스톤의 로그를 읽는데, 하스스톤은 설정 파일 두 개를 바꿔야 로그를 기록합니다. |
| Setup.Files.Heading | Files that will change | 바뀌는 파일 |
| Setup.Files.LogConfig | {0}: turns on the game log | {0}: 게임 로그를 켭니다 |
| Setup.Files.ClientConfig | {0}: removes the log size limit | {0}: 로그 크기 제한을 없앱니다 |
| Setup.Files.Note | Only these settings change. Other settings in the files stay as they are. | 이 설정만 바뀝니다. 파일의 다른 설정은 그대로 둡니다. |
| Setup.Risk.Heading | Before you continue | 계속하기 전에 |
| Setup.Risk.Body | Kindly Bartender is an unofficial fan project, not made or endorsed by Blizzard Entertainment. Blizzard’s End User License Agreement forbids programs it hasn’t authorized from reading game data. Deck trackers that read the same log have been used for years, but Blizzard has never authorized them. Use Kindly Bartender at your own risk. | Kindly Bartender는 Blizzard Entertainment가 만들거나 보증하지 않은 비공식 팬 프로젝트입니다. Blizzard 최종 사용자 사용권 계약은 허가받지 않은 프로그램이 게임 데이터를 읽는 것을 금지합니다. 같은 로그를 읽는 덱 트래커가 오랫동안 쓰였지만 Blizzard가 허가한 적은 없습니다. 사용에 따른 책임은 사용자에게 있습니다. |
| Setup.StartWithWindows | Start Kindly Bartender with Windows | Windows 시작 시 Kindly Bartender 실행 |
| Setup.BringToFront | Bring Hearthstone to the front when a phase starts | 단계가 시작되면 하스스톤 화면을 앞으로 가져오기 |
| Setup.BringToFront.Note | The game window comes to the front without taking your keyboard. | 게임 화면만 앞으로 오고 키보드 입력은 그대로입니다. |
| Setup.Button.SetUp | Agree and set up | 동의하고 설정하기 |
| Setup.Button.Close | Close | 닫기 |
| Setup.Folder.Missing | Hearthstone wasn’t found. Choose the folder that contains Hearthstone.exe. | 하스스톤을 찾지 못했습니다. Hearthstone.exe가 있는 폴더를 선택하세요. |
| Setup.Folder.Choose | Choose folder | 폴더 선택 |
| Setup.Folder.Invalid | This folder doesn’t contain Hearthstone.exe. Choose another folder. | 이 폴더에 Hearthstone.exe가 없습니다. 다른 폴더를 선택하세요. |
| Setup.Done | Log settings are set up. | 로그 설정을 마쳤습니다. |
| Setup.RestartNeeded | Log settings are set up. Restart Hearthstone to start notifications. | 로그 설정을 마쳤습니다. 알림을 받으려면 하스스톤을 다시 시작하세요. |
| Setup.Elevation.Cancelled | The log size limit wasn’t removed because administrator permission wasn’t given. Select Agree and set up to try again. | 관리자 권한을 허용하지 않아 로그 크기 제한을 없애지 못했습니다. 다시 하려면 동의하고 설정하기를 선택하세요. |
| Setup.Failed | The log settings couldn’t be changed. Close Hearthstone and try again. | 로그 설정을 바꾸지 못했습니다. 하스스톤을 종료한 뒤 다시 시도하세요. |

### Settings window

| ID | English | Korean |
| --- | --- | --- |
| Settings.Title | Settings | 설정 |
| Settings.Notify.Heading | When a phase starts | 단계가 시작되면 |
| Settings.Notify.Toast | Show a notification | 알림 표시 |
| Settings.Notify.Sound | Play a sound | 소리 재생 |
| Settings.Notify.Flash | Flash Hearthstone on the taskbar | 작업 표시줄에서 하스스톤 깜빡이기 |
| Settings.Notify.BringToFront | Bring Hearthstone to the front | 하스스톤 화면을 앞으로 가져오기 |
| Settings.Notify.Note | Nothing happens while Hearthstone is the active window. | 하스스톤이 활성 창이면 아무것도 하지 않습니다. |
| Settings.DoNotDisturb | Do not disturb is on, so notifications may be hidden. To let them through, add Kindly Bartender to priority notifications in Windows settings. | 방해 금지가 켜져 있어 알림이 숨겨질 수 있습니다. 알림을 받으려면 Windows 설정에서 Kindly Bartender를 우선순위 알림에 추가하세요. |
| Settings.WindowsNotifications | Windows notification settings | Windows 알림 설정 |
| Settings.General.Heading | General | 일반 |
| Settings.StartWithWindows | Start with Windows | Windows 시작 시 실행 |
| Settings.Language | Language | 언어 |
| Settings.Language.System | Same as Windows | Windows 설정과 같게 |
| Settings.Folder | Hearthstone folder | 하스스톤 폴더 |
| Settings.Folder.Change | Change | 변경 |

### About window

| ID | English | Korean |
| --- | --- | --- |
| About.Title | About | 정보 |
| About.Version | Version {0} | 버전 {0} |
| About.Update | A new version is available: {0} | 새 버전을 사용할 수 있습니다: {0} |
| About.Update.Link | Open the download page | 다운로드 페이지 열기 |
| About.Unofficial | Kindly Bartender is an unofficial fan project and is not affiliated with or endorsed by Blizzard Entertainment. Hearthstone is a trademark of Blizzard Entertainment, Inc. | Kindly Bartender는 비공식 팬 프로젝트이며 Blizzard Entertainment와 제휴하거나 보증을 받지 않았습니다. Hearthstone은 Blizzard Entertainment, Inc.의 상표입니다. |
| About.Risk | Blizzard’s End User License Agreement forbids programs it hasn’t authorized from reading game data. Use Kindly Bartender at your own risk. | Blizzard 최종 사용자 사용권 계약은 허가받지 않은 프로그램이 게임 데이터를 읽는 것을 금지합니다. 사용에 따른 책임은 사용자에게 있습니다. |
| About.Files.Heading | What Kindly Bartender reads and changes | Kindly Bartender가 읽고 바꾸는 것 |
| About.Files.Body | It reads Hearthstone’s Power.log while the game runs. After you agree, it changes log settings in two files: {0}, {1}. Uninstalling doesn’t undo these changes, because other tools may use them. To undo them, delete the [Power] section from the first file and the [Log] section from the second. | 게임이 실행되는 동안 하스스톤의 Power.log를 읽습니다. 동의하면 다음 두 파일의 로그 설정을 바꿉니다: {0}, {1}. 다른 도구가 쓸 수 있어 제거해도 이 변경은 되돌리지 않습니다. 되돌리려면 첫 번째 파일의 [Power] 구역과 두 번째 파일의 [Log] 구역을 지우세요. |
| About.Privacy.Heading | Privacy | 개인정보 |
| About.Privacy.Body | Kindly Bartender collects no personal data and sends nothing about you or your games. At startup it checks GitHub for a new version, so GitHub sees your IP address, as with any web request. | Kindly Bartender는 개인정보를 수집하지 않으며 사용자나 게임에 관한 정보를 보내지 않습니다. 시작할 때 GitHub에서 새 버전을 확인하므로 다른 웹 요청과 마찬가지로 GitHub가 IP 주소를 알 수 있습니다. |
| About.License | License: MIT | 라이선스: MIT |
| About.ThirdParty | Third-party notices | 제3자 고지 |
| About.Feedback | Report a problem | 문제 신고 |
| About.DiagnosticLog | Open diagnostic log | 진단 기록 열기 |
| About.DiagnosticLog.Note | The diagnostic log contains no account names. You can attach it to a problem report. | 진단 기록에는 계정 이름이 없습니다. 문제를 신고할 때 첨부할 수 있습니다. |
