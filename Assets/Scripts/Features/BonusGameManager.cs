using System;
using CasinoSlot.Core;

namespace CasinoSlot.Features
{
    /// <summary>
    /// Represents a single pick choice in the bonus game.
    /// </summary>
    public class BonusPick
    {
        /// <summary>Gets the display label for this pick.</summary>
        public string Label { get; }

        /// <summary>Gets the credit award for this pick.</summary>
        public long Credits { get; }

        /// <summary>Gets whether this pick reveals a multiplier.</summary>
        public bool IsMultiplier { get; }

        /// <summary>Gets the multiplier value (if IsMultiplier is true).</summary>
        public int MultiplierValue { get; }

        public BonusPick(string label, long credits, bool isMultiplier = false, int multiplierValue = 1)
        {
            Label = label;
            Credits = credits;
            IsMultiplier = isMultiplier;
            MultiplierValue = multiplierValue;
        }
    }

    /// <summary>
    /// Manages the bonus "pick-em" game triggered by 3+ bonus symbols.
    /// Players pick from hidden items to reveal credit awards.
    /// </summary>
    public class BonusGameManager
    {
        private readonly RNG _rng;
        private BonusPick[] _picks;
        private int _picksRemaining;
        private long _totalBonusWin;
        private int _multiplier;
        private bool _isActive;

        // Possible bonus awards (indexed)
        private static readonly long[] PossibleAwards = { 50, 75, 100, 150, 200, 300, 500, 750, 1000 };
        private const int NumPicks = 9;

        /// <summary>Gets whether the bonus game is currently active.</summary>
        public bool IsActive => _isActive;

        /// <summary>Gets picks remaining in the current bonus session.</summary>
        public int PicksRemaining => _picksRemaining;

        /// <summary>Gets the total win accumulated in the current bonus game.</summary>
        public long TotalWin => _totalBonusWin;

        /// <summary>Raised when the bonus game starts.</summary>
        public event Action<int> BonusStarted; // (numPicks)

        /// <summary>Raised when a pick is revealed.</summary>
        public event Action<int, BonusPick> PickRevealed; // (pickIndex, pick)

        /// <summary>Raised when the bonus game ends.</summary>
        public event Action<long> BonusCompleted; // (totalWin)

        /// <summary>
        /// Initializes the bonus game manager.
        /// </summary>
        public BonusGameManager(RNG rng)
        {
            _rng = rng;
        }

        /// <summary>
        /// Starts a new bonus game.
        /// </summary>
        /// <param name="baseBet">The base bet to scale bonus awards.</param>
        /// <param name="numPicksAllowed">How many picks the player may make.</param>
        public void StartBonus(long baseBet, int numPicksAllowed = 3)
        {
            if (_isActive) return;

            _totalBonusWin = 0;
            _multiplier = 1;
            _picksRemaining = numPicksAllowed;
            _picks = GeneratePicks(baseBet);
            _isActive = true;

            BonusStarted?.Invoke(NumPicks);
        }

        /// <summary>
        /// Player selects a pick by index (0-based).
        /// Returns the revealed pick, or null if invalid.
        /// </summary>
        public BonusPick SelectPick(int index)
        {
            if (!_isActive || _picksRemaining <= 0) return null;
            if (index < 0 || index >= _picks.Length) return null;

            var pick = _picks[index];
            _picks[index] = null; // Mark as selected

            if (pick.IsMultiplier)
                _multiplier *= pick.MultiplierValue;
            else
                _totalBonusWin += pick.Credits * _multiplier;

            _picksRemaining--;
            PickRevealed?.Invoke(index, pick);

            if (_picksRemaining <= 0)
                EndBonus();

            return pick;
        }

        /// <summary>
        /// Ends the bonus game (can be called early).
        /// </summary>
        public void EndBonus()
        {
            if (!_isActive) return;
            _isActive = false;
            BonusCompleted?.Invoke(_totalBonusWin);
        }

        private BonusPick[] GeneratePicks(long baseBet)
        {
            var picks = new BonusPick[NumPicks];
            for (int i = 0; i < NumPicks - 1; i++)
            {
                long award = PossibleAwards[_rng.Next(PossibleAwards.Length)] * baseBet / 10;
                picks[i] = new BonusPick($"${award}", award);
            }
            // One multiplier pick
            int multiplierPos = _rng.Next(NumPicks);
            int mult = _rng.Next(2, 6); // 2x-5x multiplier
            picks[multiplierPos] = new BonusPick($"{mult}X MULTIPLIER", 0, true, mult);
            return picks;
        }
    }
}
