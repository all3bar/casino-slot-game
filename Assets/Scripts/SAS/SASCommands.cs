namespace CasinoSlot.SAS
{
    /// <summary>
    /// Defines SAS 6.0 command byte codes.
    /// Reference: SAS Protocol Specification Version 6.02
    /// </summary>
    public static class SASCommandCodes
    {
        // General polling / machine status
        public const byte Shutdown               = 0x01;
        public const byte Startup                = 0x11;
        public const byte SoundOff               = 0x12;
        public const byte SoundOn                = 0x13;
        public const byte EnableBillAcceptor     = 0x06;
        public const byte DisableBillAcceptor    = 0x07;
        public const byte EnableGamePlay         = 0x08;
        public const byte DisableGamePlay        = 0x09;

        // Game information
        public const byte SendEnabledGameNumbers = 0x1F;
        public const byte SendSelectedGameNumber = 0x52;
        public const byte SendGamingMachineId    = 0x53;

        // Meters
        public const byte SendCurrentCredits     = 0x2E;
        public const byte SendTotalCoinIn        = 0x54;
        public const byte SendTotalCoinOut       = 0x55;
        public const byte SendTotalDrop          = 0x56;
        public const byte SendTotalJackpot       = 0x57;
        public const byte SendGamesPlayed        = 0x58;
        public const byte SendGamesWon           = 0x59;
        public const byte SendGamesLost          = 0x5A;

        // Bill acceptor
        public const byte SendLastAcceptedBill   = 0x4C;
        public const byte SendBillDenomination   = 0x4F;

        // Security and authentication
        public const byte SendAuthenticationInfo = 0x6F;
        public const byte SendGameMachineMeters  = 0x7B;
        public const byte SendDenomination       = 0x7E;

        // AFT (Account Funds Transfer)
        public const byte AFTTransfer            = 0x72;
        public const byte AFTStatus              = 0x74;

        // TITO (Ticket In Ticket Out)
        public const byte SendTicketValidationData = 0x70;
        public const byte ValidateTicket         = 0x71;

        // Extended meters
        public const byte SendAllGameMeters      = 0x2F;
        public const byte SendDoorStatus         = 0x46;
    }

    /// <summary>
    /// Defines SAS 6.0 real-time exception codes (unsolicited events).
    /// </summary>
    public static class SASExceptionCodes
    {
        public const byte SlotDoorOpened         = 0x11;
        public const byte SlotDoorClosed         = 0x12;
        public const byte DroppoorOpened         = 0x17;
        public const byte DropDoorClosed         = 0x18;
        public const byte CardCageOpened         = 0x19;
        public const byte CardCageClosed         = 0x1A;
        public const byte ACPowerLoss            = 0x20;
        public const byte ACPowerRestored        = 0x21;
        public const byte LowBattery             = 0x22;
        public const byte CashoutButtonPressed   = 0x23;
        public const byte TicketCashoutButtonPressed = 0x25;
        public const byte GameStarted            = 0x51;
        public const byte GameEnded              = 0x52;
        public const byte GameRecalled           = 0x53;
        public const byte JackpotHandpayPending  = 0x54;
        public const byte HandpayCanceled        = 0x56;
        public const byte BillAccepted           = 0x67;
        public const byte BillRejected           = 0x68;
        public const byte BillJam                = 0x69;
        public const byte StackerFull            = 0x6E;
        public const byte ProgressiveWin         = 0x80;
        public const byte AFTTransferComplete    = 0x98;
        public const byte AFTTransferPartial     = 0x99;
        public const byte AFTTransferFailed      = 0x9A;
    }

    /// <summary>
    /// SAS polling address constants.
    /// </summary>
    public static class SASConstants
    {
        /// <summary>Broadcast address (no address byte in response expected).</summary>
        public const byte BroadcastAddress = 0x00;

        /// <summary>Global broadcast to all machines.</summary>
        public const byte GlobalBroadcast = 0x00;

        /// <summary>Wakeup byte sent at start of polling.</summary>
        public const byte WakeupByte = 0x80;

        /// <summary>NACK byte.</summary>
        public const byte NACK = 0xFF;

        /// <summary>Default polling interval in milliseconds.</summary>
        public const int DefaultPollingIntervalMs = 20;

        /// <summary>CRC polynomial for SAS CRC-16/CRC-CCITT.</summary>
        public const ushort CrcPolynomial = 0x1021;

        /// <summary>Maximum message size in bytes.</summary>
        public const int MaxMessageSize = 255;
    }
}
