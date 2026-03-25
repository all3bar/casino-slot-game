using System;
using System.Collections.Generic;

namespace CasinoSlot.Hardware
{
    /// <summary>
    /// Implements the ID-003 bill acceptor communication protocol.
    /// ID-003 is an industry-standard RS-232 protocol for bill validators.
    /// 
    /// Message format: [STX][LEN][DATA...][ETX][CHK]
    /// - STX: 0xFC
    /// - LEN: Length of [DATA + ETX]
    /// - ETX: 0xFD
    /// - CHK: XOR of all bytes from STX through ETX
    /// </summary>
    public class ID003Protocol
    {
        private const byte STX = 0xFC;
        private const byte ETX = 0xFD;

        // ID-003 event codes (from acceptor to controller)
        public const byte EVENT_IDLE           = 0x11;
        public const byte EVENT_ACCEPTING      = 0x12;
        public const byte EVENT_ESCROW         = 0x13;
        public const byte EVENT_STACKING       = 0x14;
        public const byte EVENT_STACKED        = 0x15;
        public const byte EVENT_RETURNING      = 0x16;
        public const byte EVENT_RETURNED       = 0x17;
        public const byte EVENT_CHEATED        = 0x18;
        public const byte EVENT_REJECTED       = 0x19;
        public const byte EVENT_JAMMED         = 0x1A;
        public const byte EVENT_STACKER_FULL   = 0x41;
        public const byte EVENT_STACKER_OPEN   = 0x42;
        public const byte EVENT_DISABLED       = 0x43;

        // ID-003 command codes (from controller to acceptor)
        public const byte CMD_STATUS           = 0x11;
        public const byte CMD_STACK            = 0x41;
        public const byte CMD_RETURN           = 0x43;
        public const byte CMD_ENABLE_DISABLE   = 0x30;
        public const byte CMD_RESET            = 0xF1;

        private readonly BillAcceptor _billAcceptor;
        private readonly SerialCommunication _serial;
        private readonly byte[] _receiveBuffer = new byte[512];
        private int _bufferPos;

        // Bill denomination mapping (ID-003 bill type code -> cents)
        private static readonly Dictionary<byte, int> BillDenominations = new Dictionary<byte, int>
        {
            { 0x01, 100 },   // $1
            { 0x02, 200 },   // $2
            { 0x03, 500 },   // $5
            { 0x04, 1000 },  // $10
            { 0x05, 2000 },  // $20
            { 0x06, 5000 },  // $50
            { 0x07, 10000 }, // $100
        };

        /// <summary>Raised when a complete ID-003 message is received and parsed.</summary>
        public event Action<byte, byte[]> MessageReceived;

        /// <summary>
        /// Initializes the ID-003 protocol handler.
        /// </summary>
        public ID003Protocol(BillAcceptor billAcceptor, SerialCommunication serial)
        {
            _billAcceptor = billAcceptor;
            _serial = serial;
            _serial.DataReceived += OnDataReceived;
        }

        /// <summary>
        /// Sends a command to the bill acceptor hardware.
        /// </summary>
        public void SendCommand(byte commandCode, byte[] data = null)
        {
            var msg = BuildMessage(commandCode, data ?? new byte[0]);
            _serial.Send(msg);
        }

        /// <summary>
        /// Sends a stack command to the acceptor (accept bill in escrow).
        /// </summary>
        public void SendStack() => SendCommand(CMD_STACK);

        /// <summary>
        /// Sends a return command to the acceptor (return bill in escrow).
        /// </summary>
        public void SendReturn() => SendCommand(CMD_RETURN);

        /// <summary>
        /// Sends an enable/disable command.
        /// </summary>
        public void SendEnable(bool enable)
        {
            byte[] data = { (byte)(enable ? 0x00 : 0x01) };
            SendCommand(CMD_ENABLE_DISABLE, data);
        }

        /// <summary>
        /// Sends a reset command to the acceptor.
        /// </summary>
        public void SendReset() => SendCommand(CMD_RESET);

        /// <summary>
        /// Builds an ID-003 framed message.
        /// </summary>
        public static byte[] BuildMessage(byte commandCode, byte[] data)
        {
            // [STX][LEN][CMD][DATA...][ETX][CHK]
            int dataLen = (data?.Length ?? 0);
            byte len = (byte)(1 + dataLen + 1); // CMD + DATA + ETX
            var msg = new List<byte> { STX, len, commandCode };
            if (data != null) msg.AddRange(data);
            msg.Add(ETX);

            byte chk = 0;
            foreach (byte b in msg) chk ^= b;
            msg.Add(chk);

            return msg.ToArray();
        }

        /// <summary>
        /// Validates an ID-003 message checksum.
        /// </summary>
        public static bool ValidateChecksum(byte[] message)
        {
            if (message == null || message.Length < 4) return false;
            byte chk = 0;
            for (int i = 0; i < message.Length - 1; i++)
                chk ^= message[i];
            return chk == message[message.Length - 1];
        }

        private void OnDataReceived(byte[] data)
        {
            foreach (byte b in data)
            {
                if (b == STX)
                {
                    _bufferPos = 0;
                    _receiveBuffer[_bufferPos++] = b;
                }
                else if (_bufferPos > 0)
                {
                    if (_bufferPos < _receiveBuffer.Length)
                        _receiveBuffer[_bufferPos++] = b;

                    if (b == ETX && _bufferPos >= 4)
                    {
                        // Read one more byte (checksum)
                        continue;
                    }

                    // After ETX, next byte is checksum
                    if (_bufferPos > 4 && _receiveBuffer[_bufferPos - 2] == ETX)
                    {
                        byte[] msg = new byte[_bufferPos];
                        Array.Copy(_receiveBuffer, msg, _bufferPos);
                        _bufferPos = 0;

                        if (ValidateChecksum(msg))
                            ProcessMessage(msg);
                    }
                }
            }
        }

        private void ProcessMessage(byte[] message)
        {
            if (message.Length < 4) return;
            byte eventCode = message[2];
            MessageReceived?.Invoke(eventCode, message);

            switch (eventCode)
            {
                case EVENT_ESCROW:
                    byte billType = message.Length > 3 ? message[3] : (byte)0;
                    int denom = BillDenominations.TryGetValue(billType, out int d) ? d : 0;
                    if (denom > 0)
                        _billAcceptor.ProcessEscrow(denom);
                    else
                        _billAcceptor.Return();
                    break;
                case EVENT_STACKED:
                    byte stackedBillType = message.Length > 3 ? message[3] : (byte)0;
                    int stackedDenom = BillDenominations.TryGetValue(stackedBillType, out int sd) ? sd : 0;
                    _billAcceptor.NotifyStacked(stackedDenom);
                    break;
                case EVENT_JAMMED:
                    _billAcceptor.NotifyJam();
                    break;
                case EVENT_STACKER_FULL:
                    _billAcceptor.NotifyStackerFull();
                    break;
            }
        }
    }
}
