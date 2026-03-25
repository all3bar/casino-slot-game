using System;
using Xunit;
using CasinoSlot.SAS;
using CasinoSlot.Accounting;

namespace CasinoSlot.Tests.SAS
{
    /// <summary>
    /// Unit tests for SAS protocol message building, parsing, and CRC validation.
    /// </summary>
    public class SASProtocolTests
    {
        [Fact]
        public void BuildResponse_IncludesCRC()
        {
            var protocol = new SASProtocol(1);
            byte[] response = protocol.BuildResponse(0x2E, new byte[] { 0x00, 0x01, 0x23, 0x45 });
            // Message: [addr][cmd][data...][crc-l][crc-h]
            Assert.Equal(8, response.Length); // 1 + 1 + 4 + 2
            Assert.True(SASProtocol.ValidateCRC(response));
        }

        [Fact]
        public void ValidateCRC_ValidMessage_ReturnsTrue()
        {
            var protocol = new SASProtocol(1);
            byte[] msg = protocol.BuildResponse(0x54, new byte[] { 0x00, 0x00, 0x01, 0x23, 0x45 });
            Assert.True(SASProtocol.ValidateCRC(msg));
        }

        [Fact]
        public void ValidateCRC_TamperedMessage_ReturnsFalse()
        {
            var protocol = new SASProtocol(1);
            byte[] msg = protocol.BuildResponse(0x54, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
            // Tamper with data
            msg[3] ^= 0xFF;
            Assert.False(SASProtocol.ValidateCRC(msg));
        }

        [Fact]
        public void ValidateCRC_NullMessage_ReturnsFalse()
        {
            Assert.False(SASProtocol.ValidateCRC(null));
        }

        [Fact]
        public void ValidateCRC_TooShortMessage_ReturnsFalse()
        {
            Assert.False(SASProtocol.ValidateCRC(new byte[] { 0x01, 0x02 }));
        }

        [Fact]
        public void ComputeCRC_Deterministic()
        {
            byte[] data = { 0x01, 0x2E, 0x00, 0x00, 0x12, 0x34 };
            ushort crc1 = SASProtocol.ComputeCRC(data);
            ushort crc2 = SASProtocol.ComputeCRC(data);
            Assert.Equal(crc1, crc2);
        }

        [Fact]
        public void EncodeBCD4_CorrectEncoding()
        {
            // 12345678 -> bytes: 0x12, 0x34, 0x56, 0x78
            byte[] bcd = SASProtocol.EncodeBCD4(12345678);
            Assert.Equal(0x12, bcd[0]);
            Assert.Equal(0x34, bcd[1]);
            Assert.Equal(0x56, bcd[2]);
            Assert.Equal(0x78, bcd[3]);
        }

        [Fact]
        public void EncodeBCD5_CorrectEncoding()
        {
            // 1234567890 -> 5 bytes
            byte[] bcd = SASProtocol.EncodeBCD5(1234567890);
            long decoded = SASProtocol.DecodeBCD(bcd);
            Assert.Equal(1234567890L, decoded);
        }

        [Fact]
        public void DecodeBCD_RoundTrip()
        {
            long value = 9876543210L;
            byte[] bcd = SASProtocol.EncodeBCD5(value);
            long decoded = SASProtocol.DecodeBCD(bcd);
            Assert.Equal(value, decoded);
        }

        [Fact]
        public void EncodeASCII_PadsToLength()
        {
            byte[] encoded = SASProtocol.EncodeASCII("AB", 6);
            Assert.Equal(6, encoded.Length);
            Assert.Equal((byte)'A', encoded[0]);
            Assert.Equal((byte)'B', encoded[1]);
            Assert.Equal(0x20, encoded[2]); // space padding
        }

        [Fact]
        public void ParseMessage_ValidMessage_Succeeds()
        {
            var protocol = new SASProtocol(1);
            byte[] msg = protocol.BuildResponse(0x54, new byte[] { 0x00, 0x00, 0x01, 0x23, 0x45 });
            bool ok = SASProtocol.ParseMessage(msg, out byte addr, out byte cmd, out byte[] payload);
            Assert.True(ok);
            Assert.Equal(1, addr);
            Assert.Equal(0x54, cmd);
        }

        [Fact]
        public void ParseMessage_InvalidCRC_Fails()
        {
            var protocol = new SASProtocol(1);
            byte[] msg = protocol.BuildResponse(0x54, new byte[] { 0x01, 0x02, 0x03 });
            msg[msg.Length - 1] ^= 0xFF; // corrupt CRC
            bool ok = SASProtocol.ParseMessage(msg, out _, out _, out _);
            Assert.False(ok);
        }

        [Fact]
        public void MachineAddress_SetValidRange()
        {
            var protocol = new SASProtocol(127);
            Assert.Equal(127, protocol.MachineAddress);
        }

        [Fact]
        public void MachineAddress_InvalidRange_Clamps()
        {
            var protocol = new SASProtocol(0); // 0 is invalid, should become 1
            Assert.Equal(1, protocol.MachineAddress);
        }
    }
}
