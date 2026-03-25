using System.Collections.Generic;

namespace CasinoSlot.Input
{
    /// <summary>
    /// Contract for any input source that can generate <see cref="SlotAction"/> events.
    /// Implement this interface to add support for a new device type
    /// (keyboard, joystick, gamepad, button panel, touchscreen, etc.).
    /// </summary>
    public interface IInputProvider
    {
        /// <summary>
        /// Gets a human-readable name for this input provider (e.g. "Keyboard", "Joystick").
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets whether this input provider is currently connected / available.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Polls the device and returns the list of actions that have been activated
        /// since the last call to <see cref="Poll"/>.
        /// Returns an empty list when no actions are active.
        /// </summary>
        IReadOnlyList<SlotAction> Poll();

        /// <summary>
        /// Returns the complete mapping of device input identifiers to <see cref="SlotAction"/> values.
        /// The meaning of the key string depends on the provider type:
        /// - Keyboard: a <see cref="System.ConsoleKey"/> name (e.g. "Spacebar", "Enter")
        /// - Joystick: a button index as a string (e.g. "0", "1")
        /// </summary>
        IReadOnlyDictionary<string, SlotAction> GetBindings();

        /// <summary>
        /// Updates (or adds) a binding. Returns false if the binding key is invalid for this provider.
        /// </summary>
        bool SetBinding(string inputKey, SlotAction action);

        /// <summary>
        /// Removes a binding. Returns true if the binding existed and was removed.
        /// </summary>
        bool RemoveBinding(string inputKey);
    }
}
