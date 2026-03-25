namespace CasinoSlot.Core
{
    /// <summary>
    /// Defines symbol types available on the slot machine reels.
    /// </summary>
    public enum SymbolType
    {
        Wild = 0,
        Scatter = 1,
        Seven = 2,
        Bell = 3,
        BAR3 = 4,
        BAR2 = 5,
        BAR1 = 6,
        Cherry = 7,
        Lemon = 8,
        Orange = 9,
        Plum = 10,
        Bonus = 11
    }

    /// <summary>
    /// Represents a single symbol on the slot machine reel strip.
    /// </summary>
    public class Symbol
    {
        /// <summary>Gets the type of this symbol.</summary>
        public SymbolType Type { get; }

        /// <summary>Gets the display name of this symbol.</summary>
        public string Name { get; }

        /// <summary>Gets the sprite/asset name for this symbol.</summary>
        public string SpriteName { get; }

        /// <summary>Gets whether this symbol acts as a Wild (substitutes for others).</summary>
        public bool IsWild => Type == SymbolType.Wild;

        /// <summary>Gets whether this symbol is a Scatter (pays anywhere).</summary>
        public bool IsScatter => Type == SymbolType.Scatter;

        /// <summary>Gets whether this symbol triggers the Bonus game.</summary>
        public bool IsBonus => Type == SymbolType.Bonus;

        /// <summary>
        /// Initializes a new Symbol.
        /// </summary>
        /// <param name="type">The symbol type.</param>
        /// <param name="name">Display name.</param>
        /// <param name="spriteName">Asset name for the sprite.</param>
        public Symbol(SymbolType type, string name, string spriteName)
        {
            Type = type;
            Name = name;
            SpriteName = spriteName;
        }

        public override string ToString() => Name;
    }

    /// <summary>
    /// Factory for creating Symbol instances. Implements the Factory pattern.
    /// </summary>
    public static class SymbolFactory
    {
        /// <summary>
        /// Creates a Symbol from its SymbolType.
        /// </summary>
        public static Symbol Create(SymbolType type)
        {
            switch (type)
            {
                case SymbolType.Wild:    return new Symbol(type, "WILD",   "sym_wild");
                case SymbolType.Scatter: return new Symbol(type, "SCATTER","sym_scatter");
                case SymbolType.Seven:   return new Symbol(type, "7",      "sym_seven");
                case SymbolType.Bell:    return new Symbol(type, "BELL",   "sym_bell");
                case SymbolType.BAR3:    return new Symbol(type, "BAR3",   "sym_bar3");
                case SymbolType.BAR2:    return new Symbol(type, "BAR2",   "sym_bar2");
                case SymbolType.BAR1:    return new Symbol(type, "BAR1",   "sym_bar1");
                case SymbolType.Cherry:  return new Symbol(type, "CHERRY", "sym_cherry");
                case SymbolType.Lemon:   return new Symbol(type, "LEMON",  "sym_lemon");
                case SymbolType.Orange:  return new Symbol(type, "ORANGE", "sym_orange");
                case SymbolType.Plum:    return new Symbol(type, "PLUM",   "sym_plum");
                case SymbolType.Bonus:   return new Symbol(type, "BONUS",  "sym_bonus");
                default:                 return new Symbol(type, "UNKNOWN","sym_unknown");
            }
        }
    }
}
