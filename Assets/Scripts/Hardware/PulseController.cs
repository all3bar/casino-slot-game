using System;

namespace CasinoSlot.Hardware
{
    /// <summary>
    /// Controls coin-in pulse input and hopper coin-out pulse output.
    /// Implements the Pulse protocol for coin acceptors and hoppers.
    /// </summary>
    public class PulseController
    {
        private readonly int _coinsPerPulseIn;
        private readonly int _creditsPerCoin;
        private int _pulseOutCount;
        private bool _isRunning;

        /// <summary>Gets total coins counted in.</summary>
        public int TotalCoinsIn { get; private set; }

        /// <summary>Gets total coins paid out via hopper.</summary>
        public int TotalCoinsOut { get; private set; }

        /// <summary>Gets the credit value per coin (denomination).</summary>
        public int CreditsPerCoin => _creditsPerCoin;

        /// <summary>Raised when a coin pulse is received from the coin acceptor.</summary>
        public event Action<int> CoinAccepted;

        /// <summary>Raised when the hopper completes a payout.</summary>
        public event Action<int> HopperPayout;

        /// <summary>
        /// Initializes the pulse controller.
        /// </summary>
        /// <param name="coinsPerPulseIn">Number of coins represented by each input pulse.</param>
        /// <param name="creditsPerCoin">Credits value per coin.</param>
        public PulseController(int coinsPerPulseIn = 1, int creditsPerCoin = 1)
        {
            _coinsPerPulseIn = coinsPerPulseIn;
            _creditsPerCoin = creditsPerCoin;
        }

        /// <summary>
        /// Called when a coin-in pulse is detected from hardware.
        /// </summary>
        public void OnCoinInPulse()
        {
            int coins = _coinsPerPulseIn;
            TotalCoinsIn += coins;
            int credits = coins * _creditsPerCoin;
            CoinAccepted?.Invoke(credits);
        }

        /// <summary>
        /// Starts a hopper payout for the given number of credits.
        /// </summary>
        /// <param name="creditsToPay">Credits to pay out via hopper.</param>
        public void StartHopperPayout(int creditsToPay)
        {
            if (_isRunning) return;
            int coinsToDispense = creditsToPay / _creditsPerCoin;
            _pulseOutCount = coinsToDispense;
            _isRunning = true;
            DispenseNextCoin();
        }

        /// <summary>
        /// Called when a hopper coin-out pulse is confirmed by hardware.
        /// </summary>
        public void OnHopperPulseConfirmed()
        {
            if (!_isRunning || _pulseOutCount <= 0) return;
            TotalCoinsOut++;
            _pulseOutCount--;

            if (_pulseOutCount == 0)
            {
                _isRunning = false;
                HopperPayout?.Invoke(TotalCoinsOut);
            }
            else
            {
                DispenseNextCoin();
            }
        }

        /// <summary>
        /// Stops the hopper (emergency stop).
        /// </summary>
        public void StopHopper()
        {
            _isRunning = false;
            _pulseOutCount = 0;
        }

        private void DispenseNextCoin()
        {
            // In real implementation, this would trigger a GPIO output pulse
            // For simulation, call OnHopperPulseConfirmed() immediately
        }
    }

    /// <summary>
    /// Controls the hopper motor for coin dispensing.
    /// </summary>
    public class HopperController
    {
        private readonly PulseController _pulseController;
        private bool _hopperEmpty;
        private bool _hopperJam;

        /// <summary>Gets whether the hopper is currently dispensing.</summary>
        public bool IsDispensing { get; private set; }

        /// <summary>Gets whether the hopper is reported empty.</summary>
        public bool IsEmpty => _hopperEmpty;

        /// <summary>Gets whether a hopper jam is detected.</summary>
        public bool IsJammed => _hopperJam;

        /// <summary>Raised when hopper becomes empty during payout.</summary>
        public event Action HopperEmpty;

        /// <summary>Raised when a hopper jam is detected.</summary>
        public event Action HopperJam;

        public HopperController(PulseController pulseController)
        {
            _pulseController = pulseController;
            _pulseController.HopperPayout += OnPayoutComplete;
        }

        /// <summary>
        /// Requests a payout of the given credit amount via hopper.
        /// Returns false if hopper is unavailable.
        /// </summary>
        public bool RequestPayout(int credits)
        {
            if (_hopperEmpty || _hopperJam || IsDispensing) return false;
            IsDispensing = true;
            _pulseController.StartHopperPayout(credits);
            return true;
        }

        /// <summary>Called when a jam is detected by hardware.</summary>
        public void NotifyJam()
        {
            _hopperJam = true;
            IsDispensing = false;
            HopperJam?.Invoke();
        }

        /// <summary>Called when hopper is empty.</summary>
        public void NotifyEmpty()
        {
            _hopperEmpty = true;
            IsDispensing = false;
            HopperEmpty?.Invoke();
        }

        private void OnPayoutComplete(int coinsDispensed)
        {
            IsDispensing = false;
        }
    }
}
