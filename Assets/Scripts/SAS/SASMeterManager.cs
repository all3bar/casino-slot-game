using System;
using CasinoSlot.Accounting;

namespace CasinoSlot.SAS
{
    /// <summary>
    /// Builds comprehensive meter response messages for SAS command 0x7B (Send Gaming Machine Meters).
    /// </summary>
    public class SASMeterManager
    {
        private readonly MeterManager _meters;

        public SASMeterManager(MeterManager meters)
        {
            _meters = meters;
        }

        /// <summary>
        /// Builds the full game machine meters response (command 0x7B).
        /// Format: [CoinIn 5B BCD][CoinOut 5B BCD][CoinDrop 5B BCD][Jackpot 5B BCD]
        /// </summary>
        public byte[] BuildAllMetersResponse(SASProtocol protocol)
        {
            var coinIn   = SASProtocol.EncodeBCD5(_meters.TotalCoinIn);
            var coinOut  = SASProtocol.EncodeBCD5(_meters.TotalCoinOut);
            var drop     = SASProtocol.EncodeBCD5(_meters.TotalDrop);
            var jackpot  = SASProtocol.EncodeBCD5(_meters.TotalJackpot);
            var gamesPlayed = SASProtocol.EncodeBCD5(_meters.GamesPlayed);
            var gamesWon = SASProtocol.EncodeBCD5(_meters.GamesWon);

            byte[] data = new byte[30]; // 6 meters x 5 bytes
            Array.Copy(coinIn,      0, data,  0, 5);
            Array.Copy(coinOut,     0, data,  5, 5);
            Array.Copy(drop,        0, data, 10, 5);
            Array.Copy(jackpot,     0, data, 15, 5);
            Array.Copy(gamesPlayed, 0, data, 20, 5);
            Array.Copy(gamesWon,    0, data, 25, 5);

            return protocol.BuildResponse(SASCommandCodes.SendGameMachineMeters, data);
        }
    }
}
