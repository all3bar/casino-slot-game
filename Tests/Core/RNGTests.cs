using System;
using Xunit;
using CasinoSlot.Core;

namespace CasinoSlot.Tests.Core
{
    /// <summary>
    /// Unit tests for the RNG class.
    /// </summary>
    public class RNGTests
    {
        [Fact]
        public void Constructor_GeneratesValidSeed()
        {
            var rng = new RNG();
            // Seed should be set
            Assert.NotEqual(0L, rng.Seed);
        }

        [Fact]
        public void SeededConstructor_UsesSeed()
        {
            var rng1 = new RNG(12345L);
            var rng2 = new RNG(12345L);
            Assert.Equal(rng1.Seed, rng2.Seed);
        }

        [Fact]
        public void Next_ReturnsValueInRange()
        {
            var rng = new RNG(42L);
            for (int i = 0; i < 1000; i++)
            {
                int val = rng.Next(10);
                Assert.InRange(val, 0, 9);
            }
        }

        [Fact]
        public void NextRange_ReturnsValueInRange()
        {
            var rng = new RNG(99L);
            for (int i = 0; i < 1000; i++)
            {
                int val = rng.Next(5, 10);
                Assert.InRange(val, 5, 9);
            }
        }

        [Fact]
        public void NextDouble_ReturnsValueInRange()
        {
            var rng = new RNG(7L);
            for (int i = 0; i < 1000; i++)
            {
                double val = rng.NextDouble();
                Assert.InRange(val, 0.0, 1.0);
            }
        }

        [Fact]
        public void CallCount_Increments()
        {
            var rng = new RNG(1L);
            Assert.Equal(0UL, rng.CallCount);
            rng.Next(10);
            Assert.Equal(1UL, rng.CallCount);
            rng.Next(5, 10);
            Assert.Equal(2UL, rng.CallCount);
            rng.NextDouble();
            Assert.Equal(3UL, rng.CallCount);
        }

        [Fact]
        public void NextReelStop_ReturnsValidIndex()
        {
            var rng = new RNG(0L);
            for (int i = 0; i < 100; i++)
            {
                int stop = rng.NextReelStop(32);
                Assert.InRange(stop, 0, 31);
            }
        }

        [Fact]
        public void Reseed_ChangesCallCount()
        {
            var rng = new RNG(10L);
            rng.Next(5);
            rng.Next(5);
            rng.Reseed();
            Assert.Equal(0UL, rng.CallCount);
        }

        [Fact]
        public void SameSeeds_ProduceSameSequence()
        {
            var rng1 = new RNG(999L);
            var rng2 = new RNG(999L);
            for (int i = 0; i < 20; i++)
                Assert.Equal(rng1.Next(100), rng2.Next(100));
        }
    }
}
