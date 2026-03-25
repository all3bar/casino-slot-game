using System;
using System.Collections.Generic;
using CasinoSlot.Accounting;

namespace CasinoSlot.Core
{
    /// <summary>
    /// Represents the state of a single spin result.
    /// </summary>
    public class SpinResult
    {
        /// <summary>Gets the visible symbol grid [reel, row].</summary>
        public SymbolType[,] Grid { get; set; }

        /// <summary>Gets the list of winning payline results.</summary>
        public List<PaylineResult> WinningLines { get; set; } = new List<PaylineResult>();

        /// <summary>Gets the total win amount in credits.</summary>
        public long TotalWin { get; set; }

        /// <summary>Gets the total bet for this spin.</summary>
        public long TotalBet { get; set; }

        /// <summary>Gets the number of scatter symbols.</summary>
        public int ScatterCount { get; set; }

        /// <summary>Gets the number of bonus symbols.</summary>
        public int BonusCount { get; set; }

        /// <summary>Gets whether this spin triggered free spins.</summary>
        public bool FreeSpinsTriggered => ScatterCount >= 3;

        /// <summary>Gets the free spins awarded (0 if not triggered).</summary>
        public int FreeSpinsAwarded { get; set; }

        /// <summary>Gets whether this spin triggered the bonus game.</summary>
        public bool BonusTriggered => BonusCount >= 3;

        /// <summary>Gets whether this spin triggered a jackpot.</summary>
        public bool JackpotTriggered { get; set; }

        /// <summary>Gets whether this was a free spin.</summary>
        public bool WasFreeSpin { get; set; }

        /// <summary>Gets the RNG seed used for this spin (for replay).</summary>
        public long RngSeed { get; set; }

        /// <summary>Gets stop positions for each reel.</summary>
        public int[] StopPositions { get; set; } = new int[5];
    }

    /// <summary>
    /// Core slot machine logic engine. Manages reels, bet, credits, and spin evaluation.
    /// </summary>
    public class SlotMachine
    {
        private readonly Reel[] _reels;
        private readonly PaylineManager _paylineManager;
        private readonly PayTable _payTable;
        private readonly RNG _rng;
        private readonly MeterManager _meters;

        // Game configuration
        private long _credits;
        private long _betPerLine;
        private int _activeLinesCount;
        private readonly long[] _validDenominations = { 1, 5, 10, 25, 100 };

        // Free spins state
        private int _freeSpinsRemaining;
        private int _freeSpinsMultiplier = 1;

        /// <summary>Gets the current credits balance.</summary>
        public long Credits => _credits;

        /// <summary>Gets the current bet per line in credits.</summary>
        public long BetPerLine => _betPerLine;

        /// <summary>Gets the current number of active lines.</summary>
        public int ActiveLinesCount => _activeLinesCount;

        /// <summary>Gets the total bet amount (BetPerLine * ActiveLines).</summary>
        public long TotalBet => _betPerLine * _activeLinesCount;

        /// <summary>Gets the number of free spins remaining.</summary>
        public int FreeSpinsRemaining => _freeSpinsRemaining;

        /// <summary>Gets whether the machine is currently in free spins mode.</summary>
        public bool InFreeSpins => _freeSpinsRemaining > 0;

        /// <summary>Gets the RNG instance used by this machine.</summary>
        public RNG RNG => _rng;

        /// <summary>Raised when a spin is completed.</summary>
        public event Action<SpinResult> SpinCompleted;

        /// <summary>Raised when free spins mode starts.</summary>
        public event Action<int> FreeSpinsStarted;

        /// <summary>Raised when free spins mode ends.</summary>
        public event Action FreeSpinsEnded;

        /// <summary>Raised when the bonus game is triggered.</summary>
        public event Action BonusTriggered;

        /// <summary>Raised when credits change.</summary>
        public event Action<long> CreditsChanged;

        /// <summary>
        /// Initializes the slot machine with default configuration.
        /// </summary>
        /// <param name="meters">The meter manager for accounting.</param>
        public SlotMachine(MeterManager meters)
        {
            _meters = meters;
            _payTable = new PayTable();
            _paylineManager = new PaylineManager(_payTable);
            _rng = new RNG();

            // Initialize 5 reels
            _reels = new Reel[5];
            for (int i = 0; i < 5; i++)
                _reels[i] = new Reel(i, Reel.GetDefaultStrip(i));

            _credits = 0;
            _betPerLine = 1;
            _activeLinesCount = 20;
            _freeSpinsRemaining = 0;
        }

        /// <summary>
        /// Adds credits to the machine balance.
        /// </summary>
        /// <param name="amount">Amount in credits to add.</param>
        public void AddCredits(long amount)
        {
            if (amount <= 0) return;
            _credits += amount;
            _meters.IncrementCoinIn(amount);
            CreditsChanged?.Invoke(_credits);
        }

        /// <summary>
        /// Sets the bet per line.
        /// </summary>
        /// <param name="bet">New bet per line amount.</param>
        public bool SetBetPerLine(long bet)
        {
            if (bet < 1) return false;
            _betPerLine = bet;
            return true;
        }

        /// <summary>
        /// Sets the number of active paylines.
        /// </summary>
        public bool SetActiveLines(int lines)
        {
            if (lines < 1 || lines > 20) return false;
            _activeLinesCount = lines;
            _paylineManager.ActiveLinesCount = lines;
            return true;
        }

        /// <summary>
        /// Sets to max bet (max BetPerLine on all lines).
        /// </summary>
        public void MaxBet()
        {
            _betPerLine = 10;
            _activeLinesCount = 20;
            _paylineManager.ActiveLinesCount = 20;
        }

        /// <summary>
        /// Performs a spin. Returns null if insufficient credits.
        /// </summary>
        public SpinResult Spin()
        {
            bool isFreeSpinUse = InFreeSpins;

            if (!isFreeSpinUse)
            {
                if (_credits < TotalBet)
                    return null;
                _credits -= TotalBet;
            }

            // Spin all reels
            var stopPositions = new int[5];
            for (int i = 0; i < 5; i++)
            {
                _reels[i].Spin(_rng);
                stopPositions[i] = _reels[i].StopPosition;
            }

            // Build the visible grid
            var grid = BuildGrid();

            // Evaluate winning lines
            var winningLines = _paylineManager.Evaluate(grid, _betPerLine);
            long lineWins = 0;
            foreach (var w in winningLines)
                lineWins += w.WinAmount;

            // Evaluate scatters
            int scatterCount = _paylineManager.CountScatters(grid);
            long scatterWin = _payTable.GetScatterPayout(scatterCount, TotalBet);
            int freeSpinsAwarded = _payTable.GetFreeSpins(scatterCount);

            // Evaluate bonus
            int bonusCount = _paylineManager.CountBonus(grid);

            long totalWin = lineWins + scatterWin;

            // Apply free spins multiplier
            if (isFreeSpinUse)
            {
                totalWin *= _freeSpinsMultiplier;
                _freeSpinsRemaining--;
            }

            // Add winnings
            if (totalWin > 0)
            {
                _credits += totalWin;
                _meters.IncrementCoinOut(totalWin);
            }

            // Update meters
            _meters.IncrementGamesPlayed();
            if (!isFreeSpinUse)
                _meters.IncrementCoinIn(TotalBet);
            if (totalWin > 0)
                _meters.IncrementGamesWon();

            CreditsChanged?.Invoke(_credits);

            var result = new SpinResult
            {
                Grid = grid,
                WinningLines = winningLines,
                TotalWin = totalWin,
                TotalBet = isFreeSpinUse ? 0 : TotalBet,
                ScatterCount = scatterCount,
                BonusCount = bonusCount,
                FreeSpinsAwarded = freeSpinsAwarded,
                WasFreeSpin = isFreeSpinUse,
                RngSeed = _rng.Seed,
                StopPositions = stopPositions
            };

            SpinCompleted?.Invoke(result);

            // Handle free spins trigger
            if (freeSpinsAwarded > 0 && !isFreeSpinUse)
            {
                _freeSpinsRemaining += freeSpinsAwarded;
                FreeSpinsStarted?.Invoke(_freeSpinsRemaining);
            }

            if (isFreeSpinUse && _freeSpinsRemaining == 0)
                FreeSpinsEnded?.Invoke();

            // Handle bonus trigger
            if (bonusCount >= 3)
                BonusTriggered?.Invoke();

            return result;
        }

        /// <summary>
        /// Cashes out all credits and returns the amount.
        /// </summary>
        public long Cashout()
        {
            long amount = _credits;
            _credits = 0;
            _meters.IncrementCoinOut(amount);
            CreditsChanged?.Invoke(_credits);
            return amount;
        }

        /// <summary>
        /// Forces specific stop positions (for testing/debugging/replay).
        /// </summary>
        public void ForceStops(int[] positions)
        {
            for (int i = 0; i < Math.Min(positions.Length, 5); i++)
                _reels[i].ForceStop(positions[i]);
        }

        /// <summary>
        /// Builds the current visible grid from reel stop positions.
        /// </summary>
        public SymbolType[,] BuildGrid()
        {
            var grid = new SymbolType[5, 3];
            for (int reel = 0; reel < 5; reel++)
                for (int row = 0; row < 3; row++)
                    grid[reel, row] = _reels[reel].GetSymbolAtRow(row);
            return grid;
        }

        /// <summary>
        /// Gets the current stop positions of all reels.
        /// </summary>
        public int[] GetStopPositions()
        {
            var positions = new int[5];
            for (int i = 0; i < 5; i++)
                positions[i] = _reels[i].StopPosition;
            return positions;
        }
    }
}
