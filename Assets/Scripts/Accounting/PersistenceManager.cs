using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CasinoSlot.Accounting
{
    /// <summary>
    /// Handles secure persistence of game meters and event logs to disk.
    /// Uses SHA-256 checksums to detect data tampering.
    /// </summary>
    public class PersistenceManager
    {
        private readonly MeterManager _meters;
        private readonly EventLogger _eventLogger;
        private readonly string _dataPath;
        private readonly string _metersFile;
        private readonly string _logsFile;

        /// <summary>
        /// Initializes the PersistenceManager.
        /// </summary>
        /// <param name="meters">The meter manager to persist.</param>
        /// <param name="eventLogger">The event logger to persist.</param>
        /// <param name="dataPath">Directory path for data files (defaults to AppData).</param>
        public PersistenceManager(MeterManager meters, EventLogger eventLogger, string dataPath = null)
        {
            _meters = meters;
            _eventLogger = eventLogger;
            _dataPath = dataPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CasinoSlotGame");
            _metersFile = Path.Combine(_dataPath, "meters.json");
            _logsFile = Path.Combine(_dataPath, "eventlog.txt");
        }

        /// <summary>
        /// Saves current meter state and event log to disk.
        /// </summary>
        public bool Save()
        {
            try
            {
                Directory.CreateDirectory(_dataPath);

                // Serialize meters
                var meterData = _meters.Serialize();
                var wrapper = new MeterFileWrapper
                {
                    Version = 1,
                    SavedAt = DateTime.UtcNow.ToString("O"),
                    Meters = meterData
                };

                string json = JsonSerializer.Serialize(wrapper, new JsonSerializerOptions { WriteIndented = true });
                string checksum = ComputeChecksum(json);
                string finalContent = json + "\n" + checksum;

                File.WriteAllText(_metersFile, finalContent, Encoding.UTF8);

                // Append new events to log
                var logLines = _eventLogger.Serialize();
                File.WriteAllLines(_logsFile, logLines, Encoding.UTF8);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Loads persisted meter state from disk. Returns false if file is missing or checksum fails.
        /// </summary>
        public bool Load()
        {
            if (!File.Exists(_metersFile)) return false;

            try
            {
                string fileContent = File.ReadAllText(_metersFile, Encoding.UTF8).TrimEnd();

                // Split out checksum (last line)
                int lastNewline = fileContent.LastIndexOf('\n');
                if (lastNewline < 0) return false;

                string json = fileContent.Substring(0, lastNewline).TrimEnd();
                string storedChecksum = fileContent.Substring(lastNewline + 1).Trim();
                string computedChecksum = ComputeChecksum(json);

                if (storedChecksum != computedChecksum)
                {
                    _eventLogger.Log(EventType.MemoryError, "Meter file checksum mismatch - data may be corrupted.");
                    return false;
                }

                var wrapper = JsonSerializer.Deserialize<MeterFileWrapper>(json);
                if (wrapper?.Meters != null)
                    _meters.Deserialize(wrapper.Meters);

                return true;
            }
            catch (Exception ex)
            {
                _eventLogger.Log(EventType.MemoryError, $"Failed to load meters: {ex.Message}");
                return false;
            }
        }

        private static string ComputeChecksum(string content)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(content));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private class MeterFileWrapper
        {
            public int Version { get; set; }
            public string SavedAt { get; set; }
            public Dictionary<string, long> Meters { get; set; }
        }
    }
}
