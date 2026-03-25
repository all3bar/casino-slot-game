using System;
using CasinoSlot.Accounting;

namespace CasinoSlot.Core
{
    /// <summary>
    /// Defines the possible states of the slot machine game flow.
    /// Implements the State Machine pattern.
    /// </summary>
    public enum GameState
    {
        Idle,
        Spinning,
        ShowingResult,
        FreeSpins,
        BonusGame,
        Cashout,
        Error,
        Maintenance
    }

    /// <summary>
    /// Central game manager coordinating all game subsystems.
    /// Implements the Singleton pattern.
    /// </summary>
    public class GameManager
    {
        private static GameManager _instance;
        private static readonly object _lock = new object();

        private GameState _currentState;
        private readonly SlotMachine _slotMachine;
        private readonly MeterManager _meters;
        private readonly EventLogger _eventLogger;
        private readonly PersistenceManager _persistence;
        private readonly GameHistory _gameHistory;

        private SpinResult _lastSpinResult;

        /// <summary>Gets the singleton instance of GameManager.</summary>
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new GameManager();
                    }
                }
                return _instance;
            }
        }

        /// <summary>Gets the current game state.</summary>
        public GameState State => _currentState;

        /// <summary>Gets the slot machine engine.</summary>
        public SlotMachine SlotMachine => _slotMachine;

        /// <summary>Gets the meter manager.</summary>
        public MeterManager Meters => _meters;

        /// <summary>Gets the event logger.</summary>
        public EventLogger EventLogger => _eventLogger;

        /// <summary>Gets the game history manager.</summary>
        public GameHistory GameHistory => _gameHistory;

        /// <summary>Gets the last spin result.</summary>
        public SpinResult LastSpinResult => _lastSpinResult;

        /// <summary>Raised when the game state changes.</summary>
        public event Action<GameState, GameState> StateChanged;

        private GameManager()
        {
            _meters = new MeterManager();
            _eventLogger = new EventLogger();
            _persistence = new PersistenceManager(_meters, _eventLogger);
            _gameHistory = new GameHistory();
            _slotMachine = new SlotMachine(_meters);

            _currentState = GameState.Idle;

            // Wire up machine events
            _slotMachine.SpinCompleted += OnSpinCompleted;
            _slotMachine.FreeSpinsStarted += OnFreeSpinsStarted;
            _slotMachine.FreeSpinsEnded += OnFreeSpinsEnded;
            _slotMachine.BonusTriggered += OnBonusTriggered;

            // Load persisted data
            _persistence.Load();

            _eventLogger.Log(EventType.GameStartup, "Game Manager initialized.");
        }

        /// <summary>
        /// Resets the singleton (for unit testing purposes only).
        /// </summary>
        public static void ResetInstance()
        {
            lock (_lock) { _instance = null; }
        }

        /// <summary>
        /// Executes a spin if the game is in Idle or FreeSpins state.
        /// </summary>
        /// <returns>The spin result, or null if the spin could not be performed.</returns>
        public SpinResult ExecuteSpin()
        {
            if (_currentState != GameState.Idle && _currentState != GameState.FreeSpins)
                return null;

            TransitionTo(GameState.Spinning);

            var result = _slotMachine.Spin();
            if (result == null)
            {
                TransitionTo(GameState.Idle);
                return null;
            }

            _lastSpinResult = result;
            _gameHistory.RecordSpin(result);
            _persistence.Save();

            TransitionTo(GameState.ShowingResult);
            return result;
        }

        /// <summary>
        /// Acknowledges the spin result, returning to Idle (or FreeSpins if applicable).
        /// </summary>
        public void AcknowledgeResult()
        {
            if (_currentState != GameState.ShowingResult) return;

            if (_slotMachine.InFreeSpins)
                TransitionTo(GameState.FreeSpins);
            else
                TransitionTo(GameState.Idle);
        }

        /// <summary>
        /// Performs a cashout and transitions to Cashout state.
        /// </summary>
        public long Cashout()
        {
            if (_currentState == GameState.Spinning) return 0;

            TransitionTo(GameState.Cashout);
            long amount = _slotMachine.Cashout();
            _eventLogger.Log(EventType.Cashout, $"Cashout: {amount} credits.");
            TransitionTo(GameState.Idle);
            return amount;
        }

        /// <summary>
        /// Sets the machine to maintenance/operator mode.
        /// </summary>
        public void EnterMaintenance()
        {
            TransitionTo(GameState.Maintenance);
            _eventLogger.Log(EventType.DoorOpen, "Entered maintenance mode.");
        }

        /// <summary>
        /// Returns from maintenance mode to Idle.
        /// </summary>
        public void ExitMaintenance()
        {
            TransitionTo(GameState.Idle);
            _eventLogger.Log(EventType.DoorClose, "Exited maintenance mode.");
        }

        private void TransitionTo(GameState newState)
        {
            var old = _currentState;
            _currentState = newState;
            StateChanged?.Invoke(old, newState);
        }

        private void OnSpinCompleted(SpinResult result)
        {
            _eventLogger.Log(EventType.GameEnded,
                $"Spin complete. Bet={result.TotalBet} Win={result.TotalWin} FreeSpin={result.WasFreeSpin}");
        }

        private void OnFreeSpinsStarted(int count)
        {
            TransitionTo(GameState.FreeSpins);
            _eventLogger.Log(EventType.FreeSpinsStarted, $"Free spins started: {count} spins.");
        }

        private void OnFreeSpinsEnded()
        {
            _eventLogger.Log(EventType.FreeSpinsEnded, "Free spins ended.");
        }

        private void OnBonusTriggered()
        {
            TransitionTo(GameState.BonusGame);
            _eventLogger.Log(EventType.BonusTriggered, "Bonus game triggered.");
        }
    }
}
