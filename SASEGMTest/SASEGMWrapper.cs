using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace SASEGMTest
{
    // this is a hack for testing. It was never my intention to write client side code, only the DLL.
    // Someone with solid experience should be able to clean it up, or just use it as it is 
    // Its a singleton, due to the nature of the dll.
    //
    // Most stuff is simply implemented as callbacks, but there is a few events implemented as an example.

    
    class SASEGMWrapper
    {

        //SASEGMWrapper()
        //{
            
        //}
        
        #region imported methods from dll.
        [DllImport("SASEGM.dll",CallingConvention=CallingConvention.StdCall,CharSet=CharSet.Ansi)]
        public static extern UInt32 SAS_Initialise(string commPort);

        [DllImport("SASEGM.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern UInt32 SAS_UnInitialise();

        [DllImport("SASEGM.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern UInt32 SAS_AddEGM(byte byAddress);

        [DllImport("SASEGM.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern void SAS_RegisterExceptionCallback([MarshalAs(UnmanagedType.FunctionPtr)] 
            ExceptionCodeCallbackDelegate cb);

        [DllImport("SASEGM.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern void SAS_RegisterROMSignatureCallback([MarshalAs(UnmanagedType.FunctionPtr)] 
            ROMSignatureCallbackDelegate cb);

        [DllImport("SASEGM.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern void SAS_RegisterSASVersionAndSerialCallback([MarshalAs(UnmanagedType.FunctionPtr)] 
            SASVersionAndSerialCallbackDelegate cb);

        [DllImport("SASEGM.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern UInt32 SAS_RequestROMSignature(byte machineAddress, ushort seed);

        [DllImport("SASEGM.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern UInt32 SAS_RequestSASVersionAndEGMSerial(byte machineAddressd);

        [DllImport("SASEGM.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern UInt32 SAS_InitiateLegacyBonus(byte machineAddress, UInt32 amount /* credits */, byte taxCode );

        [DllImport("SASEGM.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern UInt32 SAS_GetSASExceptionCodeText(byte byExceptionCode, int bufferSize, StringBuilder exceptionCodeText);
        #endregion

        #region DLL Callbacks
 
        // Exception Code callbacks and event trigger
        [UnmanagedFunctionPointerAttribute(CallingConvention.StdCall)]
        public delegate void ExceptionCodeCallbackDelegate(byte pollAddress, byte exceptionCode);


        //not sure why this gets collected by the GC?
        public static ExceptionCodeCallbackDelegate exceptionCodeCallBackDelegate
            = new ExceptionCodeCallbackDelegate(ExceptionCodeCallback);

        public static void ExceptionCodeCallback(byte pollAddress, byte exceptionCode)
        {
            StringBuilder exceptionMessage = new StringBuilder(256);

            try
            {
                SASEGMWrapper.SAS_GetSASExceptionCodeText(exceptionCode, exceptionMessage.Capacity, exceptionMessage);
            }
            catch (Exception)
            {
            }

            instance.OnException(new ExceptionCodeEventArgs(pollAddress,exceptionCode,exceptionMessage.ToString()));
 
        }

        //ROM Signature callback and event trigger
        [UnmanagedFunctionPointerAttribute(CallingConvention.StdCall)]
        public delegate void ROMSignatureCallbackDelegate(byte pollAddress, ushort romSignature);

        public static ROMSignatureCallbackDelegate romSignatureCallbackDelgate =
            new ROMSignatureCallbackDelegate(ROMSignatureCallback);


        public static void ROMSignatureCallback(byte pollAddress, ushort romSignature)
        {
            instance.OnROMSignature(new ROMSignatureEventArgs(pollAddress,romSignature));
        }

        //SAS Version and EGM Serial Number
        [UnmanagedFunctionPointerAttribute(CallingConvention.StdCall)]
        public delegate void SASVersionAndSerialCallbackDelegate(byte byPollAddress, string szVersion, int versionSize, string szSerial, int serialSize);

        public static SASVersionAndSerialCallbackDelegate sasVersionAndSerialCallbackDelegate = 
            new SASVersionAndSerialCallbackDelegate(SASVersionAndSerialCallback);

        public static void SASVersionAndSerialCallback(byte byPollAddress,string szVersion, int versionSize, string szSerial, int serialSize)
        {
            instance.OnSASVersionAndMachineSerialNumber(new SASVersionAndMachineSerialEventArgs(szVersion, szSerial));
        }




        #endregion

        #region Events

        //Exception code event
        public delegate void ExceptionCodeEventHandler(object sender, ExceptionCodeEventArgs e);
        public event ExceptionCodeEventHandler ExceptionCode;
        private ExceptionCodeEventHandler excpetionCodeEventHandler = null;
        protected virtual void OnException(ExceptionCodeEventArgs e)
        {
            if (ExceptionCode != null)
            {
                ExceptionCode(this, e);
            }
        }

        //Rom Signature Event.
        public delegate void ROMSignatureEventHandler(object sender, ROMSignatureEventArgs e);
        public event ROMSignatureEventHandler ROMSignature;
        private ROMSignatureEventHandler romSignatureEventHandler = null;
        protected virtual void OnROMSignature(ROMSignatureEventArgs e)
        {
            if (ROMSignature != null)
            {
                ROMSignature(this, e);
            }
        }

        //SAS Version and Gaming Machine Serial Number event
        public delegate void SASVersionAndMachineSerialNumberEventHandler(object sender, SASVersionAndMachineSerialEventArgs e);
        public event SASVersionAndMachineSerialNumberEventHandler SASVersionAndMachineSerialNumber;
        private SASVersionAndMachineSerialNumberEventHandler sasVersionAndMachineSerialNumber = null;
        protected virtual void OnSASVersionAndMachineSerialNumber(SASVersionAndMachineSerialEventArgs e)
        {
            if (SASVersionAndMachineSerialNumber != null)
            {
                SASVersionAndMachineSerialNumber(this, e);
            }
        }

        #endregion



        #region singleton pattern.
        public static SASEGMWrapper Instance
        {
            get { return instance; }
        }

        private static SASEGMWrapper instance = new SASEGMWrapper();

        private SASEGMWrapper() 
        {
        }
        #endregion

    }

    public class ExceptionCodeEventArgs : EventArgs
    {
        private readonly byte pollAddress;
        private readonly byte exceptionCode;
        private readonly string exceptionCodeMessage;

        public byte PollAddress
        {
            get { return pollAddress; }
        }

        public byte ExceptionCode
        {
            get { return exceptionCode; }
        }

        public string ExceptionCodeMessage
        {
            get { return exceptionCodeMessage; }
        }

        public ExceptionCodeEventArgs(byte pollAddress, byte exceptionCode, string exceptionCodeMessage)
        {
            this.pollAddress = pollAddress;
            this.exceptionCode = exceptionCode;
            this.exceptionCodeMessage = exceptionCodeMessage;
        }

    }

    public class ROMSignatureEventArgs : EventArgs
    {
        private readonly byte pollAddress;
        private readonly ushort romSignature;

        public byte PollAddress
        {
            get { return pollAddress; }
        }

        public ushort ROMSignature
        {
            get { return romSignature; }
        }

        public ROMSignatureEventArgs(byte pollAddress, ushort romSignature)
        {
            this.pollAddress = pollAddress;
            this.romSignature = romSignature;
        }
    }

    public class SASVersionAndMachineSerialEventArgs : EventArgs
    {
        private readonly string sasVersion;
        private readonly string serialNumber;

        public string SASVersion
        {
            get { return sasVersion; }
        }

        public string SerialNumber
        {
            get { return serialNumber; }
        }

        public SASVersionAndMachineSerialEventArgs(string sasVersion, string serialNumber)
        {
            this.sasVersion = sasVersion;
            this.serialNumber = serialNumber;
        }

    }





    enum SASExcpetionCode : byte
    {
        No_activity = 0x00,
        Slot_door_was_opened = 0x11,
        Slot_door_was_closed = 0x12,
        Drop_door_was_opened = 0x13,
        Drop_door_was_closed = 0x14,
        Card_cage_was_opened = 0x15,
        Card_cage_was_closed = 0x16,
        AC_power_was_applied_to_gaming_machine = 0x17,
        AC_power_was_lost_from_gaming_machine = 0x18,
        Cashbox_door_was_opened = 0x19,
        Cashbox_door_was_closed = 0x1A,
        Cashbox_was_removed = 0x1B,
        Cashbox_was_installed = 0x1C,
        Belly_door_was_opened = 0x1D,
        Belly_door_was_closed = 0x1E,
        No_activity_and_waiting_for_player_input = 0x1F,
        General_tilt = 0x20,
        Coin_in_tilt = 0x21,
        Coin_out_tilt = 0x22,
        Hopper_empty_detected = 0x23,
        Extra_coin_paid = 0x24,
        Diverter_malfunction = 0x25,
        Cashbox_full_detected = 0x27,
        Bill_jam = 0x28,
        Bill_acceptor_hardware_failure = 0x29,
        Reverse_bill_detected = 0x2A,
        Bill_rejected = 0x2B,
        Counterfeit_bill_detected = 0x2C,
        Reverse_coin_in_detected = 0x2D,
        Cashbox_near_full_detected = 0x2E,
        CMOS_RAM_error_data_recovered_from_EEPROM = 0x31,
        CMOS_RAM_error_no_data_recovered_from_EEPROM = 0x32,
        CMOS_RAM_error_bad_device = 0x33,
        EEPROM_error_data_error = 0x34,
        EEPROM_error_bad_device = 0x35,
        EPROM_error_different_checksum_version_changed = 0x36,
        EPROM_error_bad_checksum_compare = 0x37,
        Partitioned_EPROM_error_csum_version_changed = 0x38,
        Partitioned_EPROM_error_bad_checksum_compare = 0x39,
        Memory_error_reset = 0x3A,
        Low_backup_battery_detected = 0x3B,
        Operator_changed_options = 0x3C,
        A_cash_out_ticket_has_been_printed = 0x3D,
        A_handpay_has_been_validated = 0x3E,
        Validation_ID_not_configured = 0x3F,
        Reel_Tilt = 0x40,
        Reel_1_tilt = 0x41,
        Reel_2_tilt = 0x42,
        Reel_3_tilt = 0x43,
        Reel_4_tilt = 0x44,
        Reel_5_tilt = 0x45,
        Reel_mechanism_disconnected = 0x46,
        One_dollar_bill_accepted = 0x47,
        Five_dollar_bill_accepted = 0x48,
        Ten_dollar_bill_accepted = 0x49,
        Twenty_bill_accepted = 0x4A,
        Fifty_dollar_bill_accepted = 0x4B,
        Hundred_dollar_bill_accepted = 0x4C,
        Two_dollar_bill_accepted = 0x4D,
        Five_hundred_dollar_bill_accepted = 0x4E,
        Bill_accepted = 0x4F,
        Two_hundred_dollar_bill_accepted = 0x50,
        Handpay_is_pending = 0x51,
        Handpay_was_reset = 0x52,
        No_progressive_info_received_for_5_seconds = 0x53,
        Progressive_win = 0x54,
        Player_has_cancelled_the_handpay_request = 0x55,
        SAS_progressive_level_hit = 0x56,
        System_validation_request = 0x57,
        Printer_communication_error = 0x60,
        Printer_paper_out_error = 0x61,
        Cash_out_button_pressed = 0x66,
        Ticket_has_been_inserted = 0x67,
        Ticket_transfer_complete = 0x68,
        AFT_transfer_complete = 0x69,
        AFT_request_for_host_cashout = 0x6A,
        AFT_request_for_host_to_cash_out_win = 0x6B,
        AFT_request_to_register = 0x6C,
        AFT_registration_acknowledged = 0x6D,
        AFT_registration_cancelled = 0x6E,
        Game_locked = 0x6F,
        Exception_buffer_overflow = 0x70,
        Change_lamp_on = 0x71,
        Change_lamp_off = 0x72,
        Printer_paper_low = 0x74,
        Printer_power_off = 0x75,
        Printer_power_on = 0x76,
        Replace_printer_ribbon = 0x77,
        Printer_carriage_jammed = 0x78,
        Coin_in_lockout_malfunction = 0x79,
        Gaming_machine_soft_meters_reset_to_zero = 0x7A,
        Bill_validator_totals_reset_by_an_attendant = 0x7B,
        A_legacy_bonus_pay_and_or_multiplied_jackpot = 0x7C,
        Game_has_started = 0x7E,
        Game_has_ended = 0x7F,
        Hopper_full_detected = 0x80,
        Hopper_level_low_detected = 0x81,
        Display_meters_or_attendant_menu_entered = 0x82,
        Display_meters_or_attendant_menu_exited = 0x83,
        Self_test_or_operator_menu_has_been_entered = 0x84,
        Self_test_or_operator_menu_has_been_exited = 0x85,
        Gaming_machine_is_out_of_service = 0x86,
        Player_has_requested_draw_cards = 0x87,
        Reel_N_has_stopped = 0x88,
        Coin_credit_wagered = 0x89,
        Game_recall_entry_has_been_displayed = 0x8A,
        Card_held_not_held = 0x8B,
        Game_selected = 0x8C,
        Component_list_changed = 0x8E,
        Authentication_complete = 0x8F,
        Power_off_card_cage_access = 0x98,
        Power_off_slot_door_access = 0x99,
        Power_off_cashbox_door_access = 0x9A,
        Power_off_drop_door_access = 0x9B,
    }
}
