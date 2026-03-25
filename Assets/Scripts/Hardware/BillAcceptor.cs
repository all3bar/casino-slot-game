using System;

namespace CasinoSlot.Hardware
{
    /// <summary>
    /// Represents the current state of the bill acceptor.
    /// </summary>
    public enum BillAcceptorState
    {
        Idle,
        Accepting,
        Stacking,
        Stacked,
        Returning,
        Jammed,
        StackerFull,
        Disabled,
        Error
    }

    /// <summary>
    /// Event args for bill acceptor events.
    /// </summary>
    public class BillEventArgs : EventArgs
    {
        /// <summary>Gets the bill denomination in cents (e.g., 100 = $1.00).</summary>
        public int DenominationCents { get; }

        /// <summary>Gets the country code (e.g., "USD").</summary>
        public string CountryCode { get; }

        public BillEventArgs(int denominationCents, string countryCode = "USD")
        {
            DenominationCents = denominationCents;
            CountryCode = countryCode;
        }
    }

    /// <summary>
    /// Interface for bill acceptor hardware communication.
    /// Supports both ID-003 protocol and simulated/mock devices.
    /// </summary>
    public interface IBillAcceptor
    {
        BillAcceptorState State { get; }
        bool IsEnabled { get; }
        event EventHandler<BillEventArgs> BillAccepted;
        event EventHandler<BillEventArgs> BillRejected;
        event EventHandler<string> ErrorOccurred;
        void Enable();
        void Disable();
        void Stack();
        void Return();
    }

    /// <summary>
    /// High-level bill acceptor controller.
    /// Bridges the ID-003 protocol to game logic.
    /// </summary>
    public class BillAcceptor : IBillAcceptor
    {
        private BillAcceptorState _state = BillAcceptorState.Disabled;
        private bool _enabled;
        private readonly int[] _acceptedDenominations;

        /// <summary>Gets the current acceptor state.</summary>
        public BillAcceptorState State => _state;

        /// <summary>Gets whether the acceptor is enabled.</summary>
        public bool IsEnabled => _enabled;

        /// <summary>Raised when a bill is successfully accepted and stacked.</summary>
        public event EventHandler<BillEventArgs> BillAccepted;

        /// <summary>Raised when a bill is rejected.</summary>
        public event EventHandler<BillEventArgs> BillRejected;

        /// <summary>Raised when an error occurs.</summary>
        public event EventHandler<string> ErrorOccurred;

        /// <summary>
        /// Initializes the BillAcceptor.
        /// </summary>
        /// <param name="acceptedDenominations">
        /// Array of bill denominations to accept (in cents, e.g., 100, 500, 1000, 2000, 5000, 10000).
        /// </param>
        public BillAcceptor(int[] acceptedDenominations = null)
        {
            _acceptedDenominations = acceptedDenominations
                ?? new[] { 100, 500, 1000, 2000, 5000, 10000 };
        }

        /// <summary>Enables the bill acceptor.</summary>
        public void Enable()
        {
            _enabled = true;
            _state = BillAcceptorState.Idle;
        }

        /// <summary>Disables the bill acceptor.</summary>
        public void Disable()
        {
            _enabled = false;
            _state = BillAcceptorState.Disabled;
        }

        /// <summary>
        /// Called when the ID-003 protocol reports a bill is in escrow.
        /// Validates denomination and stacks or returns.
        /// </summary>
        public void ProcessEscrow(int denominationCents, string countryCode = "USD")
        {
            if (!_enabled)
            {
                Return();
                return;
            }

            _state = BillAcceptorState.Accepting;

            bool accepted = Array.IndexOf(_acceptedDenominations, denominationCents) >= 0;
            if (accepted)
                Stack();
            else
            {
                Return();
                BillRejected?.Invoke(this, new BillEventArgs(denominationCents, countryCode));
            }
        }

        /// <summary>Instructs the acceptor to stack the bill in escrow.</summary>
        public void Stack()
        {
            _state = BillAcceptorState.Stacking;
            // In real implementation, send stack command to hardware
            // Simulate immediate success:
            _state = BillAcceptorState.Stacked;
        }

        /// <summary>Instructs the acceptor to return the bill in escrow.</summary>
        public void Return()
        {
            _state = BillAcceptorState.Returning;
            // In real implementation, send return command to hardware
            _state = BillAcceptorState.Idle;
        }

        /// <summary>
        /// Called by ID-003 protocol handler when a bill has been stacked.
        /// </summary>
        public void NotifyStacked(int denominationCents, string countryCode = "USD")
        {
            _state = BillAcceptorState.Idle;
            BillAccepted?.Invoke(this, new BillEventArgs(denominationCents, countryCode));
        }

        /// <summary>Called when a jam is detected.</summary>
        public void NotifyJam()
        {
            _state = BillAcceptorState.Jammed;
            ErrorOccurred?.Invoke(this, "Bill jam detected");
        }

        /// <summary>Called when stacker is full.</summary>
        public void NotifyStackerFull()
        {
            _state = BillAcceptorState.StackerFull;
            ErrorOccurred?.Invoke(this, "Stacker full");
        }
    }
}
