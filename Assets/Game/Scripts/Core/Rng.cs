using System;

namespace RHMG.Core
{
    public interface IRng
    {
        int Next(int maxExclusive);
        float NextFloat();
    }

    /// <summary>Deterministic xorshift32. Never uses System.Random.</summary>
    public sealed class SeededRng : IRng
    {
        private uint _state;

        public SeededRng(int seed)
        {
            _state = seed == 0 ? 0x9E3779B9u : unchecked((uint)seed);
        }

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException("maxExclusive");
            }

            return (int)(NextUInt() % (uint)maxExclusive);
        }

        public float NextFloat()
        {
            return (NextUInt() >> 8) / 16777216f;
        }

        private uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }
    }
}
