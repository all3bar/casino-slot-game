# 카지노 슬롯 게임 (Casino Slot Game)

카지노 하드웨어를 위한 SAS 6.0 프로토콜 및 ID-003/Pulse 통신을 지원하는 전문가용 슬롯 머신 시스템입니다.

## 개요

C# (.NET / Unity)으로 개발된 전문가용 카지노 슬롯 게임으로, 다음 기능을 완벽히 지원합니다:

- **5릴 × 3행 엔진**: 12개 심볼, 20개 페이라인(Payline) 및 인증 가능한 RNG 탑재
- **SAS 6.0 프로토콜**: 실시간 폴링, 미터(계측기) 명령 및 예외 처리 완벽 지원
- **ID-003 통신**: 지폐 인식기(Bill Validator, RS-232 직렬 통신) 지원
- **Pulse 프로토콜**: 코인 인식기(Coin Acceptor) 및 호퍼(Hopper) 지원
- **회계(Accounting) 시스템**: 영구 미터 및 SHA-256 체크섬을 통한 데이터 무결성 검증
- **고급 게임 기능**: 무료 스핀(Free Spins), 보너스 게임(Pick-em), 와일드/스캐터(Wild/Scatter), 프로그레시브 잭팟(Progressive Jackpot)
- **운영자 패널(Operator Panel)**: PIN 인증 기반 보안 제어
- **설정 가능한 입력 시스템**: USB 키보드, HID 조이스틱/버튼 패널 (JSON 파일로 키 바인딩 설정 가능)
- **109개 단위 테스트(Unit Tests)**: 게임 로직, SAS 프로토콜, 하드웨어 및 입력 계층 검증 완료

---

## 프로젝트 구조

```
casino-slot-game/
├── Assets/
│   ├── config.json                        # 게임 설정 파일
│   └── Scripts/
│       ├── Core/
│       │   ├── GameManager.cs             # 중앙 싱글톤, 상태 머신(State Machine)
│       │   ├── SlotMachine.cs             # 슬롯 게임 엔진 코어
│       │   ├── Reel.cs                    # 릴 및 심볼 스트립 구성
│       │   ├── Symbol.cs                  # 심볼 타입 및 팩토리
│       │   ├── PaylineManager.cs          # 20개 페이라인 및 페이테이블(PayTable)
│       │   └── RNG.cs                     # 규격 인증 가능한 난수 생성기
│       ├── SAS/
│       │   ├── SASProtocol.cs             # 메시지 처리, CRC-16, BCD 인코딩
│       │   ├── SASCommands.cs             # 명령 코드 및 예외 코드 정의
│       │   ├── SASExceptions.cs           # 실시간 예외 큐(Exception Queue)
│       │   ├── SASCommunicator.cs         # SAS 명령 처리기
│       │   └── SASMeterManager.cs         # 0x7B 계측 미터 응답 관리
│       ├── Hardware/
│       │   ├── SerialCommunication.cs     # System.IO.Ports 래퍼
│       │   ├── BillAcceptor.cs            # 지폐 인식기 컨트롤러
│       │   ├── ID003Protocol.cs           # ID-003 프로토콜 (RS-232)
│       │   └── PulseController.cs         # 코인 투입/호퍼 펄스 + HopperController
│       ├── Input/                         # 입력 장치 레이어
│       │   ├── SlotAction.cs              # 플레이어 동작 열거형(Enum)
│       │   ├── IInputProvider.cs          # 입력 소스 인터페이스
│       │   ├── KeyboardInputProvider.cs   # USB 키보드 (설정 가능)
│       │   ├── JoystickInputProvider.cs   # HID 조이스틱 / 버튼 패널 (설정 가능)
│       │   └── InputManager.cs            # 입력 공급자 취합 → MainGameUI 전달
│       ├── Accounting/
│       │   ├── MeterManager.cs            # 영구 미터 (스레드 안전)
│       │   ├── EventLogger.cs             # 이벤트 타입별 감사 로그
│       │   ├── PersistenceManager.cs      # SHA-256 체크섬 기반 안전 저장
│       │   └── GameHistory.cs             # 최근 500회 게임 이력 (리플레이 지원)
│       ├── UI/
│       │   ├── MainGameUI.cs              # 메인 게임 UI
│       │   ├── PaytableUI.cs              # 페이테이블 UI
│       │   ├── OperatorUI.cs              # 운영자 패널 (PIN 보호)
│       │   └── AnimationController.cs     # 애니메이션 제어
│       └── Features/
│           ├── FreeSpinsManager.cs        # 승수(배율) 적용 무료 스핀
│           ├── BonusGameManager.cs        # 보너스 Pick-em 게임
│           └── JackpotManager.cs          # 프로그레시브 잭팟 관리
└── Tests/
    ├── CasinoSlot.Tests.csproj
    ├── Core/
    │   ├── RNGTests.cs
    │   ├── PaylineManagerTests.cs
    │   └── SlotMachineTests.cs
    ├── SAS/
    │   └── SASProtocolTests.cs
    ├── Hardware/
    │   └── HardwareTests.cs
    └── Input/
        └── InputTests.cs                  # 키보드, 조이스틱, InputManager 46개 테스트
```

---

## 실행 환경 및 요구사항

- **.NET 8.0 SDK** (Unity 없이 단위 테스트 실행 및 컴파일 시 필요)
- **Unity 2021.3 LTS 이상** (그래픽 UI를 포함한 전체 게임 구동 시 필요)
- **Windows** (System.IO.Ports / SerialPort 하드웨어 통신 지원)

---

## 빌드 및 실행 방법

### 1. GUI 시뮬레이터 실행 (Windows 데스크톱 앱)

Unity 없이도 Windows 화면에서 시각적으로 슬롯 머신을 직접 돌리고 가상 지폐/코인 투입 및 SAS 미터를 테스트할 수 있는 전용 GUI 시뮬레이터입니다.

```bash
# GUI 앱 실행
dotnet run --project GUI/CasinoSlot.GUI.csproj
```

또는 탐색기에서 **`GUI\bin\Debug\net8.0-windows\CasinoSlot.GUI.exe`**를 더블 클릭하여 즉시 실행할 수 있습니다.

- **5릴 × 3행 시각화 릴 회전 애니메이션**
- **스페이스바(Spin), M(Max Bet), C(Cashout), 방향키(베팅/라인 조절)**
- **가상 지폐 투입기 (\$1, \$5, \$10, \$20, \$50, \$100) 및 코인 펄스 버튼**
- **실시간 SAS 6.0 회계 계측기(Meters) 및 실시간 감사/이벤트 로그 뷰어**

---

### 2. 단위 및 목업 통합 테스트 실행

터미널에서 아래 명령을 실행합니다:

```bash
# 1) 신규 목업 시나리오 5개만 실행
dotnet test Tests/CasinoSlot.Tests.csproj --filter "FullyQualifiedName~MockIntegratedScenarioTests"

# 2) 전체 단위 및 통합 테스트 (총 114개) 실행
dotnet test Tests/CasinoSlot.Tests.csproj
```

**예상 결과:** `총 114개의 테스트 통과, 0개 실패 (Passed: 114, Failed: 0)`

---

## 입력 시스템 — 조이스틱 / 버튼 패널 / USB 키보드

`Assets/Scripts/Input/` 모듈은 다음 장치들을 동시에 지원하는 **완전 구성 가능한 추상화 입력 계층**을 제공합니다:

| 장치                     | 클래스                  | 설명                                  |
| ------------------------ | ----------------------- | ------------------------------------- |
| USB 키보드               | `KeyboardInputProvider` | `ConsoleKey` 키를 게임 액션으로 매핑  |
| HID 조이스틱 / 버튼 패널 | `JoystickInputProvider` | 버튼 인덱스(0~N)를 게임 액션으로 매핑 |
| 기타 커스텀 장치         | `IInputProvider`        | 새 하드웨어용 인터페이스 구현         |

### 지원 액션 목록 (`SlotAction`)

| 값  | 액션                              | 기본 USB 키보드 키 | 기본 조이스틱 버튼 |
| :-: | --------------------------------- | ------------------ | ------------------ |
|  1  | **Spin (스핀)**                   | `Space` / `Enter`  | 버튼 0             |
|  2  | **MaxBet (최대 베팅)**            | `M`                | 버튼 1             |
|  3  | **BetIncrease (베팅 증가)**       | `↑` (위 화살표)    | 버튼 2             |
|  4  | **BetDecrease (베팅 감소)**       | `↓` (아래 화살표)  | 버튼 3             |
|  5  | **LinesIncrease (라인 수 증가)**  | `→` (우측 화살표)  | 버튼 4             |
|  6  | **LinesDecrease (라인 수 감소)**  | `←` (좌측 화살표)  | 버튼 5             |
|  7  | **Cashout (정산/출금)**           | `C`                | 버튼 6             |
|  8  | **AcknowledgeResult (결과 확인)** | `Escape` / `A`     | 버튼 7             |
|  9  | **OperatorMenu (운영자 메뉴)**    | `F10`              | 버튼 9             |
| 10  | **RepeatBet (동일 베팅 반복)**    | `R`                | 버튼 8             |

### 바인딩 설정 (`Assets/config.json`)

키 및 버튼 바인딩은 `Assets/config.json`의 `input` 섹션에서 지정합니다:

```json
"input": {
  "keyboard": {
    "enabled": true,
    "bindings": {
      "Spacebar": 1,   // Spin
      "Enter":    1,   // Spin (대체 키)
      "C":        7    // Cashout
    }
  },
  "joystick": {
    "enabled": true,
    "bindings": {
      "0": 1,   // 0번 버튼 → Spin
      "6": 7    // 6번 버튼 → Cashout
    }
  }
}
```

### Unity 연동 예시

```csharp
// 1. 공급자(Provider) 생성
var keyboard = new KeyboardInputProvider();
var joystick = new JoystickInputProvider("USB Button Panel");

// 2. InputManager 설정
var inputManager = new InputManager();
inputManager.RegisterProvider(keyboard);
inputManager.RegisterProvider(joystick);
inputManager.ConnectUI(mainGameUI); // UI 연결

// 3. Unity Update()에서 조이스틱 상태 전달
void Update()
{
    for (int i = 0; i < 10; i++)
    {
        SlotAction a = joystick.NotifyButtonState(i, Input.GetButton("Joystick1Button" + i));
        if (a != SlotAction.None)
            inputManager.DispatchAction(a, joystick);
    }
    // 키보드는 inputManager.Update()에서 자동 처리
    inputManager.Update();
}
```

### 새로운 독자 규격 버튼 패널 추가 방법

1. `IInputProvider`를 구현하는 클래스 생성
2. `Poll()` 메서드를 오버라이드하여 감지된 액션 반환
3. `inputManager.RegisterProvider(myDevice)`로 등록

---

## 설정 항목 (`Assets/config.json`)

| 키                                   | 설명                              | 기본값                           |
| ------------------------------------ | --------------------------------- | -------------------------------- |
| `sas.address`                        | 머신의 SAS 주소 (1~127)           | `1`                              |
| `sas.portName`                       | SAS 통신용 COM 포트               | `COM1`                           |
| `sas.baudRate`                       | SAS 통신 속도 (Baud Rate)         | `19200`                          |
| `billAcceptor.portName`              | 지폐 인식기 COM 포트              | `COM2`                           |
| `billAcceptor.acceptedDenominations` | 수락 가능한 지폐 권종 (센트 단위) | `[100,500,1000,2000,5000,10000]` |
| `machine.denomination`               | 크레딧 권종 (단위 화폐)           | `1`                              |
| `machine.maxBetPerLine`              | 라인당 최대 베팅                  | `10`                             |
| `machine.numPaylines`                | 활성 페이라인 수                  | `20`                             |
| `jackpot.seedAmount`                 | 잭팟 시작 금액 (크레딧)           | `1000`                           |
| `jackpot.contributionRate`           | 잭팟 적립률 (각 베팅당 비율)      | `0.01` (1%)                      |
| `rtp.target`                         | 목표 환수율 (RTP)                 | `96.0%`                          |

---

## 페이테이블 (라인 베팅 배율 기준)

|  심볼   |  2개 일치   |   3개 일치   |   4개 일치   |  5개 일치   |
| :-----: | :---------: | :----------: | :----------: | :---------: |
|  WILD   |     10      |      50      |     200      |    2000     |
|    7    |      5      |      30      |     150      |    1000     |
|  BELL   |      4      |      20      |     100      |     500     |
|  BAR 3  |      3      |      15      |      75      |     300     |
|  BAR 2  |      2      |      10      |      50      |     200     |
|  BAR 1  |      1      |      8       |      40      |     150     |
| CHERRY  |      4      |      10      |      40      |     100     |
|  LEMON  |      1      |      5       |      20      |     80      |
| ORANGE  |      1      |      5       |      20      |     80      |
|  PLUM   |      1      |      5       |      20      |     80      |
| SCATTER | 총 베팅 × 2 | 총 베팅 × 10 | 총 베팅 × 50 | (아래 참조) |

- **SCATTER**: 3개 = 무료 스핀 10회, 4개 = 무료 스핀 15회, 5개 = 무료 스핀 25회
- **BONUS**: 위치 상관없이 3개 이상 = 보너스 Pick-em 게임 시작
- **WILD**: SCATTER 및 BONUS를 제외한 모든 심볼 대체 가능

---

## SAS 6.0 프로토콜 — 구현된 명령 목록

| 명령 코드 | 설명                                                 |
| :-------: | ---------------------------------------------------- |
|  `0x01`   | Shutdown (머신 셧다운)                               |
|  `0x11`   | Startup (머신 시작)                                  |
|  `0x12`   | Sound Off (사운드 음소거)                            |
|  `0x13`   | Sound On (사운드 켜기)                               |
|  `0x06`   | Enable Bill Acceptor (지폐 인식기 활성화)            |
|  `0x07`   | Disable Bill Acceptor (지폐 인식기 비활성화)         |
|  `0x08`   | Enable Game Play (게임 플레이 활성화)                |
|  `0x09`   | Disable Game Play (게임 플레이 비활성화)             |
|  `0x1F`   | Send Enabled Game Numbers (활성화된 게임 번호 전송)  |
|  `0x2E`   | Send Current Credits (현재 크레딧 전송)              |
|  `0x52`   | Send Selected Game Number (선택된 게임 번호 전송)    |
|  `0x53`   | Send Gaming Machine ID (게임 머신 식별자 전송)       |
|  `0x4C`   | Send Last Accepted Bill (마지막 수락 지폐 정보 전송) |
|  `0x54`   | Send Total Coin In (총 코인 투입량 전송)             |
|  `0x55`   | Send Total Coin Out (총 코인 배출량 전송)            |
|  `0x56`   | Send Total Drop (총 드롭 금액 전송)                  |
|  `0x57`   | Send Total Jackpot (총 잭팟 금액 전송)               |
|  `0x58`   | Send Games Played (총 플레이 게임 수 전송)           |
|  `0x59`   | Send Games Won (총 승리 게임 수 전송)                |
|  `0x6F`   | Send Authentication Info (인증 정보 전송)            |
|  `0x7B`   | Send Gaming Machine Meters (게임 머신 미터 전송)     |

### SAS 실시간 예외 이벤트 (Real-time Exceptions)

100개 크기의 대기열을 가진 `SASExceptionManager`를 통해 관리됩니다:

- 게임 시작(Game Started) / 게임 종료(Game Ended) / 게임 리콜(Game Recalled)
- 캐시아웃 버튼 누름(Cashout Button Pressed)
- 지폐 수락(Bill Accepted) / 거부(Rejected) / 걸림(Jammed) / 스태커 꽉 참(Stacker Full)
- 잭팟 핸드페이 대기(Handpay Pending) / 취소(Canceled)
- 슬롯 도어 열림(Door Opened) / 닫힘(Closed)
- AC 전원 차단(Power Loss) / 복구(Restored)
- AFT 전자 자금 이체 완료(Transfer Complete) / 부분 완료(Partial) / 실패(Failed)

---

## 아키텍처 및 디자인 패턴

| 디자인 패턴                   | 적용 클래스                                                   |
| ----------------------------- | ------------------------------------------------------------- |
| **Singleton (싱글톤)**        | `GameManager`                                                 |
| **State Machine (상태 머신)** | `GameState` 열거형 + `GameManager.TransitionTo()`             |
| **Observer (옵서버)**         | `SpinCompleted`, `FreeSpinsStarted`, `BillAccepted` 등 이벤트 |
| **Command (커맨드)**          | `SASCommandProcessor.ProcessMessage()` + `switch` 식          |
| **Factory (팩토리)**          | `SymbolFactory.Create()`                                      |
| **Strategy (전략)**           | `IPayoutStrategy` (`PayTable`을 통해 확장 가능)               |

---

## 보안 메커니즘

- **SHA-256 체크섬**: 미터 데이터 파일 위변조 감지
- **PIN 인증**: 운영자 패널 접근 보호 (최소 4자리 이상)
- **SAS CRC-16**: 모든 프로토콜 메시지 무결성 검증
- **XOR 유효성 검증**: ID-003 직렬 메시지 검증
- **Lock 기반 동시성 제어**: 멀티스레드 환경에서 안전한 미터 접근 보장
- **암호학적 RNG**: 초기 시드 생성을 위한 `RandomNumberGenerator` 사용

---

## GLI / BMM 카지노 인증 규격 준수 항목

1. **인증 가능한 RNG**: `RNG` 클래스는 암호화 시드(`RandomNumberGenerator.Create()`)를 사용하며 감사를 위해 `Seed` 및 `CallCount`를 노출합니다.
2. **릴 스트립 문서화**: 5개 릴 스트립은 `Reel.GetDefaultStrip()`에 정의되어 있으며, ~96% RTP를 유지하도록 심볼이 균형 배치되어 있습니다.
3. **비초기화형 영구 계측기(Non-resettable Meters)**: 미터 값은 오직 누적 증가만 되며, 외부에서 감소시킬 수 없습니다.
4. **완전한 이벤트 감사 로그**: 모든 시스템 이벤트는 타임스탬프 및 시퀀스 번호와 함께 `EventLogger`에 영구 기록됩니다.
5. **게임 이력 보존**: `GameHistory` 클래스는 분쟁 해결 및 재현을 위해 릴 정지 위치 및 RNG 시드를 포함한 최근 500회 게임 정보를 보관합니다.

---

## 라이선스

카지노 게임 산업용 비공개 독점 소프트웨어입니다. All rights reserved.
