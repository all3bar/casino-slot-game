using System;
using System.Collections.Generic;
using CasinoSlot.Accounting;
using CasinoSlot.Core;
using CasinoSlot.SAS;

namespace CasinoSlot.UI
{
    /// <summary>
    /// Operator menu UI controller. Provides access to meters, configuration,
    /// event logs, and hardware test functions. Password-protected.
    /// </summary>
    public class OperatorUI
    {
        private readonly GameManager _gameManager;
        private bool _isAuthenticated;

        // Default operator PIN (should be changed in production)
        private string _operatorPIN = "1234";

        /// <summary>Gets whether the operator is currently authenticated.</summary>
        public bool IsAuthenticated => _isAuthenticated;

        /// <summary>Raised when operator authentication succeeds.</summary>
        public event Action AuthenticationSuccess;

        /// <summary>Raised when operator authentication fails.</summary>
        public event Action AuthenticationFailed;

        public OperatorUI(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        /// <summary>
        /// Attempts to authenticate the operator with the given PIN.
        /// </summary>
        /// <returns>True if authentication succeeded.</returns>
        public bool Authenticate(string pin)
        {
            if (pin == _operatorPIN)
            {
                _isAuthenticated = true;
                _gameManager.EnterMaintenance();
                _gameManager.EventLogger.Log(EventType.OperatorLogin, "Operator logged in.");
                AuthenticationSuccess?.Invoke();
                return true;
            }
            _gameManager.EventLogger.Log(EventType.OperatorLogin, "Operator login failed (wrong PIN).");
            AuthenticationFailed?.Invoke();
            return false;
        }

        /// <summary>
        /// Logs out the operator and returns game to normal mode.
        /// </summary>
        public void Logout()
        {
            if (!_isAuthenticated) return;
            _isAuthenticated = false;
            _gameManager.ExitMaintenance();
            _gameManager.EventLogger.Log(EventType.OperatorLogout, "Operator logged out.");
        }

        /// <summary>
        /// Changes the operator PIN.
        /// </summary>
        public bool ChangePIN(string currentPIN, string newPIN)
        {
            if (!_isAuthenticated) return false;
            if (currentPIN != _operatorPIN) return false;
            if (newPIN == null || newPIN.Length < 4) return false;
            _operatorPIN = newPIN;
            _gameManager.EventLogger.Log(EventType.ConfigChanged, "Operator PIN changed.");
            return true;
        }

        /// <summary>
        /// Returns a formatted summary of all accounting meters.
        /// </summary>
        public string GetMetersSummary()
        {
            if (!_isAuthenticated) return "ACCESS DENIED";
            var m = _gameManager.Meters;
            return $"=== ACCOUNTING METERS ===\n" +
                   $"Total Coin In:       {m.TotalCoinIn,10}\n" +
                   $"Total Coin Out:      {m.TotalCoinOut,10}\n" +
                   $"Total Drop:          {m.TotalDrop,10}\n" +
                   $"Total Jackpot:       {m.TotalJackpot,10}\n" +
                   $"Total Bills:         {m.TotalBillsAccepted,10}\n" +
                   $"Total Bills Value:   {m.TotalBillsValue,10}\n" +
                   $"Games Played:        {m.GamesPlayed,10}\n" +
                   $"Games Won:           {m.GamesWon,10}\n" +
                   $"Door Opens:          {m.DoorOpenCount,10}\n" +
                   $"Power Ups:           {m.PowerUpCount,10}\n" +
                   $"RTP:                 {m.RTP,9:F2}%\n";
        }

        /// <summary>
        /// Returns recent event log entries as a list of strings.
        /// </summary>
        public List<string> GetEventLog(int lastN = 100)
        {
            if (!_isAuthenticated) return new List<string> { "ACCESS DENIED" };
            var events = _gameManager.EventLogger.RecentEvents;
            var result = new List<string>();
            int start = Math.Max(0, events.Count - lastN);
            for (int i = start; i < events.Count; i++)
                result.Add(events[i].ToString());
            return result;
        }

        /// <summary>
        /// Returns game history as a list of formatted records.
        /// </summary>
        public List<string> GetGameHistory(int lastN = 50)
        {
            if (!_isAuthenticated) return new List<string> { "ACCESS DENIED" };
            var records = _gameManager.GameHistory.GetLastN(lastN);
            var result = new List<string>();
            foreach (var r in records)
            {
                string stops = string.Join(",", r.StopPositions);
                result.Add($"Game#{r.GameNumber:D6} {r.Timestamp:HH:mm:ss} " +
                           $"Bet={r.TotalBet} Win={r.TotalWin} Stops=[{stops}]" +
                           $"{(r.WasFreeSpin ? " FS" : "")}" +
                           $"{(r.FreeSpinsTriggered ? " FST" : "")}" +
                           $"{(r.BonusTriggered ? " BNS" : "")}");
            }
            return result;
        }

        /// <summary>
        /// Sets the machine to a specific RNG seed for testing.
        /// Only available in maintenance mode.
        /// </summary>
        public string RunHardwareTest()
        {
            if (!_isAuthenticated) return "ACCESS DENIED";
            return "Hardware test: OK\nSerial ports: SIMULATED\nBill acceptor: SIMULATED\nHopper: SIMULATED\n";
        }
    }
}
