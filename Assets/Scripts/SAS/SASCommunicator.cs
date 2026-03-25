using System;
using System.Collections.Generic;
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
            EventLogger eventLogger)
        {
            _identity = identity;
            _meters = meters;
            _gameManager = gameManager;
            _eventLogger = eventLogger;
            _protocol = new SASProtocol(identity.SASAddress);
            _sasMeterManager = new SASMeterManager(meters);
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
                SASCommandCodes.SendCurrentCredits   => HandleSendCurrentCredits(),
                SASCommandCodes.SendEnabledGameNumbers => HandleSendEnabledGameNumbers(),
                SASCommandCodes.SendSelectedGameNumber => HandleSendSelectedGameNumber(),
                SASCommandCodes.SendGamingMachineId  => HandleSendGamingMachineId(),
                SASCommandCodes.SendLastAcceptedBill => HandleSendLastAcceptedBill(),
                SASCommandCodes.SendTotalCoinIn      => HandleSendTotalCoinIn(),
                SASCommandCodes.SendTotalCoinOut     => HandleSendTotalCoinOut(),
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

        /// <summary>
        /// Notifies the processor that a bill was accepted (for last-bill responses).
        /// </summary>
        public void NotifyBillAccepted(byte denominationCode)
        {
            _lastBillDenomination = denominationCode;
        }
    }
}
