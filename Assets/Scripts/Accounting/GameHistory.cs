using System;
using System.Collections.Generic;
using CasinoSlot.Core;

namespace CasinoSlot.Accounting
{
    /// <summary>
    /// Stores a summary record of a completed game spin.
    /// </summary>
    public class GameRecord
    {
        /// <summary>Gets the unique game sequence number.</summary>
        public long GameNumber { get; }

        /// <summary>Gets the UTC timestamp of the spin.</summary>
        public DateTime Timestamp { get; }

        /// <summary>Gets the stop positions of each reel.</summary>
        public int[] StopPositions { get; }

        /// <summary>Gets the total bet for this spin.</summary>
        public long TotalBet { get; }

        /// <summary>Gets the total win for this spin.</summary>
        public long TotalWin { get; }

        /// <summary>Gets whether this was a free spin.</summary>
        public bool WasFreeSpin { get; }

        /// <summary>Gets whether free spins were triggered.</summary>
        public bool FreeSpinsTriggered { get; }

        /// <summary>Gets whether the bonus game was triggered.</summary>
        public bool BonusTriggered { get; }

        /// <summary>Gets the RNG seed used.</summary>
        public long RngSeed { get; }

        public GameRecord(long gameNumber, SpinResult result)
        {
            GameNumber = gameNumber;
            Timestamp = DateTime.UtcNow;
            StopPositions = result.StopPositions;
            TotalBet = result.TotalBet;
            TotalWin = result.TotalWin;
            WasFreeSpin = result.WasFreeSpin;
            FreeSpinsTriggered = result.FreeSpinsTriggered;
            BonusTriggered = result.BonusTriggered;
            RngSeed = result.RngSeed;
        }
    }

    /// <summary>
    /// Maintains a circular buffer of recent game records for replay and audit.
    /// </summary>
    public class GameHistory
    {
        private readonly List<GameRecord> _records = new List<GameRecord>();
        private long _gameCounter;
        private readonly object _lock = new object();
        private const int MaxHistoryRecords = 500;

        /// <summary>Gets the total number of games recorded.</summary>
        public long TotalGames => _gameCounter;

        /// <summary>Gets a read-only snapshot of recent game records.</summary>
        public IReadOnlyList<GameRecord> Records
        {
            get { lock (_lock) return new List<GameRecord>(_records); }
        }

        /// <summary>
        /// Records a spin result in the history.
        /// </summary>
        public void RecordSpin(SpinResult result)
        {
            lock (_lock)
            {
                _gameCounter++;
                var record = new GameRecord(_gameCounter, result);
                _records.Add(record);
                if (_records.Count > MaxHistoryRecords)
                    _records.RemoveAt(0);
            }
        }

        /// <summary>
        /// Returns the last N game records.
        /// </summary>
        public List<GameRecord> GetLastN(int n)
        {
            lock (_lock)
            {
                int start = Math.Max(0, _records.Count - n);
                return _records.GetRange(start, _records.Count - start);
            }
        }

        /// <summary>
        /// Gets a record by game number (if still in history buffer).
        /// </summary>
        public GameRecord GetByGameNumber(long gameNumber)
        {
            lock (_lock)
                return _records.Find(r => r.GameNumber == gameNumber);
        }
    }
}
