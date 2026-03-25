using System;
using System.Security.Cryptography;

namespace CasinoSlot.Core
{
    /// <summary>
    /// Certifiable Random Number Generator using cryptographic seed.
    /// Supports controlled seeding for audit/replay purposes.
    /// </summary>
    public class RNG
    {
        private Random _random;
        private long _seed;
        private readonly object _lock = new object();
        private ulong _callCount;

        /// <summary>Gets the seed used to initialize this RNG instance.</summary>
        public long Seed => _seed;

        /// <summary>Gets total number of random numbers generated (for audit trail).</summary>
        public ulong CallCount => _callCount;

        /// <summary>
        /// Initializes the RNG with a cryptographically generated seed.
        /// </summary>
        public RNG()
        {
            _seed = GenerateCryptographicSeed();
            _random = new Random((int)(_seed & 0x7FFFFFFF));
            _callCount = 0;
        }

        /// <summary>
        /// Initializes the RNG with a specific seed (for replay/testing).
        /// </summary>
        /// <param name="seed">The seed value to use.</param>
        public RNG(long seed)
        {
            _seed = seed;
            _random = new Random((int)(seed & 0x7FFFFFFF));
            _callCount = 0;
        }

        /// <summary>
        /// Re-seeds the RNG with a new cryptographic seed.
        /// </summary>
        public void Reseed()
        {
            lock (_lock)
            {
                _seed = GenerateCryptographicSeed();
                _random = new Random((int)(_seed & 0x7FFFFFFF));
                _callCount = 0;
            }
        }

        /// <summary>
        /// Returns a non-negative random integer in [0, maxValue).
        /// </summary>
        public int Next(int maxValue)
        {
            lock (_lock)
            {
                _callCount++;
                return _random.Next(maxValue);
            }
        }

        /// <summary>
        /// Returns a random integer in [minValue, maxValue).
        /// </summary>
        public int Next(int minValue, int maxValue)
        {
            lock (_lock)
            {
                _callCount++;
                return _random.Next(minValue, maxValue);
            }
        }

        /// <summary>
        /// Returns a random double in [0.0, 1.0).
        /// </summary>
        public double NextDouble()
        {
            lock (_lock)
            {
                _callCount++;
                return _random.NextDouble();
            }
        }

        /// <summary>
        /// Returns a random stop position for a reel (0 to stripLength-1).
        /// </summary>
        /// <param name="stripLength">Number of positions on the reel strip.</param>
        public int NextReelStop(int stripLength)
        {
            return Next(stripLength);
        }

        private static long GenerateCryptographicSeed()
        {
            byte[] bytes = new byte[8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return BitConverter.ToInt64(bytes, 0);
        }
    }
}
