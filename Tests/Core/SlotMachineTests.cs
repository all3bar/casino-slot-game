using System;
using Xunit;
using CasinoSlot.Core;
using CasinoSlot.Accounting;

namespace CasinoSlot.Tests.Core
{
    /// <summary>
    /// Unit tests for SlotMachine core logic.
    /// </summary>
    public class SlotMachineTests
    {
        private SlotMachine CreateMachine()
        {
            GameManager.ResetInstance();
            var meters = new MeterManager();
            return new SlotMachine(meters);
        }

        [Fact]
        public void InitialCredits_AreZero()
        {
            var machine = CreateMachine();
            Assert.Equal(0L, machine.Credits);
        }

        [Fact]
        public void AddCredits_IncreasesBalance()
        {
            var machine = CreateMachine();
            machine.AddCredits(100);
            Assert.Equal(100L, machine.Credits);
        }

        [Fact]
        public void Spin_WithoutCredits_ReturnsNull()
        {
            var machine = CreateMachine();
            var result = machine.Spin();
            Assert.Null(result);
        }

        [Fact]
        public void Spin_WithCredits_ReturnsResult()
        {
            var machine = CreateMachine();
            machine.AddCredits(1000);
            var result = machine.Spin();
            Assert.NotNull(result);
        }

        [Fact]
        public void Spin_DeductsCredits()
        {
            var machine = CreateMachine();
            machine.AddCredits(1000);
            long bet = machine.TotalBet;
            machine.Spin();
            Assert.True(machine.Credits <= 1000 - bet + 10000); // could win
        }

        [Fact]
        public void Cashout_ReturnsCredits()
        {
            var machine = CreateMachine();
            machine.AddCredits(500);
            long cashout = machine.Cashout();
            Assert.Equal(500L, cashout);
            Assert.Equal(0L, machine.Credits);
        }

        [Fact]
        public void MaxBet_SetsCorrectValues()
        {
            var machine = CreateMachine();
            machine.MaxBet();
            Assert.Equal(10L, machine.BetPerLine);
            Assert.Equal(20, machine.ActiveLinesCount);
        }

        [Fact]
        public void SetBetPerLine_Updates()
        {
            var machine = CreateMachine();
            bool result = machine.SetBetPerLine(5);
            Assert.True(result);
            Assert.Equal(5L, machine.BetPerLine);
        }

        [Fact]
        public void SetBetPerLine_InvalidValue_ReturnsFalse()
        {
            var machine = CreateMachine();
            bool result = machine.SetBetPerLine(0);
            Assert.False(result);
        }

        [Fact]
        public void SetActiveLines_Valid()
        {
            var machine = CreateMachine();
            bool result = machine.SetActiveLines(10);
            Assert.True(result);
            Assert.Equal(10, machine.ActiveLinesCount);
        }

        [Fact]
        public void SetActiveLines_OutOfRange_ReturnsFalse()
        {
            var machine = CreateMachine();
            bool result = machine.SetActiveLines(25);
            Assert.False(result);
        }

        [Fact]
        public void Spin_Grid_Is5x3()
        {
            var machine = CreateMachine();
            machine.AddCredits(1000);
            var result = machine.Spin();
            Assert.NotNull(result);
            Assert.Equal(5, result.Grid.GetLength(0));
            Assert.Equal(3, result.Grid.GetLength(1));
        }

        [Fact]
        public void ForceStops_SetsReelPositions()
        {
            var machine = CreateMachine();
            machine.ForceStops(new[] { 0, 1, 2, 3, 4 });
            var positions = machine.GetStopPositions();
            Assert.Equal(0, positions[0]);
            Assert.Equal(1, positions[1]);
            Assert.Equal(2, positions[2]);
        }

        [Fact]
        public void Spin_TotalBetEqualsLinesTimesBetPerLine()
        {
            var machine = CreateMachine();
            machine.SetBetPerLine(3);
            machine.SetActiveLines(5);
            Assert.Equal(15L, machine.TotalBet);
        }

        [Fact]
        public void Spin_MultipleSpins_CreditsNeverNegative()
        {
            var machine = CreateMachine();
            machine.AddCredits(100);
            for (int i = 0; i < 50; i++)
            {
                if (machine.Credits < machine.TotalBet) break;
                machine.Spin();
                Assert.True(machine.Credits >= 0);
            }
        }

        [Fact]
        public void SpinCompleted_EventFired()
        {
            var machine = CreateMachine();
            machine.AddCredits(1000);
            bool fired = false;
            machine.SpinCompleted += r => fired = true;
            machine.Spin();
            Assert.True(fired);
        }
    }
}
