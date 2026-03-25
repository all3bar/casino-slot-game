using System.Collections.Generic;
using CasinoSlot.Core;

namespace CasinoSlot.UI
{
    /// <summary>
    /// Holds data for a single paytable entry (symbol + payouts for 2-5 of a kind).
    /// </summary>
    public class PaytableEntry
    {
        public string SymbolName { get; }
        public string SpriteName { get; }
        public long Pay2 { get; }
        public long Pay3 { get; }
        public long Pay4 { get; }
        public long Pay5 { get; }

        public PaytableEntry(string name, string sprite, long pay2, long pay3, long pay4, long pay5)
        {
            SymbolName = name;
            SpriteName = sprite;
            Pay2 = pay2;
            Pay3 = pay3;
            Pay4 = pay4;
            Pay5 = pay5;
        }
    }

    /// <summary>
    /// Manages the paytable display screen.
    /// Builds the paytable from the game configuration for display.
    /// </summary>
    public class PaytableUI
    {
        private readonly List<PaytableEntry> _entries;
        private readonly PayTable _payTable;

        /// <summary>Gets all paytable entries for display.</summary>
        public IReadOnlyList<PaytableEntry> Entries => _entries;

        /// <summary>Gets the scatter pay description.</summary>
        public string ScatterDescription =>
            "3 SCATTER = 2x total bet + 10 FREE SPINS\n" +
            "4 SCATTER = 10x total bet + 15 FREE SPINS\n" +
            "5 SCATTER = 50x total bet + 25 FREE SPINS";

        /// <summary>Gets the bonus game description.</summary>
        public string BonusDescription =>
            "3+ BONUS symbols anywhere = BONUS GAME!\n" +
            "Pick items to reveal credit awards.";

        /// <summary>Gets the wild symbol description.</summary>
        public string WildDescription =>
            "WILD substitutes for all symbols except SCATTER and BONUS.\n" +
            "WILD pays the highest value when completing a line.";

        public PaytableUI()
        {
            _payTable = new PayTable();
            _entries = BuildEntries();
        }

        private List<PaytableEntry> BuildEntries()
        {
            return new List<PaytableEntry>
            {
                new PaytableEntry("WILD",   "sym_wild",    10,   50,   200,  2000),
                new PaytableEntry("7",      "sym_seven",    5,   30,   150,  1000),
                new PaytableEntry("BELL",   "sym_bell",     4,   20,   100,   500),
                new PaytableEntry("BAR3",   "sym_bar3",     3,   15,    75,   300),
                new PaytableEntry("BAR2",   "sym_bar2",     2,   10,    50,   200),
                new PaytableEntry("BAR1",   "sym_bar1",     1,    8,    40,   150),
                new PaytableEntry("CHERRY", "sym_cherry",   4,   10,    40,   100),
                new PaytableEntry("LEMON",  "sym_lemon",    1,    5,    20,    80),
                new PaytableEntry("ORANGE", "sym_orange",   1,    5,    20,    80),
                new PaytableEntry("PLUM",   "sym_plum",     1,    5,    20,    80),
                new PaytableEntry("SCATTER","sym_scatter",  2,   10,    50,   200),
            };
        }
    }
}
