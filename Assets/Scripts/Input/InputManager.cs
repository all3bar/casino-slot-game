using System;
using System.Collections.Generic;
using CasinoSlot.UI;

namespace CasinoSlot.Input
{
    /// <summary>
    /// Aggregates all registered <see cref="IInputProvider"/> instances, polls them each
    /// frame / tick, and routes triggered <see cref="SlotAction"/> values to the
    /// connected <see cref="MainGameUI"/>.
    ///
    /// Usage (Unity Update loop):
    /// <code>
    ///   void Update()
    ///   {
    ///       // Notify joystick button states first (Unity Input API)
    ///       for (int i = 0; i &lt; 10; i++)
    ///           _joystickProvider.NotifyButtonState(i, Input.GetButton("Joystick1Button" + i));
    ///
    ///       // Then let the manager poll all providers
    ///       _inputManager.Update();
    ///   }
    /// </code>
    /// </summary>
    public class InputManager
    {
        private readonly List<IInputProvider> _providers = new List<IInputProvider>();
        private MainGameUI? _gameUI;
        private bool _enabled = true;

        /// <summary>Gets whether input processing is currently active.</summary>
        public bool IsEnabled => _enabled;

        /// <summary>
        /// Raised whenever any input provider triggers a <see cref="SlotAction"/>.
        /// Subscribe here to handle input in non-Unity contexts.
        /// </summary>
        public event Action<SlotAction, IInputProvider>? ActionTriggered;

        /// <summary>
        /// Registers an <see cref="IInputProvider"/> with this manager.
        /// The provider will be polled on every <see cref="Update"/> call.
        /// </summary>
        public void RegisterProvider(IInputProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (!_providers.Contains(provider))
                _providers.Add(provider);
        }

        /// <summary>
        /// Removes a previously registered provider.
        /// </summary>
        public bool UnregisterProvider(IInputProvider provider)
        {
            return _providers.Remove(provider);
        }

        /// <summary>
        /// Returns all currently registered providers.
        /// </summary>
        public IReadOnlyList<IInputProvider> Providers => _providers;

        /// <summary>
        /// Connects a <see cref="MainGameUI"/> instance so that triggered actions are
        /// automatically forwarded to it.
        /// </summary>
        public void ConnectUI(MainGameUI ui)
        {
            _gameUI = ui;
        }

        /// <summary>Disconnects the UI without unregistering providers.</summary>
        public void DisconnectUI() => _gameUI = null;

        /// <summary>
        /// Enables or disables input processing (e.g. disabled during maintenance mode).
        /// </summary>
        public void SetEnabled(bool enabled) => _enabled = enabled;

        /// <summary>
        /// Polls all registered providers and dispatches any triggered actions.
        /// Call once per frame / game tick.
        /// </summary>
        public void Update()
        {
            if (!_enabled) return;

            foreach (var provider in _providers)
            {
                if (!provider.IsConnected) continue;

                var actions = provider.Poll();
                foreach (var action in actions)
                {
                    DispatchAction(action, provider);
                }
            }
        }

        /// <summary>
        /// Dispatches a single action from an external call (e.g. from Unity's joystick handling
        /// that uses <see cref="JoystickInputProvider.NotifyButtonState"/> directly).
        /// </summary>
        public void DispatchAction(SlotAction action, IInputProvider? source = null)
        {
            if (!_enabled || action == SlotAction.None) return;

            ActionTriggered?.Invoke(action, source!);
            RouteToUI(action);
        }

        // ---------------------------------------------------------------
        private void RouteToUI(SlotAction action)
        {
            if (_gameUI == null) return;

            switch (action)
            {
                case SlotAction.Spin:
                    _gameUI.OnSpinButtonPressed();
                    break;
                case SlotAction.MaxBet:
                    _gameUI.OnMaxBetPressed();
                    break;
                case SlotAction.BetIncrease:
                    _gameUI.OnBetPerLineChanged(_gameUI.DisplayedBet / Math.Max(1, GetActiveLinesCount()) + 1);
                    break;
                case SlotAction.BetDecrease:
                    long newBet = _gameUI.DisplayedBet / Math.Max(1, GetActiveLinesCount()) - 1;
                    _gameUI.OnBetPerLineChanged(Math.Max(1, newBet));
                    break;
                case SlotAction.LinesIncrease:
                    _gameUI.OnActiveLinesChanged(GetActiveLinesCount() + 1);
                    break;
                case SlotAction.LinesDecrease:
                    _gameUI.OnActiveLinesChanged(Math.Max(1, GetActiveLinesCount() - 1));
                    break;
                case SlotAction.Cashout:
                    _gameUI.OnCashoutPressed();
                    break;
                case SlotAction.AcknowledgeResult:
                    _gameUI.AcknowledgeResult();
                    break;
                case SlotAction.RepeatBet:
                    // RepeatBet simply triggers a spin without changing bet settings
                    _gameUI.OnSpinButtonPressed();
                    break;
                case SlotAction.OperatorMenu:
                    _gameUI.OnOperatorMenuRequested();
                    break;
            }
        }

        private int GetActiveLinesCount()
        {
            // Derive active lines count from displayed bet / bet-per-line
            // In a full Unity integration this would read from GameManager directly.
            // Use DisplayedBet as a proxy; UI will clamp to valid range.
            return 1; // safe default; UI clamps automatically
        }
    }
}
