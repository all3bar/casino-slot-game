using System.Collections.Generic;

namespace CasinoSlot.Core
{
    /// <summary>
    /// Represents a single reel on the slot machine, including its strip definition and current stop position.
    /// </summary>
    public class Reel
    {
        private readonly List<SymbolType> _strip;
        private int _stopPosition;

        /// <summary>Gets the index of this reel (0-based).</summary>
        public int Index { get; }

        /// <summary>Gets the number of positions on this reel strip.</summary>
        public int StripLength => _strip.Count;

        /// <summary>Gets the current stop position of the reel.</summary>
        public int StopPosition => _stopPosition;

        /// <summary>
        /// Initializes a Reel with the given strip definition.
        /// </summary>
        /// <param name="index">Reel index (0-4 for a 5-reel machine).</param>
        /// <param name="strip">List of symbols on this reel strip.</param>
        public Reel(int index, List<SymbolType> strip)
        {
            Index = index;
            _strip = new List<SymbolType>(strip);
            _stopPosition = 0;
        }

        /// <summary>
        /// Spins the reel to a new random stop position.
        /// </summary>
        /// <param name="rng">The RNG to use for the stop position.</param>
        public void Spin(RNG rng)
        {
            _stopPosition = rng.NextReelStop(_strip.Count);
        }

        /// <summary>
        /// Forces the reel to a specific stop position (for testing/debugging).
        /// </summary>
        public void ForceStop(int position)
        {
            _stopPosition = position % _strip.Count;
        }

        /// <summary>
        /// Returns the symbol at a given row offset from the current stop position.
        /// Row 0 = top row, Row 1 = middle row, Row 2 = bottom row.
        /// </summary>
        /// <param name="row">Row offset (0, 1, or 2 for a 3-row display).</param>
        public SymbolType GetSymbolAtRow(int row)
        {
            int idx = (_stopPosition + row) % _strip.Count;
            return _strip[idx];
        }

        /// <summary>
        /// Returns the symbols visible in all rows (3 rows by default).
        /// </summary>
        /// <param name="rowCount">Number of visible rows.</param>
        public SymbolType[] GetVisibleSymbols(int rowCount = 3)
        {
            var result = new SymbolType[rowCount];
            for (int r = 0; r < rowCount; r++)
            {
                result[r] = GetSymbolAtRow(r);
            }
            return result;
        }

        /// <summary>
        /// Returns the default reel strip for a given reel index.
        /// These strips are calibrated for ~96% RTP.
        /// </summary>
        public static List<SymbolType> GetDefaultStrip(int reelIndex)
        {
            // Reel strips calibrated for target RTP
            switch (reelIndex)
            {
                case 0:
                    return new List<SymbolType>
                    {
                        SymbolType.Cherry, SymbolType.Lemon, SymbolType.Orange, SymbolType.Plum,
                        SymbolType.Cherry, SymbolType.BAR1,  SymbolType.Lemon,  SymbolType.Bell,
                        SymbolType.Orange, SymbolType.Cherry, SymbolType.Plum,  SymbolType.BAR2,
                        SymbolType.Lemon,  SymbolType.Cherry, SymbolType.BAR1,  SymbolType.Seven,
                        SymbolType.Orange, SymbolType.Plum,  SymbolType.Cherry, SymbolType.BAR3,
                        SymbolType.Bell,   SymbolType.Lemon,  SymbolType.Scatter, SymbolType.Orange,
                        SymbolType.BAR1,   SymbolType.Cherry, SymbolType.Plum,   SymbolType.Lemon,
                        SymbolType.Wild,   SymbolType.Bonus,  SymbolType.Cherry, SymbolType.BAR2
                    };
                case 1:
                    return new List<SymbolType>
                    {
                        SymbolType.Lemon,  SymbolType.Cherry, SymbolType.Plum,   SymbolType.Orange,
                        SymbolType.BAR1,   SymbolType.Cherry, SymbolType.Bell,   SymbolType.Lemon,
                        SymbolType.Cherry, SymbolType.BAR2,   SymbolType.Orange, SymbolType.Plum,
                        SymbolType.Cherry, SymbolType.BAR1,   SymbolType.Seven,  SymbolType.Lemon,
                        SymbolType.Plum,   SymbolType.Orange, SymbolType.Cherry, SymbolType.BAR3,
                        SymbolType.Lemon,  SymbolType.Bell,   SymbolType.Cherry, SymbolType.Scatter,
                        SymbolType.Orange, SymbolType.BAR1,   SymbolType.Cherry, SymbolType.Plum,
                        SymbolType.Bonus,  SymbolType.Wild,   SymbolType.Lemon,  SymbolType.BAR2
                    };
                case 2:
                    return new List<SymbolType>
                    {
                        SymbolType.Orange, SymbolType.Plum,   SymbolType.Lemon,  SymbolType.Cherry,
                        SymbolType.Bell,   SymbolType.BAR1,   SymbolType.Cherry, SymbolType.Orange,
                        SymbolType.Plum,   SymbolType.Cherry, SymbolType.BAR2,   SymbolType.Lemon,
                        SymbolType.Seven,  SymbolType.Cherry, SymbolType.BAR1,   SymbolType.Plum,
                        SymbolType.Cherry, SymbolType.Orange, SymbolType.BAR3,   SymbolType.Bell,
                        SymbolType.Scatter, SymbolType.Cherry, SymbolType.Lemon,  SymbolType.BAR1,
                        SymbolType.Cherry, SymbolType.Plum,   SymbolType.Orange, SymbolType.BAR2,
                        SymbolType.Wild,   SymbolType.Cherry, SymbolType.Bonus,  SymbolType.Lemon
                    };
                case 3:
                    return new List<SymbolType>
                    {
                        SymbolType.Plum,   SymbolType.Orange, SymbolType.Cherry, SymbolType.Lemon,
                        SymbolType.BAR1,   SymbolType.Bell,   SymbolType.Cherry, SymbolType.BAR2,
                        SymbolType.Cherry, SymbolType.Plum,   SymbolType.Lemon,  SymbolType.Orange,
                        SymbolType.BAR1,   SymbolType.Cherry, SymbolType.Seven,  SymbolType.Bell,
                        SymbolType.Orange, SymbolType.BAR3,   SymbolType.Cherry, SymbolType.Plum,
                        SymbolType.Cherry, SymbolType.Scatter, SymbolType.BAR1,  SymbolType.Lemon,
                        SymbolType.Plum,   SymbolType.Cherry, SymbolType.BAR2,   SymbolType.Orange,
                        SymbolType.Bonus,  SymbolType.Lemon,  SymbolType.Wild,   SymbolType.Cherry
                    };
                case 4:
                    return new List<SymbolType>
                    {
                        SymbolType.Bell,   SymbolType.Cherry, SymbolType.Lemon,  SymbolType.Plum,
                        SymbolType.Cherry, SymbolType.BAR1,   SymbolType.Orange, SymbolType.Cherry,
                        SymbolType.Lemon,  SymbolType.BAR2,   SymbolType.Cherry, SymbolType.Bell,
                        SymbolType.Cherry, SymbolType.Seven,  SymbolType.BAR1,   SymbolType.Plum,
                        SymbolType.BAR3,   SymbolType.Cherry, SymbolType.Orange, SymbolType.Lemon,
                        SymbolType.Cherry, SymbolType.BAR1,   SymbolType.Scatter, SymbolType.Cherry,
                        SymbolType.Orange, SymbolType.Plum,   SymbolType.Cherry, SymbolType.BAR2,
                        SymbolType.Lemon,  SymbolType.Wild,   SymbolType.Cherry, SymbolType.Bonus
                    };
                default:
                    return GetDefaultStrip(0);
            }
        }
    }
}
