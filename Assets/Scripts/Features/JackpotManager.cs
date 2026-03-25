using System;

namespace CasinoSlot.Features
{
    /// <summary>
    /// Manages a progressive jackpot pool.
    /// The jackpot grows with each bet and can be won based on RNG probability.
    /// </summary>
    public class JackpotManager
    {
        private long _currentJackpot;
        private long _seedAmount;
        private double _contributionRate;
        private double _triggerProbability;
        private long _totalJackpotsWon;
        private long _totalAmountPaid;
        private readonly object _lock = new object();

        /// <summary>Gets the current jackpot value in credits.</summary>
        public long CurrentJackpot { get { lock (_lock) return _currentJackpot; } }

        /// <summary>Gets the seed amount (jackpot resets to this after win).</summary>
        public long SeedAmount => _seedAmount;

        /// <summary>Gets the total number of jackpots won.</summary>
        public long TotalJackpotsWon => _totalJackpotsWon;

        /// <summary>Gets total credits paid in jackpots.</summary>
        public long TotalAmountPaid => _totalAmountPaid;

        /// <summary>Raised when the jackpot is won.</summary>
        public event Action<long> JackpotWon;

        /// <summary>Raised when the jackpot amount changes.</summary>
        public event Action<long> JackpotUpdated;

        /// <summary>
        /// Initializes the jackpot manager.
        /// </summary>
        /// <param name="seedAmount">Initial jackpot amount in credits.</param>
        /// <param name="contributionRate">Fraction of each bet that goes to the jackpot (e.g., 0.01 = 1%).</param>
        /// <param name="triggerProbabilityPerSpin">Probability of jackpot trigger per winning spin (e.g., 1/50000).</param>
        public JackpotManager(long seedAmount = 1000, double contributionRate = 0.01,
            double triggerProbabilityPerSpin = 1.0 / 50000.0)
        {
            _seedAmount = seedAmount;
            _currentJackpot = seedAmount;
            _contributionRate = contributionRate;
            _triggerProbability = triggerProbabilityPerSpin;
        }

        /// <summary>
        /// Adds a contribution to the jackpot pool from a bet.
        /// </summary>
        /// <param name="betAmount">Total bet amount for the spin.</param>
        public void Contribute(long betAmount)
        {
            if (betAmount <= 0) return;
            long contribution = (long)Math.Max(1, betAmount * _contributionRate);
            lock (_lock)
            {
                _currentJackpot += contribution;
            }
            JackpotUpdated?.Invoke(_currentJackpot);
        }

        /// <summary>
        /// Checks whether the jackpot is won on this spin.
        /// Uses the supplied random value [0,1).
        /// </summary>
        /// <param name="randomValue">A random value in [0.0, 1.0).</param>
        /// <returns>True if jackpot is won.</returns>
        public bool CheckTrigger(double randomValue)
        {
            return randomValue < _triggerProbability;
        }

        /// <summary>
        /// Triggers and pays the jackpot. Resets to seed amount.
        /// </summary>
        /// <returns>The jackpot amount won.</returns>
        public long TriggerJackpot()
        {
            long amount;
            lock (_lock)
            {
                amount = _currentJackpot;
                _currentJackpot = _seedAmount;
                _totalJackpotsWon++;
                _totalAmountPaid += amount;
            }
            JackpotWon?.Invoke(amount);
            JackpotUpdated?.Invoke(_currentJackpot);
            return amount;
        }

        /// <summary>
        /// Resets the jackpot to the seed amount (e.g., on system reset).
        /// </summary>
        public void Reset()
        {
            lock (_lock) { _currentJackpot = _seedAmount; }
            JackpotUpdated?.Invoke(_currentJackpot);
        }
    }
}
