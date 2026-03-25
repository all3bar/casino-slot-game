using System;
using System.Collections.Generic;
using CasinoSlot.Accounting;

namespace CasinoSlot.SAS
{
    /// <summary>
    /// Manages SAS exception (real-time event) queue.
    /// Exceptions are queued and sent to the host on the next poll.
    /// </summary>
    public class SASExceptionManager
    {
        private readonly Queue<SASException> _exceptionQueue = new Queue<SASException>();
        private readonly object _lock = new object();
        private const int MaxQueueSize = 100;

        /// <summary>Gets the number of pending exceptions.</summary>
        public int PendingCount { get { lock (_lock) return _exceptionQueue.Count; } }

        /// <summary>
        /// Enqueues a real-time exception to be sent to the SAS host.
        /// </summary>
        public void QueueException(byte exceptionCode)
        {
            lock (_lock)
            {
                if (_exceptionQueue.Count < MaxQueueSize)
                    _exceptionQueue.Enqueue(new SASException(exceptionCode));
            }
        }

        /// <summary>
        /// Dequeues and returns the next pending exception, or null if none.
        /// </summary>
        public SASException DequeueException()
        {
            lock (_lock)
                return _exceptionQueue.Count > 0 ? _exceptionQueue.Dequeue() : null;
        }

        /// <summary>
        /// Peeks at the next exception without removing it.
        /// </summary>
        public SASException PeekException()
        {
            lock (_lock)
                return _exceptionQueue.Count > 0 ? _exceptionQueue.Peek() : null;
        }

        /// <summary>
        /// Clears all pending exceptions.
        /// </summary>
        public void ClearAll()
        {
            lock (_lock)
                _exceptionQueue.Clear();
        }
    }

    /// <summary>
    /// Represents a SAS real-time exception event.
    /// </summary>
    public class SASException
    {
        /// <summary>Gets the exception code byte.</summary>
        public byte Code { get; }

        /// <summary>Gets when the exception was created.</summary>
        public DateTime Timestamp { get; }

        /// <summary>Gets a human-readable description of the exception.</summary>
        public string Description { get; }

        public SASException(byte code)
        {
            Code = code;
            Timestamp = DateTime.UtcNow;
            Description = GetDescription(code);
        }

        private static string GetDescription(byte code) => code switch
        {
            SASExceptionCodes.SlotDoorOpened         => "Slot Door Opened",
            SASExceptionCodes.SlotDoorClosed         => "Slot Door Closed",
            SASExceptionCodes.ACPowerLoss            => "AC Power Loss",
            SASExceptionCodes.ACPowerRestored        => "AC Power Restored",
            SASExceptionCodes.LowBattery             => "Low Battery",
            SASExceptionCodes.CashoutButtonPressed   => "Cashout Button Pressed",
            SASExceptionCodes.GameStarted            => "Game Started",
            SASExceptionCodes.GameEnded              => "Game Ended",
            SASExceptionCodes.GameRecalled           => "Game Recalled",
            SASExceptionCodes.JackpotHandpayPending  => "Jackpot Handpay Pending",
            SASExceptionCodes.HandpayCanceled        => "Handpay Canceled",
            SASExceptionCodes.BillAccepted           => "Bill Accepted",
            SASExceptionCodes.BillRejected           => "Bill Rejected",
            SASExceptionCodes.BillJam                => "Bill Jammed",
            SASExceptionCodes.StackerFull            => "Stacker Full",
            SASExceptionCodes.ProgressiveWin         => "Progressive Win",
            SASExceptionCodes.AFTTransferComplete    => "AFT Transfer Complete",
            SASExceptionCodes.AFTTransferPartial     => "AFT Transfer Partial",
            SASExceptionCodes.AFTTransferFailed      => "AFT Transfer Failed",
            _                                        => $"Unknown Exception 0x{code:X2}"
        };

        public override string ToString()
            => $"SASException[0x{Code:X2}] {Description} at {Timestamp:HH:mm:ss.fff}";
    }
}
