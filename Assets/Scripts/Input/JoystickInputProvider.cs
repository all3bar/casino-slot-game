using System;
using System.Collections.Generic;

namespace CasinoSlot.Input
{
    /// <summary>
    /// Joystick / gamepad / USB button-panel input provider.
    ///
    /// In casino environments a "button panel" (botonera) typically enumerates as
    /// a HID joystick or HID gamepad over USB.  Each physical button corresponds to
    /// a numeric button index (0-based, as reported by the OS / Unity's Input system).
    ///
    /// Default button mapping (all configurable via <see cref="SetBinding"/>):
    /// <list type="bullet">
    ///   <item><description>Button 0 (SPIN / main button)     → Spin</description></item>
    ///   <item><description>Button 1 (MAX BET)                → MaxBet</description></item>
    ///   <item><description>Button 2 (BET +)                  → BetIncrease</description></item>
    ///   <item><description>Button 3 (BET -)                  → BetDecrease</description></item>
    ///   <item><description>Button 4 (LINES +)                → LinesIncrease</description></item>
    ///   <item><description>Button 5 (LINES -)                → LinesDecrease</description></item>
    ///   <item><description>Button 6 (CASHOUT / COLLECT)      → Cashout</description></item>
    ///   <item><description>Button 7 (COLLECT / ACKNOWLEDGE)  → AcknowledgeResult</description></item>
    ///   <item><description>Button 8 (REPEAT BET)             → RepeatBet</description></item>
    ///   <item><description>Button 9 (OPERATOR / SERVICE)     → OperatorMenu</description></item>
    /// </list>
    ///
    /// At runtime the host application (Unity) should call <see cref="NotifyButtonState"/>
    /// once per frame for each button that has changed state.
    /// </summary>
    public class JoystickInputProvider : IInputProvider
    {
        private readonly Dictionary<int, SlotAction> _bindings;

        // Button-down state: tracks which buttons are currently held
        private readonly HashSet<int> _heldButtons = new HashSet<int>();

        private bool _connected;
        private string _deviceName;

        /// <inheritdoc/>
        public string Name => $"Joystick ({_deviceName})";

        /// <inheritdoc/>
        public bool IsConnected => _connected;

        /// <summary>
        /// Creates a <see cref="JoystickInputProvider"/> with default button bindings.
        /// </summary>
        /// <param name="deviceName">Human-readable device name (e.g. "USB Button Panel" or "Joystick 1").</param>
        /// <param name="connected">Whether the device is currently connected.</param>
        public JoystickInputProvider(string deviceName = "USB Button Panel", bool connected = true)
        {
            _deviceName = deviceName;
            _connected = connected;
            _bindings = BuildDefaultBindings();
        }

        /// <summary>
        /// Updates the connection state of the joystick (called by Unity when device connects/disconnects).
        /// </summary>
        public void SetConnected(bool connected, string deviceName = "")
        {
            _connected = connected;
            if (!string.IsNullOrEmpty(deviceName))
                _deviceName = deviceName;
            if (!connected)
                _heldButtons.Clear();
        }

        /// <summary>
        /// Notifies the provider that a button's pressed state has changed.
        /// Call this from Unity's Update() for each joystick button:
        /// <code>
        ///   provider.NotifyButtonState(buttonIndex, Input.GetButton("Joystick1Button" + buttonIndex));
        /// </code>
        /// Returns the <see cref="SlotAction"/> triggered on press, or <see cref="SlotAction.None"/>.
        /// </summary>
        /// <param name="buttonIndex">Zero-based button index.</param>
        /// <param name="isPressed">True if the button is pressed this frame.</param>
        public SlotAction NotifyButtonState(int buttonIndex, bool isPressed)
        {
            if (!_connected) return SlotAction.None;

            if (isPressed && !_heldButtons.Contains(buttonIndex))
            {
                _heldButtons.Add(buttonIndex);
                if (_bindings.TryGetValue(buttonIndex, out SlotAction action))
                    return action;
            }
            else if (!isPressed)
            {
                _heldButtons.Remove(buttonIndex);
            }

            return SlotAction.None;
        }

        /// <summary>
        /// <see cref="IInputProvider.Poll"/> is used by non-Unity environments.
        /// In Unity, call <see cref="NotifyButtonState"/> per button per frame instead.
        /// Returns any actions from the internal pending queue.
        /// </summary>
        public IReadOnlyList<SlotAction> Poll()
        {
            // In a pure-C# / test context this is a no-op; Unity uses NotifyButtonState.
            return Array.Empty<SlotAction>();
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, SlotAction> GetBindings()
        {
            var result = new Dictionary<string, SlotAction>();
            foreach (var kv in _bindings)
                result[kv.Key.ToString()] = kv.Value;
            return result;
        }

        /// <summary>
        /// Sets the action for a given button index.
        /// The <paramref name="inputKey"/> must be a valid non-negative integer string
        /// representing the button index (e.g. "0", "1", "2").
        /// </summary>
        public bool SetBinding(string inputKey, SlotAction action)
        {
            if (int.TryParse(inputKey, out int btn) && btn >= 0)
            {
                _bindings[btn] = action;
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public bool RemoveBinding(string inputKey)
        {
            if (int.TryParse(inputKey, out int btn))
                return _bindings.Remove(btn);
            return false;
        }

        /// <summary>
        /// Clears all held-button state (e.g. when focus is lost or device disconnects).
        /// </summary>
        public void ClearHeldButtons() => _heldButtons.Clear();

        /// <summary>
        /// Returns true if the given button is currently held.
        /// </summary>
        public bool IsButtonHeld(int buttonIndex) => _heldButtons.Contains(buttonIndex);

        // ---------------------------------------------------------------
        private static Dictionary<int, SlotAction> BuildDefaultBindings()
        {
            return new Dictionary<int, SlotAction>
            {
                { 0, SlotAction.Spin              },  // SPIN / main button
                { 1, SlotAction.MaxBet            },  // MAX BET
                { 2, SlotAction.BetIncrease       },  // BET +
                { 3, SlotAction.BetDecrease       },  // BET -
                { 4, SlotAction.LinesIncrease     },  // LINES +
                { 5, SlotAction.LinesDecrease     },  // LINES -
                { 6, SlotAction.Cashout           },  // CASHOUT / COLLECT
                { 7, SlotAction.AcknowledgeResult },  // COLLECT / ACKNOWLEDGE
                { 8, SlotAction.RepeatBet         },  // REPEAT BET
                { 9, SlotAction.OperatorMenu      },  // OPERATOR / SERVICE
            };
        }
    }
}
