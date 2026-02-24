namespace LaneWars.Math;

using System;

/// <summary>
/// Q16.16 fixed-point number. 16 bits integer, 16 bits fraction.
/// Raw value is a 32-bit int where the lower 16 bits are the fractional part.
/// All simulation math flows through this type — no floats allowed.
/// </summary>
public readonly struct Fixed : IEquatable<Fixed>, IComparable<Fixed>
{
    public const int SHIFT = 16;
    public const int ONE_RAW = 1 << SHIFT; // 65536

    public readonly int Raw;

    // --- Factory methods (no implicit float conversions) ---

    private Fixed(int raw)
    {
        Raw = raw;
    }

    /// <summary>Create a Fixed from a raw Q16.16 value.</summary>
    public static Fixed FromRaw(int raw) => new Fixed(raw);

    /// <summary>Create a Fixed from a whole integer (shifts it into Q16.16).</summary>
    public static Fixed FromInt(int value) => new Fixed(value << SHIFT);

    // --- Constants ---

    public static readonly Fixed Zero = new Fixed(0);
    public static readonly Fixed One  = new Fixed(ONE_RAW);
    public static readonly Fixed Half = new Fixed(ONE_RAW >> 1);

    // --- Conversions ---

    /// <summary>Truncate toward zero, returning the integer part.</summary>
    public int ToInt()
    {
        if (Raw >= 0)
            return Raw >> SHIFT;

        // For negative values, shift rounds toward negative infinity.
        // Truncate toward zero instead: add (ONE_RAW - 1) before shifting.
        return (Raw + (ONE_RAW - 1)) >> SHIFT;
    }

    // --- Arithmetic operators ---

    public static Fixed operator +(Fixed a, Fixed b) => new Fixed(a.Raw + b.Raw);

    public static Fixed operator -(Fixed a, Fixed b) => new Fixed(a.Raw - b.Raw);

    public static Fixed operator -(Fixed a) => new Fixed(-a.Raw);

    /// <summary>
    /// Multiply two Q16.16 values. Uses a long intermediate to avoid overflow.
    /// (a * b) >> 16
    /// </summary>
    public static Fixed operator *(Fixed a, Fixed b)
    {
        long product = (long)a.Raw * b.Raw;
        return new Fixed((int)(product >> SHIFT));
    }

    /// <summary>
    /// Divide two Q16.16 values. Uses a long intermediate to preserve precision.
    /// (a << 16) / b
    /// </summary>
    public static Fixed operator /(Fixed a, Fixed b)
    {
        long numerator = (long)a.Raw << SHIFT;
        return new Fixed((int)(numerator / b.Raw));
    }

    // --- Comparison operators ---

    public static bool operator ==(Fixed a, Fixed b) => a.Raw == b.Raw;
    public static bool operator !=(Fixed a, Fixed b) => a.Raw != b.Raw;
    public static bool operator < (Fixed a, Fixed b) => a.Raw <  b.Raw;
    public static bool operator > (Fixed a, Fixed b) => a.Raw >  b.Raw;
    public static bool operator <=(Fixed a, Fixed b) => a.Raw <= b.Raw;
    public static bool operator >=(Fixed a, Fixed b) => a.Raw >= b.Raw;

    // --- IEquatable<Fixed> ---

    public bool Equals(Fixed other) => Raw == other.Raw;

    public override bool Equals(object obj) => obj is Fixed other && Equals(other);

    public override int GetHashCode() => Raw;

    // --- IComparable<Fixed> ---

    public int CompareTo(Fixed other) => Raw.CompareTo(other.Raw);

    // --- Debug ---

    public override string ToString()
    {
        // Show as a decimal for readability: integer part + fractional part.
        int intPart = Raw >> SHIFT;
        int fracRaw = Raw & (ONE_RAW - 1);

        if (Raw < 0 && fracRaw != 0)
        {
            // For negative values with a fractional part, adjust display.
            // Raw = intPart * ONE_RAW + fracRaw  (where fracRaw is always positive from masking)
            // We want to display e.g. -1.5 not -2.5
            intPart += 1;
            fracRaw = ONE_RAW - fracRaw;
        }

        // Convert fractional bits to a 4-digit decimal.
        // fracRaw / 65536 * 10000 = fracRaw * 10000 / 65536
        int fracDecimal = (int)((long)fracRaw * 10000 / ONE_RAW);
        string sign = (Raw < 0 && intPart == 0) ? "-" : "";
        return $"{sign}{intPart}.{fracDecimal:D4}";
    }
}
