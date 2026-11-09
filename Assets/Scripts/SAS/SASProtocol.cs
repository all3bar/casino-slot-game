using System;
using System.Collections.Generic;
using System.Text;

namespace CasinoSlot.SAS
{
    /// <summary>
    /// Implements SAS 6.0 protocol message building, parsing, and CRC validation.
    /// All messages follow the format: [Address] [Command] [Data...] [CRC-L] [CRC-H]
    /// </summary>
    public class SASProtocol
    {
        private byte _machineAddress;

        /// <summary>Gets or sets the SAS machine address (1-127).</summary>
        public byte MachineAddress
        {
            get => _machineAddress;
            set => _machineAddress = (value >= 1 && value <= 127) ? value : (byte)1;
        }

        /// <summary>
        /// Initializes SAS protocol handler.
        /// </summary>
        /// <param name="address">Machine SAS address (1-127).</param>
        public SASProtocol(byte address = 1)
        {
            MachineAddress = address;
        }

        /// <summary>
        /// Builds a SAS response message with address, command, data, and CRC.
        /// </summary>
        /// <param name="command">Command byte.</param>
        /// <param name="data">Response data bytes (without CRC).</param>
        /// <returns>Complete message bytes including CRC.</returns>
        public byte[] BuildResponse(byte command, byte[] data)
        {
            var msg = new List<byte>();
            msg.Add(_machineAddress);
            msg.Add(command);
            if (data != null)
                msg.AddRange(data);

            ushort crc = ComputeCRC(msg.ToArray());
            msg.Add((byte)(crc & 0xFF));
            msg.Add((byte)(crc >> 8));

            return msg.ToArray();
        }

        /// <summary>
        /// Validates the CRC of a received message.
        /// </summary>
        /// <param name="message">Full message including CRC bytes at end.</param>
        /// <returns>True if CRC is valid.</returns>
        public static bool ValidateCRC(byte[] message)
        {
            if (message == null || message.Length < 4) return false;

            byte[] dataWithoutCrc = new byte[message.Length - 2];
            Array.Copy(message, dataWithoutCrc, dataWithoutCrc.Length);

            ushort receivedCrc = (ushort)(message[message.Length - 2] | (message[message.Length - 1] << 8));
            ushort computedCrc = ComputeCRC(dataWithoutCrc);
            if (computedCrc == receivedCrc) return true;

            // SAS 6.0: If address had wakeup bit (bit 7) set on wire, retry with bit 7 cleared
            dataWithoutCrc[0] = (byte)(dataWithoutCrc[0] & 0x7F);
            return ComputeCRC(dataWithoutCrc) == receivedCrc;
        }

        /// <summary>
        /// Computes the SAS CRC-16 (CRC-CCITT variant with 0x0000 init).
        /// </summary>
        public static ushort ComputeCRC(byte[] data)
        {
            ushort crc = 0x0000;
            foreach (byte b in data)
            {
                crc ^= (ushort)(b << 8);
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x8000) != 0)
                        crc = (ushort)((crc << 1) ^ SASConstants.CrcPolynomial);
                    else
                        crc <<= 1;
                }
            }
            return crc;
        }

        /// <summary>
        /// Parses an incoming SAS poll message.
        /// </summary>
        /// <param name="rawBytes">Raw bytes received from host.</param>
        /// <param name="address">Parsed address byte.</param>
        /// <param name="command">Parsed command byte.</param>
        /// <param name="payload">Remaining payload bytes.</param>
        /// <returns>True if message was parsed and CRC validated successfully.</returns>
        public static bool ParseMessage(byte[] rawBytes, out byte address, out byte command, out byte[] payload)
        {
            address = 0;
            command = 0;
            payload = new byte[0];

            if (rawBytes == null || rawBytes.Length < 4) return false;
            if (!ValidateCRC(rawBytes)) return false;

            address = (byte)(rawBytes[0] & 0x7F);
            command = rawBytes[1];
            payload = new byte[rawBytes.Length - 4]; // minus address, command, crc-l, crc-h
            if (payload.Length > 0)
                Array.Copy(rawBytes, 2, payload, 0, payload.Length);

            return true;
        }

        /// <summary>
        /// Encodes a 4-byte BCD credit value (for meter responses).
        /// </summary>
        public static byte[] EncodeBCD4(long value)
        {
            value = Math.Abs(value) % 100000000L; // Max 8 BCD digits
            byte[] result = new byte[4];
            for (int i = 3; i >= 0; i--)
            {
                int lo = (int)(value % 10); value /= 10;
                int hi = (int)(value % 10); value /= 10;
                result[i] = (byte)((hi << 4) | lo);
            }
            return result;
        }

        /// <summary>
        /// Encodes a meter value as 5-byte BCD (10 BCD digits).
        /// </summary>
        public static byte[] EncodeBCD5(long value)
        {
            value = Math.Abs(value) % 10000000000L;
            byte[] result = new byte[5];
            for (int i = 4; i >= 0; i--)
            {
                int lo = (int)(value % 10); value /= 10;
                int hi = (int)(value % 10); value /= 10;
                result[i] = (byte)((hi << 4) | lo);
            }
            return result;
        }

        /// <summary>
        /// Decodes a BCD-encoded byte array to a long integer.
        /// </summary>
        public static long DecodeBCD(byte[] data)
        {
            long result = 0;
            foreach (byte b in data)
            {
                result = result * 100 + ((b >> 4) * 10) + (b & 0x0F);
            }
            return result;
        }

        /// <summary>
        /// Encodes an ASCII game machine ID string into bytes (padded/truncated to length).
        /// </summary>
        public static byte[] EncodeASCII(string text, int length)
        {
            byte[] result = new byte[length];
            byte[] encoded = Encoding.ASCII.GetBytes(text ?? "");
            int copyLen = Math.Min(encoded.Length, length);
            Array.Copy(encoded, result, copyLen);
            // Pad with spaces
            for (int i = copyLen; i < length; i++)
                result[i] = 0x20;
            return result;
        }
    }
}
