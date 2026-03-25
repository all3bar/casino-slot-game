using System;
using System.Collections.Generic;
using CasinoSlot.Core;

namespace CasinoSlot.UI
{
    /// <summary>
    /// Controls animations and visual effects for the slot machine.
    /// Manages spin animation, win highlight, and transition effects.
    /// In a Unity project this would use Unity's animation system; here it
    /// tracks animation state for UI controllers to act on.
    /// </summary>
    public class AnimationController
    {
        /// <summary>Defines available animation states.</summary>
        public enum AnimState
        {
            Idle,
            SpinningAllReels,
            StoppingReels,
            ShowingWin,
            CelebrationWin,
            FreeSpinsIntro,
            BonusIntro,
            JackpotCelebration
        }

        private AnimState _currentState = AnimState.Idle;
        private int _reelsStillSpinning;
        private readonly List<int> _stoppedReels = new List<int>();

        /// <summary>Gets the current animation state.</summary>
        public AnimState CurrentState => _currentState;

        /// <summary>Gets the list of reels that have stopped in the current spin.</summary>
        public IReadOnlyList<int> StoppedReels => _stoppedReels;

        /// <summary>Raised when an animation phase starts.</summary>
        public event Action<AnimState> AnimationStarted;

        /// <summary>Raised when an animation phase completes.</summary>
        public event Action<AnimState> AnimationCompleted;

        /// <summary>Raised when a reel stops (reel index 0-4).</summary>
        public event Action<int> ReelStopped;

        /// <summary>
        /// Starts the spin animation for all 5 reels.
        /// </summary>
        public void StartSpin()
        {
            _stoppedReels.Clear();
            _reelsStillSpinning = 5;
            SetState(AnimState.SpinningAllReels);
        }

        /// <summary>
        /// Signals that a specific reel should stop (called in order, reel 0 first).
        /// </summary>
        public void StopReel(int reelIndex)
        {
            if (_currentState != AnimState.SpinningAllReels &&
                _currentState != AnimState.StoppingReels) return;

            if (_currentState == AnimState.SpinningAllReels)
                SetState(AnimState.StoppingReels);

            _stoppedReels.Add(reelIndex);
            _reelsStillSpinning = Math.Max(0, _reelsStillSpinning - 1);
            ReelStopped?.Invoke(reelIndex);

            if (_reelsStillSpinning == 0)
                AnimationCompleted?.Invoke(AnimState.StoppingReels);
        }

        /// <summary>
        /// Starts the win highlight animation for winning paylines.
        /// </summary>
        /// <param name="winLines">Winning payline results to highlight.</param>
        public void StartWinAnimation(List<PaylineResult> winLines)
        {
            if (winLines == null || winLines.Count == 0) return;
            SetState(AnimState.ShowingWin);
        }

        /// <summary>
        /// Starts the jackpot celebration animation.
        /// </summary>
        public void StartJackpotCelebration()
        {
            SetState(AnimState.JackpotCelebration);
        }

        /// <summary>
        /// Starts the free spins intro animation.
        /// </summary>
        public void StartFreeSpinsIntro(int count)
        {
            SetState(AnimState.FreeSpinsIntro);
        }

        /// <summary>
        /// Starts the bonus game intro animation.
        /// </summary>
        public void StartBonusIntro()
        {
            SetState(AnimState.BonusIntro);
        }

        /// <summary>
        /// Marks the current animation as complete, returning to Idle.
        /// </summary>
        public void CompleteAnimation()
        {
            var completedState = _currentState;
            SetState(AnimState.Idle);
            AnimationCompleted?.Invoke(completedState);
        }

        private void SetState(AnimState state)
        {
            _currentState = state;
            AnimationStarted?.Invoke(state);
        }
    }
}
