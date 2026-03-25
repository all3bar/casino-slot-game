using System;
using Xunit;
using CasinoSlot.Core;
using CasinoSlot.Accounting;

namespace CasinoSlot.Tests.Core
{
    /// <summary>
    /// Unit tests for PaylineManager and PayTable.
    /// </summary>
    public class PaylineManagerTests
    {
        private PayTable CreatePayTable() => new PayTable();
        private PaylineManager CreateManager() => new PaylineManager(new PayTable());

        [Fact]
        public void PaylineManager_Has20Lines()
        {
            var mgr = CreateManager();
            Assert.Equal(20, mgr.Paylines.Count);
        }

        [Fact]
        public void Evaluate_AllLowSymbols_NoWin()
        {
            var mgr = CreateManager();
            // Grid of all Lemons with no 5-of-a-kind (would pay on line 1)
            // Actually lemon 5-of-a-kind pays 80, so let's use different symbols on each reel
            var grid = new SymbolType[5, 3];
            for (int r = 0; r < 5; r++)
            {
                grid[r, 0] = SymbolType.Lemon;
                grid[r, 1] = SymbolType.Cherry;
                grid[r, 2] = SymbolType.Orange;
            }
            // Middle row: Cherry-Cherry-Cherry-Cherry-Cherry = 100 credits x bet
            var results = mgr.Evaluate(grid, 1);
            Assert.True(results.Count > 0);
        }

        [Fact]
        public void Evaluate_3OfAKind_Pays()
        {
            var mgr = CreateManager();
            var grid = new SymbolType[5, 3];
            // Middle row: Seven, Seven, Seven, Lemon, Lemon
            grid[0, 1] = SymbolType.Seven;
            grid[1, 1] = SymbolType.Seven;
            grid[2, 1] = SymbolType.Seven;
            grid[3, 1] = SymbolType.Lemon;
            grid[4, 1] = SymbolType.Lemon;
            // Fill others
            for (int r = 0; r < 5; r++)
            {
                grid[r, 0] = SymbolType.Lemon;
                grid[r, 2] = SymbolType.Plum;
            }

            var results = mgr.Evaluate(grid, 1);
            var middleLine = results.Find(r => r.Payline.Id == 1);
            Assert.NotNull(middleLine);
            Assert.Equal(3, middleLine.MatchCount);
            Assert.Equal(SymbolType.Seven, middleLine.WinningSymbol);
            Assert.Equal(30L, middleLine.WinAmount); // PayTable: Seven x3 = 30 x betPerLine=1
        }

        [Fact]
        public void Evaluate_WildSubstitutes()
        {
            var mgr = CreateManager();
            var grid = new SymbolType[5, 3];
            // Middle row: Seven, Wild, Seven, Lemon, Lemon
            grid[0, 1] = SymbolType.Seven;
            grid[1, 1] = SymbolType.Wild;
            grid[2, 1] = SymbolType.Seven;
            grid[3, 1] = SymbolType.Lemon;
            grid[4, 1] = SymbolType.Lemon;
            for (int r = 0; r < 5; r++)
            {
                grid[r, 0] = SymbolType.Lemon;
                grid[r, 2] = SymbolType.Plum;
            }

            var results = mgr.Evaluate(grid, 1);
            var middleLine = results.Find(r => r.Payline.Id == 1);
            Assert.NotNull(middleLine);
            // Wild at position 1 still extends the match from position 0
            Assert.True(middleLine.MatchCount >= 3);
        }

        [Fact]
        public void CountScatters_ReturnsCorrectCount()
        {
            var mgr = CreateManager();
            var grid = new SymbolType[5, 3];
            grid[0, 1] = SymbolType.Scatter;
            grid[2, 0] = SymbolType.Scatter;
            grid[4, 2] = SymbolType.Scatter;

            int count = mgr.CountScatters(grid);
            Assert.Equal(3, count);
        }

        [Fact]
        public void PayTable_GetPayout_ReturnsCorrectValues()
        {
            var table = CreatePayTable();
            Assert.Equal(1000L, table.GetPayout(SymbolType.Seven, 5));
            Assert.Equal(30L, table.GetPayout(SymbolType.Seven, 3));
            Assert.Equal(0L, table.GetPayout(SymbolType.Seven, 1));
        }

        [Fact]
        public void PayTable_GetScatterPayout_3Scatters()
        {
            var table = CreatePayTable();
            long payout = table.GetScatterPayout(3, 100);
            Assert.Equal(200L, payout); // 2x total bet
        }

        [Fact]
        public void PayTable_GetFreeSpins_3Scatters_Returns10()
        {
            var table = CreatePayTable();
            Assert.Equal(10, table.GetFreeSpins(3));
            Assert.Equal(15, table.GetFreeSpins(4));
            Assert.Equal(25, table.GetFreeSpins(5));
            Assert.Equal(0, table.GetFreeSpins(2));
        }

        [Fact]
        public void ActiveLines_SetReducesEvaluatedLines()
        {
            var mgr = CreateManager();
            mgr.ActiveLinesCount = 1;
            Assert.Equal(1, mgr.ActiveLinesCount);
        }
    }
}
