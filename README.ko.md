# Kindly Bartender

[English](README.md) | 한국어

하스스톤 전장용 Windows 트레이 앱입니다. 다른 창을 보고 있을 때 상점 단계가 시작되면 알려 줘서, 턴 타이머가 끝나기 전에 돌아올 수 있게 합니다.

아직 개발 중이며 출시된 버전은 없습니다.

Kindly Bartender는 비공식 팬 프로젝트이며 Blizzard Entertainment와 제휴하거나 보증을 받지 않았습니다. Hearthstone은 Blizzard Entertainment, Inc.의 상표입니다.

## 설치

Windows 10 버전 2004 이상 또는 Windows 11이 필요합니다.

1. [Releases](https://github.com/dev1f965x/kindly-bartender/releases)에서 `KindlyBartender-win-Setup.exe`를 내려받습니다. 현재 사용자에게만 설치되며 관리자 권한이 필요 없습니다.
2. 아직 코드 서명이 없어 SmartScreen이 인식할 수 없는 앱이라고 경고할 수 있습니다. 파일의 SHA-256을 `SHA256SUMS.txt`와 비교한 뒤(PowerShell에서 `Get-FileHash .\KindlyBartender-win-Setup.exe`) **추가 정보**와 **실행**을 선택하세요.
3. 시작 메뉴에서 Kindly Bartender를 열고 로그 설정 창을 따르세요. 동의한 뒤에만 하스스톤 설정 파일 두 개를 바꿉니다. 하스스톤이 실행 중이었다면 다시 시작하세요.

설치하지 않고 쓰려면 `KindlyBartender-win-Portable.zip`의 압축을 풀고 `Kindly Bartender.exe`를 실행하세요. 포터블 버전은 설정과 진단 기록을 자기 폴더에 보관합니다. 지우려면 설정에서 Windows 시작 시 실행을 끄고 트레이에서 종료한 뒤 폴더를 삭제하세요.

## 제거

**설정 > 앱 > 설치된 앱**(Windows 11) 또는 **설정 > 앱 > 앱 및 기능**(Windows 10)에서 제거합니다. 앱과 설정, 진단 기록, Windows 시작 시 실행 항목이 지워집니다. 덱 트래커 같은 다른 도구가 쓸 수 있어 하스스톤의 로그 설정은 그대로 둡니다. 되돌리려면 `%LOCALAPPDATA%\Blizzard\Hearthstone\log.config`에서 `[Power]` 구역을, 하스스톤 폴더의 `client.config`에서 `FileSizeLimit.Int` 줄을 지우세요.

## 개인정보

Kindly Bartender는 개인정보를 수집하지 않으며 사용 통계도 보내지 않습니다.

- 시작할 때마다 GitHub(`api.github.com`)에 새 버전이 있는지 확인합니다. 다른 웹 요청과 마찬가지로 IP 주소와 앱 버전이 GitHub에 전달됩니다. 아무것도 내려받거나 설치하지 않습니다.
- `%LOCALAPPDATA%\KindlyBartender\data\logs`에 진단 기록을 하루 한 파일, 최대 일곱 개까지 남깁니다. 정해진 이벤트 이름, 숫자, 오류 종류만 기록하며 경로, 계정 이름, 게임 내용은 기록하지 않습니다. 문제를 신고할 때 첨부할 수 있으며, 앱이 어디로도 보내지 않습니다.

## 개발

필요한 것: Windows 11과 [global.json](global.json)에 적힌 버전의 .NET SDK

명령 목록은 [README.md](README.md#development)의 표를 기준으로 합니다.

## 라이선스

[MIT](LICENSE)
