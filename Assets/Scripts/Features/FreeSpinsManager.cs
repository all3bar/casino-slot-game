using System;

namespace CasinoSlot.Features
{
    /// <summary>
    /// Manages the Free Spins feature state and award logic.
    /// </summary>
    public class FreeSpinsManager
    {
        private int _totalFreeSpinsAwarded;
        private int _totalFreeSpinsUsed;
        private long _totalFreeSpinsWin;
        private int _currentMultiplier;
        private bool _isActive;

        /// <summary>Gets whether free spins mode is currently active.</summary>
        public bool IsActive => _isActive;

        /// <summary>Gets free spins remaining in current session.</summary>
        public int Remaining { get; private set; }

        /// <summary>Gets the current win multiplier during free spins.</summary>
        public int Multiplier => _currentMultiplier;

        /// <summary>Gets total win during current free spins session.</summary>
        public long SessionWin { get; private set; }

        /// <summary>Gets total free spins awarded all time.</summary>
        public int TotalAwarded => _totalFreeSpinsAwarded;

        /// <summary>Raised when free spins mode starts.</summary>
        public event Action<int, int> FreeSpinsStarted; // (count, multiplier)

        /// <summary>Raised when a free spin is executed.</summary>
        public event Action<int, long> FreeSpinExecuted; // (remaining, spinWin)

        /// <summary>Raised when free spins mode ends.</summary>
        public event Action<long> FreeSpinsCompleted; // (totalWin)

        /// <summary>
        /// Awards free spins.
        /// </summary>
        /// <param name="count">Number of free spins to award.</param>
        /// <param name="multiplier">Win multiplier during free spins (default 3x).</param>
        public void Award(int count, int multiplier = 3)
        {
            if (count <= 0) return;
            _totalFreeSpinsAwarded += count;
            Remaining += count;
            _currentMultiplier = multiplier;
            SessionWin = 0;

            if (!_isActive)
            {
                _isActive = true;
                FreeSpinsStarted?.Invoke(Remaining, _currentMultiplier);
            }
        }

        /// <summary>
        /// Records the result of one free spin.
        /// </summary>
        /// <param name="spinWin">Win amount for this spin.</param>
        public void RecordFreeSpin(long spinWin)
        {
            if (!_isActive || Remaining <= 0) return;

            _totalFreeSpinsUsed++;
            Remaining--;
            SessionWin += spinWin * _currentMultiplier;
            _totalFreeSpinsWin += spinWin;

            FreeSpinExecuted?.Invoke(Remaining, spinWin);

            if (Remaining <= 0)
                EndSession();
        }

        private void EndSession()
        {
            _isActive = false;
            long totalWin = SessionWin;
            FreeSpinsCompleted?.Invoke(totalWin);
        }

        /// <summary>
        /// Resets the free spins session (e.g., on cashout).
        /// </summary>
        public void Reset()
        {
            _isActive = false;
            Remaining = 0;
            SessionWin = 0;
        }
    }
}
