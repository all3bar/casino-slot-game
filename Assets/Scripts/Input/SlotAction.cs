namespace CasinoSlot.Input
{
    /// <summary>
    /// Defines all player-facing actions that can be triggered by any input device
    /// (USB keyboard, joystick button panel, gamepad, touchscreen, etc.).
    /// </summary>
    public enum SlotAction
    {
        /// <summary>No action / unknown.</summary>
        None = 0,

        /// <summary>Start/execute a spin.</summary>
        Spin = 1,

        /// <summary>Set maximum bet and all lines.</summary>
        MaxBet = 2,

        /// <summary>Increase bet per line by one step.</summary>
        BetIncrease = 3,

        /// <summary>Decrease bet per line by one step.</summary>
        BetDecrease = 4,

        /// <summary>Increase the number of active paylines by one step.</summary>
        LinesIncrease = 5,

        /// <summary>Decrease the number of active paylines by one step.</summary>
        LinesDecrease = 6,

        /// <summary>Cash out current credits.</summary>
        Cashout = 7,

        /// <summary>Acknowledge / dismiss the current result screen.</summary>
        AcknowledgeResult = 8,

        /// <summary>Open/close the operator menu (requires PIN).</summary>
        OperatorMenu = 9,

        /// <summary>Repeat last bet without changing any settings.</summary>
        RepeatBet = 10,
    }
}
