using System;
using Xunit;
using CasinoSlot.Hardware;

namespace CasinoSlot.Tests.Hardware
{
    /// <summary>
    /// Unit tests for BillAcceptor, ID-003 protocol, and PulseController.
    /// </summary>
    public class HardwareTests
    {
        // ============================================================
        // BillAcceptor Tests
        // ============================================================

        [Fact]
        public void BillAcceptor_InitialState_Disabled()
        {
            var acceptor = new BillAcceptor();
            Assert.Equal(BillAcceptorState.Disabled, acceptor.State);
            Assert.False(acceptor.IsEnabled);
        }

        [Fact]
        public void BillAcceptor_Enable_ChangesState()
        {
            var acceptor = new BillAcceptor();
            acceptor.Enable();
            Assert.Equal(BillAcceptorState.Idle, acceptor.State);
            Assert.True(acceptor.IsEnabled);
        }

        [Fact]
        public void BillAcceptor_Disable_ChangesState()
        {
            var acceptor = new BillAcceptor();
            acceptor.Enable();
            acceptor.Disable();
            Assert.Equal(BillAcceptorState.Disabled, acceptor.State);
        }

        [Fact]
        public void BillAcceptor_ProcessEscrow_AcceptedDenomination_Stacks()
        {
            var acceptor = new BillAcceptor(new[] { 100, 500, 1000 });
            acceptor.Enable();
            bool accepted = false;
            acceptor.BillAccepted += (s, e) => accepted = true;

            acceptor.ProcessEscrow(100);
            acceptor.NotifyStacked(100);

            Assert.True(accepted);
        }

        [Fact]
        public void BillAcceptor_ProcessEscrow_RejectedDenomination_Rejects()
        {
            var acceptor = new BillAcceptor(new[] { 100 });
            acceptor.Enable();
            bool rejected = false;
            acceptor.BillRejected += (s, e) => rejected = true;

            acceptor.ProcessEscrow(200); // Not in accepted list

            Assert.True(rejected);
        }

        [Fact]
        public void BillAcceptor_ProcessEscrow_WhileDisabled_Returns()
        {
            var acceptor = new BillAcceptor(new[] { 100 });
            // Not enabled
            bool accepted = false;
            acceptor.BillAccepted += (s, e) => accepted = true;
            acceptor.ProcessEscrow(100);
            Assert.False(accepted);
        }

        [Fact]
        public void BillAcceptor_NotifyJam_SetsJamState()
        {
            var acceptor = new BillAcceptor();
            acceptor.Enable();
            bool errorFired = false;
            acceptor.ErrorOccurred += (s, e) => errorFired = true;
            acceptor.NotifyJam();
            Assert.Equal(BillAcceptorState.Jammed, acceptor.State);
            Assert.True(errorFired);
        }

        [Fact]
        public void BillAcceptor_NotifyStackerFull_SetsState()
        {
            var acceptor = new BillAcceptor();
            acceptor.Enable();
            acceptor.NotifyStackerFull();
            Assert.Equal(BillAcceptorState.StackerFull, acceptor.State);
        }

        // ============================================================
        // ID003Protocol Tests
        // ============================================================

        [Fact]
        public void ID003_BuildMessage_ValidFraming()
        {
            byte[] msg = ID003Protocol.BuildMessage(0x41, new byte[] { 0x01 });
            // [STX=0xFC][LEN][CMD=0x41][DATA=0x01][ETX=0xFD][CHK]
            Assert.Equal(0xFC, msg[0]); // STX
            Assert.Equal(0x41, msg[2]); // Command
            Assert.Equal(0xFD, msg[msg.Length - 2]); // ETX
        }

        [Fact]
        public void ID003_ValidateChecksum_ValidMessage_ReturnsTrue()
        {
            byte[] msg = ID003Protocol.BuildMessage(0x11, new byte[0]);
            Assert.True(ID003Protocol.ValidateChecksum(msg));
        }

        [Fact]
        public void ID003_ValidateChecksum_TamperedMessage_ReturnsFalse()
        {
            byte[] msg = ID003Protocol.BuildMessage(0x11, new byte[0]);
            msg[2] ^= 0xFF; // Tamper
            Assert.False(ID003Protocol.ValidateChecksum(msg));
        }

        [Fact]
        public void ID003_ValidateChecksum_NullMessage_ReturnsFalse()
        {
            Assert.False(ID003Protocol.ValidateChecksum(null));
        }

        // ============================================================
        // PulseController Tests
        // ============================================================

        [Fact]
        public void PulseController_InitialState_ZeroCounters()
        {
            var ctrl = new PulseController(1, 1);
            Assert.Equal(0, ctrl.TotalCoinsIn);
            Assert.Equal(0, ctrl.TotalCoinsOut);
        }

        [Fact]
        public void PulseController_CoinIn_RaisesEvent()
        {
            var ctrl = new PulseController(1, 5);
            int receivedCredits = 0;
            ctrl.CoinAccepted += c => receivedCredits = c;
            ctrl.OnCoinInPulse();
            Assert.Equal(5, receivedCredits);
            Assert.Equal(1, ctrl.TotalCoinsIn);
        }

        [Fact]
        public void PulseController_CreditsPerCoin_IsCorrect()
        {
            var ctrl = new PulseController(1, 10);
            Assert.Equal(10, ctrl.CreditsPerCoin);
        }
    }
}
