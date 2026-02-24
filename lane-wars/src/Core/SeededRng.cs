namespace LaneWars.Core;

using System;

/// <summary>
/// Deterministic xoshiro256** PRNG.
/// State is four ulong fields, initialized from a single seed via SplitMix64.
/// Same seed always produces the same sequence — required for lockstep simulation.
/// </summary>
public class SeededRng
{
    private ulong s0;
    private ulong s1;
    private ulong s2;
    private ulong s3;

    /// <summary>Create a new PRNG seeded deterministically from <paramref name="seed"/>.</summary>
    public SeededRng(ulong seed)
    {
        // Initialise the four state words via SplitMix64 (as recommended by the
        // xoshiro authors) to spread a single seed across the full state space.
        s0 = SplitMix64(ref seed);
        s1 = SplitMix64(ref seed);
        s2 = SplitMix64(ref seed);
        s3 = SplitMix64(ref seed);
    }

    // --- SplitMix64 helper (used only for seeding) ---

    private static ulong SplitMix64(ref ulong state)
    {
        ulong z = (state += 0x9E3779B97F4A7C15UL);
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    // --- Core generator: xoshiro256** ---

    /// <summary>Return the next 64-bit pseudorandom value and advance state.</summary>
    public ulong NextULong()
    {
        // result = rotl(s1 * 5, 7) * 9
        ulong result = RotateLeft(s1 * 5, 7) * 9;

        ulong t = s1 << 17;

        s2 ^= s0;
        s3 ^= s1;
        s1 ^= s2;
        s0 ^= s3;

        s2 ^= t;
        s3 = RotateLeft(s3, 45);

        return result;
    }

    /// <summary>
    /// Return a uniformly distributed int in [min, maxExclusive).
    /// Uses rejection sampling on a power-of-two range to eliminate modulo bias,
    /// falling back to simple modulo for non-power-of-two ranges.
    /// </summary>
    public int Next(int min, int maxExclusive)
    {
        if (maxExclusive <= min)
            throw new ArgumentException($"maxExclusive ({maxExclusive}) must be greater than min ({min}).");

        uint range = (uint)(maxExclusive - min);

        if (range == 1)
            return min;

        // Rejection sampling to remove modulo bias.
        // threshold is the largest multiple of range that fits in a uint.
        uint threshold = (uint)(-((int)range)) % range; // equivalent to (2^32 - range) % range

        uint sample;
        do
        {
            sample = (uint)(NextULong() >> 32);
        }
        while (sample < threshold);

        return min + (int)(sample % range);
    }

    // --- Utility ---

    private static ulong RotateLeft(ulong value, int count)
    {
        return (value << count) | (value >> (64 - count));
    }

    // --- Serialisation helpers (for save/replay) ---

    public (ulong, ulong, ulong, ulong) GetState() => (s0, s1, s2, s3);

    public void SetState(ulong state0, ulong state1, ulong state2, ulong state3)
    {
        s0 = state0;
        s1 = state1;
        s2 = state2;
        s3 = state3;
    }
}
