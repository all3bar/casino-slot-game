using System;
using System.Collections.Generic;

namespace CasinoSlot.Accounting
{
    /// <summary>
    /// Defines the types of events logged for auditing.
    /// </summary>
    public enum EventType
    {
        GameStartup,
        GameShutdown,
        SpinStarted,
        GameEnded,
        Cashout,
        BillAccepted,
        BillRejected,
        DoorOpen,
        DoorClose,
        JackpotPending,
        JackpotPaid,
        FreeSpinsStarted,
        FreeSpinsEnded,
        BonusTriggered,
        BonusCompleted,
        OperatorLogin,
        OperatorLogout,
        SASCommand,
        SASException,
        HardwareError,
        ConfigChanged,
        MemoryError,
        PowerUp,
        PowerDown
    }

    /// <summary>
    /// Represents a single logged event entry.
    /// </summary>
    public class GameEvent
    {
        /// <summary>Gets the unique event sequence number.</summary>
        public long SequenceNumber { get; }

        /// <summary>Gets the UTC timestamp of the event.</summary>
        public DateTime Timestamp { get; }

        /// <summary>Gets the event type.</summary>
        public EventType Type { get; }

        /// <summary>Gets the event description/details.</summary>
        public string Description { get; }

        public GameEvent(long seqNo, EventType type, string description)
        {
            SequenceNumber = seqNo;
            Timestamp = DateTime.UtcNow;
            Type = type;
            Description = description;
        }

        public override string ToString()
            => $"[{SequenceNumber:D8}] {Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Type}: {Description}";
    }

    /// <summary>
    /// Thread-safe event logger for full audit trail.
    /// Stores events in memory and provides file output.
    /// </summary>
    public class EventLogger
    {
        private readonly List<GameEvent> _events = new List<GameEvent>();
        private long _sequenceCounter;
        private readonly object _lock = new object();
        private const int MaxInMemoryEvents = 10000;

        /// <summary>Gets a read-only snapshot of recent events.</summary>
        public IReadOnlyList<GameEvent> RecentEvents
        {
            get
            {
                lock (_lock)
                    return new List<GameEvent>(_events);
            }
        }

        /// <summary>
        /// Logs a new event.
        /// </summary>
        /// <param name="type">Event type.</param>
        /// <param name="description">Human-readable description.</param>
        public void Log(EventType type, string description)
        {
            lock (_lock)
            {
                _sequenceCounter++;
                var evt = new GameEvent(_sequenceCounter, type, description);
                _events.Add(evt);

                // Circular buffer: remove oldest when over limit
                if (_events.Count > MaxInMemoryEvents)
                    _events.RemoveAt(0);
            }
        }

        /// <summary>
        /// Returns events of a specific type.
        /// </summary>
        public List<GameEvent> GetEventsByType(EventType type)
        {
            lock (_lock)
                return _events.FindAll(e => e.Type == type);
        }

        /// <summary>
        /// Returns events in a specified time range.
        /// </summary>
        public List<GameEvent> GetEventsByTimeRange(DateTime from, DateTime to)
        {
            lock (_lock)
                return _events.FindAll(e => e.Timestamp >= from && e.Timestamp <= to);
        }

        /// <summary>
        /// Serializes event log to a list of string lines.
        /// </summary>
        public List<string> Serialize()
        {
            lock (_lock)
            {
                var lines = new List<string>();
                foreach (var e in _events)
                    lines.Add(e.ToString());
                return lines;
            }
        }
    }
}
