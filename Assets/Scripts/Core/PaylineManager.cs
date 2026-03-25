using System.Collections.Generic;

namespace CasinoSlot.Core
{
    /// <summary>
    /// Defines a single payline pattern across the 5x3 reel grid.
    /// Each entry in Rows is the row index (0=top, 1=mid, 2=bot) for that reel column.
    /// </summary>
    public class Payline
    {
        /// <summary>Gets the payline identifier (1-based).</summary>
        public int Id { get; }

        /// <summary>Gets the row indices per reel column defining the payline path.</summary>
        public int[] Rows { get; }

        /// <summary>
        /// Initializes a Payline with an identifier and row pattern.
        /// </summary>
        /// <param name="id">Payline ID (1-25).</param>
        /// <param name="rows">5-element array of row indices for each reel.</param>
        public Payline(int id, int[] rows)
        {
            Id = id;
            Rows = rows;
        }
    }

    /// <summary>
    /// Manages all paylines and evaluates winning combinations.
    /// </summary>
    public class PaylineManager
    {
        private readonly List<Payline> _paylines;
        private readonly PayTable _payTable;
        private int _activeLinesCount;

        /// <summary>Gets all defined paylines.</summary>
        public IReadOnlyList<Payline> Paylines => _paylines;

        /// <summary>Gets or sets the number of active paylines.</summary>
        public int ActiveLinesCount
        {
            get => _activeLinesCount;
            set => _activeLinesCount = value > 0 && value <= _paylines.Count ? value : _activeLinesCount;
        }

        /// <summary>
        /// Initializes the PaylineManager with the default 20 paylines.
        /// </summary>
        /// <param name="payTable">The pay table defining symbol win values.</param>
        public PaylineManager(PayTable payTable)
        {
            _payTable = payTable;
            _paylines = BuildDefaultPaylines();
            _activeLinesCount = _paylines.Count;
        }

        /// <summary>
        /// Evaluates a spin result and returns all winning payline results.
        /// </summary>
        /// <param name="grid">5x3 symbol grid [reel][row].</param>
        /// <param name="betPerLine">Bet amount per active line.</param>
        /// <returns>List of winning payline evaluations.</returns>
        public List<PaylineResult> Evaluate(SymbolType[,] grid, long betPerLine)
        {
            var results = new List<PaylineResult>();

            for (int i = 0; i < _activeLinesCount; i++)
            {
                var line = _paylines[i];
                var lineResult = EvaluateLine(grid, line, betPerLine);
                if (lineResult != null)
                    results.Add(lineResult);
            }

            return results;
        }

        /// <summary>
        /// Counts Scatter symbols anywhere on the visible grid.
        /// </summary>
        public int CountScatters(SymbolType[,] grid)
        {
            int count = 0;
            for (int reel = 0; reel < 5; reel++)
                for (int row = 0; row < 3; row++)
                    if (grid[reel, row] == SymbolType.Scatter)
                        count++;
            return count;
        }

        /// <summary>
        /// Counts Bonus symbols anywhere on the visible grid.
        /// </summary>
        public int CountBonus(SymbolType[,] grid)
        {
            int count = 0;
            for (int reel = 0; reel < 5; reel++)
                for (int row = 0; row < 3; row++)
                    if (grid[reel, row] == SymbolType.Bonus)
                        count++;
            return count;
        }

        private PaylineResult EvaluateLine(SymbolType[,] grid, Payline line, long betPerLine)
        {
            var symbols = new SymbolType[5];
            for (int reel = 0; reel < 5; reel++)
                symbols[reel] = grid[reel, line.Rows[reel]];

            // Count consecutive matching symbols from left, allowing Wilds
            SymbolType baseSymbol = SymbolType.Wild;
            int matchCount = 0;

            for (int reel = 0; reel < 5; reel++)
            {
                var sym = symbols[reel];
                if (sym == SymbolType.Wild)
                {
                    matchCount++;
                    continue;
                }
                if (baseSymbol == SymbolType.Wild)
                {
                    // First non-wild symbol sets the base
                    baseSymbol = sym;
                    matchCount++;
                }
                else if (sym == baseSymbol)
                {
                    matchCount++;
                }
                else
                {
                    break;
                }
            }

            // Wild-only line: use Wild payouts
            if (baseSymbol == SymbolType.Wild)
                baseSymbol = SymbolType.Wild;

            long payout = _payTable.GetPayout(baseSymbol, matchCount);
            if (payout <= 0) return null;

            long winAmount = payout * betPerLine;
            return new PaylineResult(line, matchCount, baseSymbol, winAmount);
        }

        private static List<Payline> BuildDefaultPaylines()
        {
            return new List<Payline>
            {
                // Standard lines
                new Payline(1,  new[] { 1, 1, 1, 1, 1 }), // Middle row
                new Payline(2,  new[] { 0, 0, 0, 0, 0 }), // Top row
                new Payline(3,  new[] { 2, 2, 2, 2, 2 }), // Bottom row
                new Payline(4,  new[] { 0, 1, 2, 1, 0 }), // V shape
                new Payline(5,  new[] { 2, 1, 0, 1, 2 }), // Inverted V
                // Zigzag lines
                new Payline(6,  new[] { 0, 0, 1, 2, 2 }),
                new Payline(7,  new[] { 2, 2, 1, 0, 0 }),
                new Payline(8,  new[] { 1, 0, 0, 0, 1 }),
                new Payline(9,  new[] { 1, 2, 2, 2, 1 }),
                new Payline(10, new[] { 0, 1, 0, 1, 0 }),
                new Payline(11, new[] { 2, 1, 2, 1, 2 }),
                new Payline(12, new[] { 1, 0, 1, 0, 1 }),
                new Payline(13, new[] { 1, 2, 1, 2, 1 }),
                new Payline(14, new[] { 0, 0, 1, 0, 0 }),
                new Payline(15, new[] { 2, 2, 1, 2, 2 }),
                new Payline(16, new[] { 0, 1, 1, 1, 0 }),
                new Payline(17, new[] { 2, 1, 1, 1, 2 }),
                new Payline(18, new[] { 1, 1, 0, 1, 1 }),
                new Payline(19, new[] { 1, 1, 2, 1, 1 }),
                new Payline(20, new[] { 0, 2, 0, 2, 0 }),
            };
        }
    }

    /// <summary>
    /// Holds the result of evaluating a single payline.
    /// </summary>
    public class PaylineResult
    {
        /// <summary>Gets the winning payline.</summary>
        public Payline Payline { get; }

        /// <summary>Gets the number of matching symbols.</summary>
        public int MatchCount { get; }

        /// <summary>Gets the winning symbol type.</summary>
        public SymbolType WinningSymbol { get; }

        /// <summary>Gets the win amount in credits.</summary>
        public long WinAmount { get; }

        public PaylineResult(Payline payline, int matchCount, SymbolType symbol, long winAmount)
        {
            Payline = payline;
            MatchCount = matchCount;
            WinningSymbol = symbol;
            WinAmount = winAmount;
        }
    }

    /// <summary>
    /// Defines payout multipliers for each symbol and match count combination.
    /// </summary>
    public class PayTable
    {
        // [SymbolType][matchCount] = multiplier
        private readonly Dictionary<SymbolType, int[]> _table;

        public PayTable()
        {
            // Multipliers: index 0=unused, 1=unused, 2=2-of-a-kind, 3=3-of-a-kind, 4=4-of-a-kind, 5=5-of-a-kind
            _table = new Dictionary<SymbolType, int[]>
            {
                { SymbolType.Wild,    new[] { 0, 0, 10,  50,  200, 2000 } },
                { SymbolType.Seven,   new[] { 0, 0, 5,   30,  150, 1000 } },
                { SymbolType.Bell,    new[] { 0, 0, 4,   20,  100,  500 } },
                { SymbolType.BAR3,    new[] { 0, 0, 3,   15,   75,  300 } },
                { SymbolType.BAR2,    new[] { 0, 0, 2,   10,   50,  200 } },
                { SymbolType.BAR1,    new[] { 0, 0, 1,    8,   40,  150 } },
                { SymbolType.Cherry,  new[] { 0, 2, 4,   10,   40,  100 } },
                { SymbolType.Lemon,   new[] { 0, 0, 1,    5,   20,   80 } },
                { SymbolType.Orange,  new[] { 0, 0, 1,    5,   20,   80 } },
                { SymbolType.Plum,    new[] { 0, 0, 1,    5,   20,   80 } },
                { SymbolType.Scatter, new[] { 0, 0, 2,   10,   50,  200 } },
                { SymbolType.Bonus,   new[] { 0, 0, 0,    0,    0,    0 } }, // Bonus triggered by 3+ anywhere
            };
        }

        /// <summary>
        /// Gets the payout multiplier for a given symbol and match count.
        /// </summary>
        public long GetPayout(SymbolType symbol, int matchCount)
        {
            if (matchCount < 2 || matchCount > 5) return 0;
            if (_table.TryGetValue(symbol, out var payouts))
                return payouts[matchCount];
            return 0;
        }

        /// <summary>
        /// Gets scatter payout based on scatter count (total credits * multiplier).
        /// </summary>
        public long GetScatterPayout(int scatterCount, long totalBet)
        {
            switch (scatterCount)
            {
                case 3: return totalBet * 2;
                case 4: return totalBet * 10;
                case 5: return totalBet * 50;
                default: return 0;
            }
        }

        /// <summary>Gets the free spins count awarded by scatter symbols.</summary>
        public int GetFreeSpins(int scatterCount)
        {
            switch (scatterCount)
            {
                case 3: return 10;
                case 4: return 15;
                case 5: return 25;
                default: return 0;
            }
        }
    }
}
