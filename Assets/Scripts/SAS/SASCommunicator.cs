using System;
using System.Collections.Generic;
using System.Text;
using CasinoSlot.Accounting;
using CasinoSlot.Core;

namespace CasinoSlot.SAS
{
    /// <summary>
    /// Configuration for the gaming machine identity used in SAS responses.
    /// </summary>
    public class MachineIdentity
    {
        public string SerialNumber { get; set; } = "SN000001";
        public string AssetNumber  { get; set; } = "001";
        public string GameId       { get; set; } = "CAS001";
        public string Manufacturer { get; set; } = "CasinoTech";
        public string ModelName    { get; set; } = "ProSlot5000";
        public byte   SASAddress   { get; set; } = 0x01;
        public int    Denomination { get; set; } = 1; // credits per denomination
    }

    /// <summary>
    /// Processes SAS 6.0 poll commands and generates responses.
    /// Implements the Command pattern for SAS message handling.
    /// </summary>
    public class SASCommandProcessor
    {
        private readonly SASProtocol _protocol;
        private readonly MeterManager _meters;
        private readonly GameManager _gameManager;
        private readonly MachineIdentity _identity;
        private readonly SASMeterManager _sasMeterManager;
        private readonly EventLogger _eventLogger;

        private readonly SASExceptionManager _exceptionManager;

        // Last accepted bill info
        private byte _lastBillDenomination;
        private byte _lastBillCountry = 0x00; // USA

        /// <summary>
        /// Initializes the command processor.
        /// </summary>
        public SASCommandProcessor(
            MachineIdentity identity,
            MeterManager meters,
            GameManager gameManager,
            EventLogger eventLogger,
            SASExceptionManager exceptionManager = null)
        {
            _identity = identity;
            _meters = meters;
            _gameManager = gameManager;
            _eventLogger = eventLogger;
            _exceptionManager = exceptionManager ?? new SASExceptionManager();
            _protocol = new SASProtocol(identity.SASAddress);
            _sasMeterManager = new SASMeterManager(meters);
        }

        /// <summary>Gets the exception manager.</summary>
        public SASExceptionManager ExceptionManager => _exceptionManager;

        /// <summary>
        /// Handles a 1-byte SAS General Poll (GP).
        /// Returns 1-byte exception code if an exception is pending, or 0x00 (No Activity) to keep connection Online.
        /// Global Sync Poll 0x80 returns null (no response per SAS specification).
        /// </summary>
        public byte[] HandleGeneralPoll(byte pollByte)
        {
            byte addr = (byte)(pollByte & 0x7F);
            // Global Sync Poll (0x80) or polls not matching our SAS Address: do not respond
            if (addr != _identity.SASAddress) return null;

            var ex = _exceptionManager.DequeueException();
            if (ex != null)
            {
                _eventLogger.Log(EventType.SASException, $"SAS GP Exception Response: 0x{ex.Code:X2} ({ex.Description})");
                return new byte[] { ex.Code };
            }

            // Standard SAS 6.02: Return 0x00 (No Activity) so the Host verifies EGM is Online
            return new byte[] { 0x00 };
        }

        /// <summary>
        /// Processes a raw SAS message and returns the response bytes.
        /// Returns null if the message is not addressed to this machine or is invalid.
        /// </summary>
        public byte[] ProcessMessage(byte[] rawMessage)
        {
            if (!SASProtocol.ParseMessage(rawMessage, out byte address, out byte command, out byte[] payload))
                return null;

            // Only handle broadcasts or messages addressed to us
            if (address != _identity.SASAddress && address != SASConstants.BroadcastAddress)
                return null;

            _eventLogger.Log(EventType.SASCommand, $"SAS Command: 0x{command:X2}");

            return command switch
            {
                SASCommandCodes.Shutdown             => HandleShutdown(),
                SASCommandCodes.Startup              => HandleStartup(),
                SASCommandCodes.SoundOff             => HandleSoundOff(),
                SASCommandCodes.SoundOn              => HandleSoundOn(),
                SASCommandCodes.EnableBillAcceptor   => HandleEnableBillAcceptor(),
                SASCommandCodes.DisableBillAcceptor  => HandleDisableBillAcceptor(),
                SASCommandCodes.EnableGamePlay       => HandleEnableGamePlay(),
                SASCommandCodes.DisableGamePlay      => HandleDisableGamePlay(),
                0x0F                                 => HandleSendMeters10To15(),         // LP0F Core Meters
                0x1A                                 => HandleSendCurrentCredits(),       // LP1A Current Credits
                SASCommandCodes.SendCurrentCredits   => HandleSendCurrentCredits(),       // 0x2E
                0x1F                                 => HandleSendMachineIdAndInfo(),     // LP1F Machine ID & Config
                0x51                                 => HandleSendNumberOfGames(),        // LP51 Games Implemented
                SASCommandCodes.SendSelectedGameNumber => HandleSendSelectedGameNumber(), // 0x52
                SASCommandCodes.SendGamingMachineId  => HandleSendGamingMachineId(),      // 0x53
                0x54                                 => HandleSendSasVersionAndSerial(),  // LP54 SAS Version & Serial
                0x55                                 => HandleSendSelectedGameNumber(),   // LP55 Selected Game
                0x7E                                 => HandleSendDateTime(),             // LP7E Machine Date & Time
                SASCommandCodes.SendLastAcceptedBill => HandleSendLastAcceptedBill(),
                SASCommandCodes.SendTotalDrop        => HandleSendTotalDrop(),
                SASCommandCodes.SendTotalJackpot     => HandleSendTotalJackpot(),
                SASCommandCodes.SendGamesPlayed      => HandleSendGamesPlayed(),
                SASCommandCodes.SendGamesWon         => HandleSendGamesWon(),
                SASCommandCodes.SendAuthenticationInfo => HandleSendAuthenticationInfo(),
                SASCommandCodes.SendGameMachineMeters => HandleSendGameMachineMeters(),
                _                                    => null // Unknown command
            };
        }

        private byte[] HandleShutdown()
        {
            _gameManager.EnterMaintenance();
            return _protocol.BuildResponse(SASCommandCodes.Shutdown, null);
        }

        private byte[] HandleStartup()
        {
            _gameManager.ExitMaintenance();
            return _protocol.BuildResponse(SASCommandCodes.Startup, null);
        }

        private byte[] HandleSoundOff()
            => _protocol.BuildResponse(SASCommandCodes.SoundOff, null);

        private byte[] HandleSoundOn()
            => _protocol.BuildResponse(SASCommandCodes.SoundOn, null);

        private byte[] HandleEnableBillAcceptor()
            => _protocol.BuildResponse(SASCommandCodes.EnableBillAcceptor, null);

        private byte[] HandleDisableBillAcceptor()
            => _protocol.BuildResponse(SASCommandCodes.DisableBillAcceptor, null);

        private byte[] HandleEnableGamePlay()
            => _protocol.BuildResponse(SASCommandCodes.EnableGamePlay, null);

        private byte[] HandleDisableGamePlay()
            => _protocol.BuildResponse(SASCommandCodes.DisableGamePlay, null);

        private byte[] HandleSendCurrentCredits()
        {
            long credits = _gameManager.SlotMachine.Credits;
            byte[] data = SASProtocol.EncodeBCD4(credits);
            return _protocol.BuildResponse(SASCommandCodes.SendCurrentCredits, data);
        }

        private byte[] HandleSendEnabledGameNumbers()
        {
            // Single game enabled: game #1
            byte[] data = { 0x00, 0x01 }; // count=1, game#=01
            return _protocol.BuildResponse(SASCommandCodes.SendEnabledGameNumbers, data);
        }

        private byte[] HandleSendSelectedGameNumber()
        {
            byte[] data = { 0x00, 0x01 }; // game #1
            return _protocol.BuildResponse(SASCommandCodes.SendSelectedGameNumber, data);
        }

        private byte[] HandleSendGamingMachineId()
        {
            // [GameId 2 bytes] [Additional ID 2 bytes]
            var idBytes = SASProtocol.EncodeASCII(_identity.GameId, 2);
            var addlBytes = SASProtocol.EncodeASCII(_identity.AssetNumber, 2);
            byte[] data = new byte[4];
            Array.Copy(idBytes, 0, data, 0, 2);
            Array.Copy(addlBytes, 0, data, 2, 2);
            return _protocol.BuildResponse(SASCommandCodes.SendGamingMachineId, data);
        }

        private byte[] HandleSendLastAcceptedBill()
        {
            byte[] data = { _lastBillCountry, _lastBillDenomination, 0x01 }; // country, denom, accepted
            return _protocol.BuildResponse(SASCommandCodes.SendLastAcceptedBill, data);
        }

        private byte[] HandleSendTotalCoinIn()
        {
            byte[] data = SASProtocol.EncodeBCD5(_meters.TotalCoinIn);
            return _protocol.BuildResponse(SASCommandCodes.SendTotalCoinIn, data);
        }

        private byte[] HandleSendTotalCoinOut()
        {
            byte[] data = SASProtocol.EncodeBCD5(_meters.TotalCoinOut);
            return _protocol.BuildResponse(SASCommandCodes.SendTotalCoinOut, data);
        }

        private byte[] HandleSendTotalDrop()
        {
            byte[] data = SASProtocol.EncodeBCD5(_meters.TotalDrop);
            return _protocol.BuildResponse(SASCommandCodes.SendTotalDrop, data);
        }

        private byte[] HandleSendTotalJackpot()
        {
            byte[] data = SASProtocol.EncodeBCD5(_meters.TotalJackpot);
            return _protocol.BuildResponse(SASCommandCodes.SendTotalJackpot, data);
        }

        private byte[] HandleSendGamesPlayed()
        {
            byte[] data = SASProtocol.EncodeBCD5(_meters.GamesPlayed);
            return _protocol.BuildResponse(SASCommandCodes.SendGamesPlayed, data);
        }

        private byte[] HandleSendGamesWon()
        {
            byte[] data = SASProtocol.EncodeBCD5(_meters.GamesWon);
            return _protocol.BuildResponse(SASCommandCodes.SendGamesWon, data);
        }

        private byte[] HandleSendAuthenticationInfo()
        {
            // Authentication response: machine serial + software version
            var serial = SASProtocol.EncodeASCII(_identity.SerialNumber, 8);
            byte[] data = new byte[serial.Length + 2];
            Array.Copy(serial, data, serial.Length);
            data[serial.Length]     = 0x01; // auth method = SHA-1
            data[serial.Length + 1] = 0x00; // reserved
            return _protocol.BuildResponse(SASCommandCodes.SendAuthenticationInfo, data);
        }

        private byte[] HandleSendGameMachineMeters()
        {
            return _sasMeterManager.BuildAllMetersResponse(_protocol);
        }

        private byte[] HandleSendSasVersionAndSerial()
        {
            // LP54: [length] [SAS Version 3B ASCII] [Serial Number ASCII]
            string version = "602";
            string serial = _identity.SerialNumber ?? "SN000001";
            byte[] verBytes = Encoding.ASCII.GetBytes(version);
            byte[] serBytes = Encoding.ASCII.GetBytes(serial);

            byte length = (byte)(verBytes.Length + serBytes.Length);
            byte[] data = new byte[1 + length];
            data[0] = length;
            Array.Copy(verBytes, 0, data, 1, verBytes.Length);
            Array.Copy(serBytes, 0, data, 1 + verBytes.Length, serBytes.Length);

            return _protocol.BuildResponse(0x54, data);
        }

        private byte[] HandleSendMachineIdAndInfo()
        {
            // LP1F: GameId(2B), AddlId(3B), Denom(1B), MaxBet(1B), ProgGroup(3B), PaytableId(6B), TheoreticalRtp(4B)
            // Total 20 data bytes (+ addr + cmd + 2B crc = 24 bytes)
            byte[] data = new byte[20];
            byte[] gid = SASProtocol.EncodeASCII(_identity.GameId ?? "01", 2);
            byte[] aid = SASProtocol.EncodeASCII(_identity.AssetNumber ?? "001", 3);
            Array.Copy(gid, 0, data, 0, 2);
            Array.Copy(aid, 0, data, 2, 3);
            data[5] = 0x01; // Denomination code: 0x01 = 1 cent
            data[6] = 10;   // Max bet
            byte[] paytable = SASProtocol.EncodeASCII("STD001", 6);
            Array.Copy(paytable, 0, data, 10, 6);
            byte[] rtp = Encoding.ASCII.GetBytes("9600"); // 96.00%
            Array.Copy(rtp, 0, data, 16, 4);

            return _protocol.BuildResponse(0x1F, data);
        }

        private byte[] HandleSendNumberOfGames()
        {
            // LP51: 2-byte BCD count of implemented games = 0x00, 0x01 (1 game)
            byte[] data = { 0x00, 0x01 };
            return _protocol.BuildResponse(0x51, data);
        }

        private byte[] HandleSendMeters10To15()
        {
            // LP0F: 6 BCD-4 meters: Canceled, CoinIn, CoinOut, Drop, Jackpot, GamesPlayed (24 bytes)
            byte[] data = new byte[24];
            Array.Copy(SASProtocol.EncodeBCD4(0), 0, data, 0, 4);                     // Canceled
            Array.Copy(SASProtocol.EncodeBCD4(_meters.TotalCoinIn), 0, data, 4, 4);   // Coin In
            Array.Copy(SASProtocol.EncodeBCD4(_meters.TotalCoinOut), 0, data, 8, 4);  // Coin Out
            Array.Copy(SASProtocol.EncodeBCD4(_meters.TotalDrop), 0, data, 12, 4);    // Drop
            Array.Copy(SASProtocol.EncodeBCD4(_meters.TotalJackpot), 0, data, 16, 4); // Jackpot
            Array.Copy(SASProtocol.EncodeBCD4(_meters.GamesPlayed), 0, data, 20, 4);  // Games Played
            return _protocol.BuildResponse(0x0F, data);
        }

        private byte[] HandleSendDateTime()
        {
            // LP7E: MM(1B), DD(1B), YYYY(2B), HH(1B), MM(1B), SS(1B) BCD (7 bytes)
            var now = DateTime.Now;
            byte[] data = new byte[7];
            data[0] = (byte)(((now.Month / 10) << 4) | (now.Month % 10));
            data[1] = (byte)(((now.Day / 10) << 4) | (now.Day % 10));
            int year = now.Year;
            data[2] = (byte)((((year / 100) / 10) << 4) | ((year / 100) % 10));
            data[3] = (byte)((((year % 100) / 10) << 4) | ((year % 100) % 10));
            data[4] = (byte)(((now.Hour / 10) << 4) | (now.Hour % 10));
            data[5] = (byte)(((now.Minute / 10) << 4) | (now.Minute % 10));
            data[6] = (byte)(((now.Second / 10) << 4) | (now.Second % 10));
            return _protocol.BuildResponse(0x7E, data);
        }

        /// <summary>
        /// Notifies the processor that a bill was accepted (for last-bill responses).
        /// </summary>
        public void NotifyBillAccepted(byte denominationCode)
        {
            _lastBillDenomination = denominationCode;
        }
    }
}
