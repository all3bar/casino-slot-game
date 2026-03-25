using System;
using System.IO.Ports;

namespace CasinoSlot.Hardware
{
    /// <summary>
    /// Manages RS-232 serial port communication for hardware peripherals.
    /// Wraps System.IO.Ports.SerialPort with error handling and event notification.
    /// </summary>
    public class SerialCommunication : IDisposable
    {
        private SerialPort _port;
        private bool _disposed;

        /// <summary>Gets the current port name.</summary>
        public string PortName { get; private set; }

        /// <summary>Gets whether the port is currently open.</summary>
        public bool IsOpen => _port?.IsOpen ?? false;

        /// <summary>Raised when data is received on the serial port.</summary>
        public event Action<byte[]> DataReceived;

        /// <summary>Raised when a communication error occurs.</summary>
        public event Action<string> ErrorOccurred;

        /// <summary>
        /// Opens a serial port connection.
        /// </summary>
        /// <param name="portName">COM port name (e.g., "COM1").</param>
        /// <param name="baudRate">Baud rate (default 9600 for SAS/ID-003).</param>
        /// <param name="parity">Parity setting.</param>
        /// <param name="dataBits">Data bits.</param>
        /// <param name="stopBits">Stop bits.</param>
        public bool Open(string portName, int baudRate = 9600,
            Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            try
            {
                PortName = portName;
                _port = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
                {
                    ReadTimeout  = 500,
                    WriteTimeout = 500,
                    ReceivedBytesThreshold = 1
                };
                _port.DataReceived += OnDataReceived;
                _port.ErrorReceived += OnErrorReceived;
                _port.Open();
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to open {portName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Closes the serial port.
        /// </summary>
        public void Close()
        {
            if (_port?.IsOpen == true)
            {
                _port.DataReceived -= OnDataReceived;
                _port.ErrorReceived -= OnErrorReceived;
                _port.Close();
            }
        }

        /// <summary>
        /// Sends bytes to the serial port.
        /// </summary>
        public bool Send(byte[] data)
        {
            if (!IsOpen || data == null || data.Length == 0) return false;
            try
            {
                _port.Write(data, 0, data.Length);
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Send error on {PortName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reads available bytes from the port synchronously.
        /// </summary>
        public byte[] Read(int maxBytes = 256)
        {
            if (!IsOpen) return null;
            try
            {
                int available = _port.BytesToRead;
                if (available == 0) return null;
                int count = Math.Min(available, maxBytes);
                byte[] buffer = new byte[count];
                _port.Read(buffer, 0, count);
                return buffer;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Read error on {PortName}: {ex.Message}");
                return null;
            }
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] data = Read();
            if (data != null)
                DataReceived?.Invoke(data);
        }

        private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            ErrorOccurred?.Invoke($"Serial error on {PortName}: {e.EventType}");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Close();
                _port?.Dispose();
                _disposed = true;
            }
        }
    }
}
