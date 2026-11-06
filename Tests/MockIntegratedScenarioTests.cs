using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using CasinoSlot.Core;
using CasinoSlot.Accounting;
using CasinoSlot.Hardware;
using CasinoSlot.Input;
using CasinoSlot.SAS;
using CasinoSlot.UI;

namespace CasinoSlot.Tests
{
    /// <summary>
    /// 실제 카지노 물리 하드웨어(시리얼 포트, 지폐인식기, 버튼 패널) 없이
    /// 전체 게임 라이프사이클을 가상으로 검증하는 엔드투엔드 목업(Mock) 통합 테스트입니다.
    /// </summary>
    public class MockIntegratedScenarioTests : IDisposable
    {
        private readonly string _tempDataDir;

        public MockIntegratedScenarioTests()
        {
            GameManager.ResetInstance();
            _tempDataDir = Path.Combine(Path.GetTempPath(), "CasinoSlot_MockTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDataDir);
        }

        public void Dispose()
        {
            GameManager.ResetInstance();
            if (Directory.Exists(_tempDataDir))
            {
                try { Directory.Delete(_tempDataDir, true); }
                catch { /* Ignore cleanup issues */ }
            }
        }

        // =========================================================================
        // 시나리오 1: 지폐 인식기(BillAcceptor) 모의 투입 -> 크레딧 충전 및 회계 미터 반영
        // =========================================================================
        [Fact]
        public void Scenario1_MockBillAcceptance_FlowsToCreditsAndMeters()
        {
            var game = GameManager.Instance;
            long beforeCredits = game.SlotMachine.Credits;
            long beforeBillsValue = game.Meters.TotalBillsValue;
            long beforeBillsAccepted = game.Meters.TotalBillsAccepted;

            var billAcceptor = new BillAcceptor(new[] { 100, 500, 1000, 2000, 5000 });

            // 지폐 인식기 이벤트 구독 (실제 카지노 머신에서의 배선)
            billAcceptor.BillAccepted += (s, e) =>
            {
                game.SlotMachine.AddCredits(e.DenominationCents);
                game.Meters.IncrementBillAccepted(e.DenominationCents);
            };

            billAcceptor.Enable();
            Assert.Equal(BillAcceptorState.Idle, billAcceptor.State);

            // $20 (2000 센트) 지폐 투입 모의 (에스크로 진입 -> 적재 완료)
            billAcceptor.ProcessEscrow(2000);
            billAcceptor.NotifyStacked(2000);

            // 검증: 크레딧 2000 추가 및 회계 미터 정상 적립
            Assert.Equal(beforeCredits + 2000L, game.SlotMachine.Credits);
            Assert.Equal(beforeBillsValue + 2000L, game.Meters.TotalBillsValue);
            Assert.Equal(beforeBillsAccepted + 1L, game.Meters.TotalBillsAccepted);
        }

        // =========================================================================
        // 시나리오 2: 가상 키보드 입력(Spacebar) 주입 -> InputManager -> 릴 스핀 및 상태 전이
        // =========================================================================
        [Fact]
        public void Scenario2_MockKeyboardInput_DispatchesSpinAction_ExecutesGameCycle()
        {
            var game = GameManager.Instance;
            var ui = new MainGameUI(game);

            // 키보드 하드웨어 대신 가상 입력 큐(Queue)를 사용하는 Mock Keyboard 생성
            var keyQueue = new Queue<ConsoleKeyInfo>();
            var mockKeyboard = new KeyboardInputProvider(
                keyAvailable: () => keyQueue.Count > 0,
                readKey: () => keyQueue.Dequeue()
            );

            var inputManager = new InputManager();
            inputManager.RegisterProvider(mockKeyboard);
            inputManager.ConnectUI(ui);

            // 크레딧 1000 충전 (기본 베팅: 20라인 x 1크레딧 = 20크레딧)
            game.SlotMachine.AddCredits(1000);
            Assert.Equal(GameState.Idle, game.State);

            // 플레이어가 스페이스바(Spin)를 누름
            keyQueue.Enqueue(new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false));

            // 프레임 틱 모의
            inputManager.Update();

            // 검증: 상태가 ShowingResult로 전이되었고 스핀 결과가 생성됨
            Assert.Equal(GameState.ShowingResult, game.State);
            Assert.NotNull(game.LastSpinResult);
            Assert.Equal(5, game.LastSpinResult.StopPositions.Length);
            Assert.True(game.SlotMachine.Credits >= 0);

            // 플레이어가 결과 확인(Acknowledge) 후 다시 Idle 상태로 복귀
            game.AcknowledgeResult();
            Assert.Equal(GameState.Idle, game.State);
        }

        // =========================================================================
        // 시나리오 3: 릴 스핀 후 SAS 6.0 카지노 호스트 폴링 명령 처리 (크레딧 & 미터 질의)
        // =========================================================================
        [Fact]
        public void Scenario3_AfterSpin_SASHostPollsMeters_ValidatesResponsesAndCRC()
        {
            var game = GameManager.Instance;
            var identity = new MachineIdentity { SASAddress = 0x01 };
            var sasProcessor = new SASCommandProcessor(identity, game.Meters, game, game.EventLogger);
            var hostProtocol = new SASProtocol(0x01);

            // 초기 크레딧 충전 및 1회 스핀
            long beforeCoinIn = game.Meters.TotalCoinIn;
            long beforeGamesPlayed = game.Meters.GamesPlayed;

            game.SlotMachine.AddCredits(500);
            long initialBet = game.SlotMachine.TotalBet; // 20
            var spinResult = game.ExecuteSpin();
            Assert.NotNull(spinResult);

            // 1) 호스트가 0x2E (Send Current Credits) 질의 전송 모의
            byte[] pollCredits = hostProtocol.BuildResponse(SASCommandCodes.SendCurrentCredits, null);
            byte[] respCredits = sasProcessor.ProcessMessage(pollCredits);

            Assert.NotNull(respCredits);
            Assert.True(SASProtocol.ValidateCRC(respCredits));
            Assert.Equal(0x01, respCredits[0]); // 머신 주소
            Assert.Equal(SASCommandCodes.SendCurrentCredits, respCredits[1]);

            // 2) 호스트가 0x54 (Send Total Coin In) 누적 베팅액 질의
            byte[] pollCoinIn = hostProtocol.BuildResponse(SASCommandCodes.SendTotalCoinIn, null);
            byte[] respCoinIn = sasProcessor.ProcessMessage(pollCoinIn);

            Assert.NotNull(respCoinIn);
            Assert.True(SASProtocol.ValidateCRC(respCoinIn));
            Assert.Equal(beforeCoinIn + 500 + initialBet, game.Meters.TotalCoinIn);

            // 3) 호스트가 0x58 (Send Games Played) 플레이 게임 수 질의
            byte[] pollGames = hostProtocol.BuildResponse(SASCommandCodes.SendGamesPlayed, null);
            byte[] respGames = sasProcessor.ProcessMessage(pollGames);

            Assert.NotNull(respGames);
            Assert.True(SASProtocol.ValidateCRC(respGames));
            Assert.Equal(beforeGamesPlayed + 1L, game.Meters.GamesPlayed);
        }

        // =========================================================================
        // 시나리오 4: 코인 펄스 투입 모의 -> 캐시아웃 및 호퍼 방출 확인
        // =========================================================================
        [Fact]
        public void Scenario4_MockPulseCoinIn_And_CashoutHopperPayout()
        {
            var game = GameManager.Instance;
            long beforeCoinIn = game.Meters.TotalCoinIn;
            long beforeCoinOut = game.Meters.TotalCoinOut;

            // 코인 1회 펄스당 1개 코인, 코인당 10 크레딧 설정
            var pulseCtrl = new PulseController(coinsPerPulseIn: 1, creditsPerCoin: 10);

            pulseCtrl.CoinAccepted += credits =>
            {
                game.SlotMachine.AddCredits(credits);
            };

            // 코인 3개 투입 모의 (펄스 3회 = 30 크레딧)
            pulseCtrl.OnCoinInPulse();
            pulseCtrl.OnCoinInPulse();
            pulseCtrl.OnCoinInPulse();

            Assert.Equal(30L, game.SlotMachine.Credits);
            Assert.Equal(beforeCoinIn + 30L, game.Meters.TotalCoinIn);

            // 캐시아웃 실행
            long cashedOut = game.Cashout();

            Assert.Equal(30L, cashedOut);
            Assert.Equal(0L, game.SlotMachine.Credits);
            Assert.Equal(beforeCoinOut + 30L, game.Meters.TotalCoinOut);
        }

        // =========================================================================
        // 시나리오 5: SHA-256 데이터 무결성 검증 (저장 및 위변조 방지 Mock)
        // =========================================================================
        [Fact]
        public void Scenario5_PersistenceManager_DetectsDataTampering()
        {
            var meters = new MeterManager();
            var logger = new EventLogger();
            var persistence = new PersistenceManager(meters, logger, _tempDataDir);

            meters.IncrementCoinIn(50000);
            meters.IncrementCoinOut(45000);
            bool saveResult = persistence.Save();
            Assert.True(saveResult);

            string metersFilePath = Path.Combine(_tempDataDir, "meters.json");
            Assert.True(File.Exists(metersFilePath));

            // 원본 데이터로 정상 로드 확인
            var cleanMeters = new MeterManager();
            var cleanPersistence = new PersistenceManager(cleanMeters, logger, _tempDataDir);
            Assert.True(cleanPersistence.Load());
            Assert.Equal(50000L, cleanMeters.TotalCoinIn);

            // 데이터 파일 악의적 변조 (체크섬 불일치 유도)
            string rawContent = File.ReadAllText(metersFilePath);
            string tamperedContent = rawContent.Replace("50000", "9999999");
            File.WriteAllText(metersFilePath, tamperedContent);

            // 변조된 파일 로드 시도 -> 실패(false) 반환 확인
            var corruptedMeters = new MeterManager();
            var corruptedPersistence = new PersistenceManager(corruptedMeters, logger, _tempDataDir);
            bool loadAfterTamper = corruptedPersistence.Load();

            Assert.False(loadAfterTamper);
            Assert.Equal(0L, corruptedMeters.TotalCoinIn);
        }
    }
}
