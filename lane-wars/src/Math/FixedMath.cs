namespace LaneWars.Math;

/// <summary>
/// Common helper functions for Fixed-point arithmetic.
/// Keeps the Fixed struct itself lean while providing the usual math utilities.
/// </summary>
public static class FixedMath
{
    /// <summary>Return the absolute value of <paramref name="value"/>.</summary>
    public static Fixed Abs(Fixed value)
    {
        return value < Fixed.Zero ? -value : value;
    }

    /// <summary>Return the smaller of two Fixed values.</summary>
    public static Fixed Min(Fixed a, Fixed b)
    {
        return a < b ? a : b;
    }

    /// <summary>Return the larger of two Fixed values.</summary>
    public static Fixed Max(Fixed a, Fixed b)
    {
        return a > b ? a : b;
    }
}
