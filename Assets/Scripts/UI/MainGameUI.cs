using System;
using System.Collections.Generic;
using CasinoSlot.Core;
using CasinoSlot.Accounting;
using CasinoSlot.Features;

namespace CasinoSlot.UI
{
    /// <summary>
    /// Represents a UI notification for display to the player.
    /// </summary>
    public class UINotification
    {
        public string Message { get; }
        public string Type { get; } // "win", "info", "error", "freespins", "bonus"
        public UINotification(string message, string type = "info")
        {
            Message = message;
            Type = type;
        }
    }

    /// <summary>
    /// Main game UI controller. Manages game display state and player input.
    /// In a Unity project, this would extend MonoBehaviour; here it is a pure C# class
    /// for testability and separation of concerns.
    /// </summary>
    public class MainGameUI
    {
        private readonly GameManager _gameManager;
        private readonly JackpotManager _jackpotManager;
        private readonly FreeSpinsManager _freeSpinsManager;

        // Display state
        private long _displayedCredits;
        private long _displayedWin;
        private long _displayedBet;
        private string _statusMessage;
        private bool _spinButtonEnabled;
        private bool _isAnimating;

        // Notification queue for display
        private readonly Queue<UINotification> _notifications = new Queue<UINotification>();

        /// <summary>Gets the credits displayed to the player.</summary>
        public long DisplayedCredits => _displayedCredits;

        /// <summary>Gets the win amount displayed after the last spin.</summary>
        public long DisplayedWin => _displayedWin;

        /// <summary>Gets the total bet displayed.</summary>
        public long DisplayedBet => _displayedBet;

        /// <summary>Gets the current status message.</summary>
        public string StatusMessage => _statusMessage;

        /// <summary>Gets whether the spin button is enabled.</summary>
        public bool SpinButtonEnabled => _spinButtonEnabled;

        /// <summary>Gets whether the UI is currently animating a result.</summary>
        public bool IsAnimating => _isAnimating;

        /// <summary>Gets the current jackpot display value.</summary>
        public long JackpotDisplay => _jackpotManager?.CurrentJackpot ?? 0;

        /// <summary>Raised when the display needs to be refreshed.</summary>
        public event Action<MainGameUI> DisplayUpdated;

        /// <summary>Raised when a win animation should play for the given paylines.</summary>
        public event Action<List<PaylineResult>> PlayWinAnimation;

        /// <summary>Raised when free spins intro should be shown.</summary>
        public event Action<int> ShowFreeSpinsIntro;

        /// <summary>
        /// Initializes the main game UI.
        /// </summary>
        public MainGameUI(GameManager gameManager, JackpotManager jackpotManager = null,
            FreeSpinsManager freeSpinsManager = null)
        {
            _gameManager = gameManager;
            _jackpotManager = jackpotManager;
            _freeSpinsManager = freeSpinsManager;

            // Wire up events
            _gameManager.StateChanged += OnGameStateChanged;
            _gameManager.SlotMachine.SpinCompleted += OnSpinCompleted;
            _gameManager.SlotMachine.CreditsChanged += OnCreditsChanged;

            if (_jackpotManager != null)
                _jackpotManager.JackpotUpdated += OnJackpotUpdated;

            OnGameStateChanged(GameState.Idle, _gameManager.State);
            RefreshDisplay();
        }

        /// <summary>
        /// Called when the player presses the Spin button.
        /// </summary>
        public void OnSpinButtonPressed()
        {
            if (!_spinButtonEnabled || _isAnimating) return;

            _displayedWin = 0;
            _spinButtonEnabled = false;
            _statusMessage = _gameManager.SlotMachine.InFreeSpins ? "FREE SPIN!" : "SPINNING...";
            RefreshDisplay();

            _gameManager.ExecuteSpin();
        }

        /// <summary>
        /// Called when the player presses the Max Bet button.
        /// </summary>
        public void OnMaxBetPressed()
        {
            if (_gameManager.State != GameState.Idle) return;
            _gameManager.SlotMachine.MaxBet();
            RefreshDisplay();
        }

        /// <summary>
        /// Called when the player changes bet per line.
        /// </summary>
        public void OnBetPerLineChanged(long newBet)
        {
            _gameManager.SlotMachine.SetBetPerLine(newBet);
            RefreshDisplay();
        }

        /// <summary>
        /// Called when the player changes the number of active lines.
        /// </summary>
        public void OnActiveLinesChanged(int lines)
        {
            _gameManager.SlotMachine.SetActiveLines(lines);
            RefreshDisplay();
        }

        /// <summary>
        /// Called when the player presses the Cashout button.
        /// </summary>
        public void OnCashoutPressed()
        {
            if (_gameManager.State == GameState.Spinning) return;
            long amount = _gameManager.Cashout();
            EnqueueNotification(new UINotification($"Cashout: {amount} credits", "info"));
            RefreshDisplay();
        }

        /// <summary>
        /// Called when the Operator Menu button is pressed from any input device.
        /// The actual authentication is handled by <see cref="OperatorUI"/>.
        /// </summary>
        public void OnOperatorMenuRequested()
        {
            EnqueueNotification(new UINotification("Operator menu requested", "info"));
            RefreshDisplay();
        }

        /// <summary>
        /// Acknowledges the current result display and returns to Idle.
        /// </summary>
        public void AcknowledgeResult()
        {
            if (_gameManager.State == GameState.ShowingResult)
            {
                _isAnimating = false;
                _gameManager.AcknowledgeResult();
            }
        }

        /// <summary>
        /// Returns the next pending notification, or null.
        /// </summary>
        public UINotification DequeueNotification()
        {
            return _notifications.Count > 0 ? _notifications.Dequeue() : null;
        }

        private void OnGameStateChanged(GameState old, GameState newState)
        {
            switch (newState)
            {
                case GameState.Idle:
                    _spinButtonEnabled = true;
                    _statusMessage = "Press SPIN";
                    _isAnimating = false;
                    break;
                case GameState.Spinning:
                    _spinButtonEnabled = false;
                    _statusMessage = "SPINNING...";
                    break;
                case GameState.ShowingResult:
                    _spinButtonEnabled = false;
                    break;
                case GameState.FreeSpins:
                    _spinButtonEnabled = true;
                    _statusMessage = $"FREE SPINS: {_gameManager.SlotMachine.FreeSpinsRemaining} remaining";
                    break;
                case GameState.BonusGame:
                    _spinButtonEnabled = false;
                    _statusMessage = "BONUS GAME!";
                    break;
                case GameState.Maintenance:
                    _spinButtonEnabled = false;
                    _statusMessage = "MAINTENANCE MODE";
                    break;
            }
            RefreshDisplay();
        }

        private void OnSpinCompleted(SpinResult result)
        {
            _displayedWin = result.TotalWin;
            _displayedBet = result.TotalBet;

            if (result.TotalWin > 0)
            {
                _statusMessage = $"WIN: {result.TotalWin} CREDITS!";
                _isAnimating = true;
                PlayWinAnimation?.Invoke(result.WinningLines);
            }

            if (result.FreeSpinsTriggered)
            {
                EnqueueNotification(new UINotification(
                    $"{result.FreeSpinsAwarded} FREE SPINS AWARDED!", "freespins"));
                ShowFreeSpinsIntro?.Invoke(result.FreeSpinsAwarded);
            }

            if (result.BonusTriggered)
            {
                EnqueueNotification(new UINotification("BONUS GAME TRIGGERED!", "bonus"));
            }

            if (result.JackpotTriggered)
            {
                EnqueueNotification(new UINotification("JACKPOT WIN!!!", "win"));
            }

            RefreshDisplay();
        }

        private void OnCreditsChanged(long newCredits)
        {
            _displayedCredits = newCredits;
            RefreshDisplay();
        }

        private void OnJackpotUpdated(long newJackpot)
        {
            RefreshDisplay();
        }

        private void EnqueueNotification(UINotification notification)
        {
            _notifications.Enqueue(notification);
        }

        private void RefreshDisplay()
        {
            _displayedCredits = _gameManager.SlotMachine.Credits;
            _displayedBet = _gameManager.SlotMachine.TotalBet;
            DisplayUpdated?.Invoke(this);
        }
    }
}
