using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CasinoSlot.Accounting
{
    /// <summary>
    /// Manages all accounting meters for the slot machine.
    /// Meters are persistent and protected with checksums.
    /// </summary>
    public class MeterManager
    {
        private readonly object _lock = new object();

        // Core meters (all in credits)
        private long _totalCoinIn;
        private long _totalCoinOut;
        private long _totalJackpot;
        private long _totalBillsAccepted;
        private long _totalBillsValue;
        private long _gamesPlayed;
        private long _gamesWon;
        private long _doorOpenCount;
        private long _powerUpCount;

        /// <summary>Gets total coin in (credits inserted/bet).</summary>
        public long TotalCoinIn { get { lock (_lock) return _totalCoinIn; } }

        /// <summary>Gets total coin out (credits paid out).</summary>
        public long TotalCoinOut { get { lock (_lock) return _totalCoinOut; } }

        /// <summary>Gets total jackpot amount paid.</summary>
        public long TotalJackpot { get { lock (_lock) return _totalJackpot; } }

        /// <summary>Gets total number of bills accepted.</summary>
        public long TotalBillsAccepted { get { lock (_lock) return _totalBillsAccepted; } }

        /// <summary>Gets total monetary value of bills accepted (in cents).</summary>
        public long TotalBillsValue { get { lock (_lock) return _totalBillsValue; } }

        /// <summary>Gets total games played.</summary>
        public long GamesPlayed { get { lock (_lock) return _gamesPlayed; } }

        /// <summary>Gets total games won (at least 1 credit returned).</summary>
        public long GamesWon { get { lock (_lock) return _gamesWon; } }

        /// <summary>Gets total door open events (for security auditing).</summary>
        public long DoorOpenCount { get { lock (_lock) return _doorOpenCount; } }

        /// <summary>Gets total power-up count.</summary>
        public long PowerUpCount { get { lock (_lock) return _powerUpCount; } }

        /// <summary>Gets the calculated Return to Player percentage.</summary>
        public double RTP
        {
            get
            {
                lock (_lock)
                {
                    if (_totalCoinIn == 0) return 0;
                    return (double)_totalCoinOut / _totalCoinIn * 100.0;
                }
            }
        }

        /// <summary>Gets the total drop (coin in - coin out).</summary>
        public long TotalDrop
        {
            get { lock (_lock) return _totalCoinIn - _totalCoinOut; }
        }

        public void IncrementCoinIn(long amount)
        {
            lock (_lock) { if (amount > 0) _totalCoinIn += amount; }
        }

        public void IncrementCoinOut(long amount)
        {
            lock (_lock) { if (amount > 0) _totalCoinOut += amount; }
        }

        public void IncrementJackpot(long amount)
        {
            lock (_lock) { if (amount > 0) { _totalJackpot += amount; _totalCoinOut += amount; } }
        }

        public void IncrementBillAccepted(int denominationCents)
        {
            lock (_lock)
            {
                _totalBillsAccepted++;
                _totalBillsValue += denominationCents;
            }
        }

        public void IncrementGamesPlayed()
        {
            lock (_lock) { _gamesPlayed++; }
        }

        public void IncrementGamesWon()
        {
            lock (_lock) { _gamesWon++; }
        }

        public void IncrementDoorOpen()
        {
            lock (_lock) { _doorOpenCount++; }
        }

        public void IncrementPowerUp()
        {
            lock (_lock) { _powerUpCount++; }
        }

        /// <summary>
        /// Serializes all meter values to a dictionary for persistence.
        /// </summary>
        public Dictionary<string, long> Serialize()
        {
            lock (_lock)
            {
                return new Dictionary<string, long>
                {
                    { "TotalCoinIn", _totalCoinIn },
                    { "TotalCoinOut", _totalCoinOut },
                    { "TotalJackpot", _totalJackpot },
                    { "TotalBillsAccepted", _totalBillsAccepted },
                    { "TotalBillsValue", _totalBillsValue },
                    { "GamesPlayed", _gamesPlayed },
                    { "GamesWon", _gamesWon },
                    { "DoorOpenCount", _doorOpenCount },
                    { "PowerUpCount", _powerUpCount }
                };
            }
        }

        /// <summary>
        /// Restores meter values from a serialized dictionary.
        /// </summary>
        public void Deserialize(Dictionary<string, long> data)
        {
            lock (_lock)
            {
                if (data == null) return;
                _totalCoinIn       = data.TryGetValue("TotalCoinIn", out long v1) ? v1 : 0;
                _totalCoinOut      = data.TryGetValue("TotalCoinOut", out long v2) ? v2 : 0;
                _totalJackpot      = data.TryGetValue("TotalJackpot", out long v3) ? v3 : 0;
                _totalBillsAccepted= data.TryGetValue("TotalBillsAccepted", out long v4) ? v4 : 0;
                _totalBillsValue   = data.TryGetValue("TotalBillsValue", out long v5) ? v5 : 0;
                _gamesPlayed       = data.TryGetValue("GamesPlayed", out long v6) ? v6 : 0;
                _gamesWon          = data.TryGetValue("GamesWon", out long v7) ? v7 : 0;
                _doorOpenCount     = data.TryGetValue("DoorOpenCount", out long v8) ? v8 : 0;
                _powerUpCount      = data.TryGetValue("PowerUpCount", out long v9) ? v9 : 0;
            }
        }
    }
}
