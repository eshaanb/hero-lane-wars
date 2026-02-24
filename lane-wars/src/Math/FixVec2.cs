namespace LaneWars.Math;

/// <summary>
/// 2D vector composed of two Fixed (Q16.16) components.
/// All positional and directional math in the simulation uses this type.
/// </summary>
public readonly struct FixVec2
{
    public readonly Fixed X;
    public readonly Fixed Y;

    public FixVec2(Fixed x, Fixed y)
    {
        X = x;
        Y = y;
    }

    // --- Constants ---

    public static readonly FixVec2 Zero = new FixVec2(Fixed.Zero, Fixed.Zero);

    // --- Static helpers ---

    public static FixVec2 Add(FixVec2 a, FixVec2 b)
    {
        return new FixVec2(a.X + b.X, a.Y + b.Y);
    }

    public static FixVec2 Sub(FixVec2 a, FixVec2 b)
    {
        return new FixVec2(a.X - b.X, a.Y - b.Y);
    }

    /// <summary>Uniform scale: multiply both components by a scalar.</summary>
    public static FixVec2 Scale(FixVec2 v, Fixed scalar)
    {
        return new FixVec2(v.X * scalar, v.Y * scalar);
    }

    /// <summary>
    /// Squared distance between two points.
    /// Avoids a square-root (which is expensive in fixed-point).
    /// Returns dx*dx + dy*dy as a Fixed.
    /// </summary>
    public static Fixed DistanceSquared(FixVec2 a, FixVec2 b)
    {
        Fixed dx = a.X - b.X;
        Fixed dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    // --- Operator overloads ---

    public static FixVec2 operator +(FixVec2 a, FixVec2 b) => Add(a, b);

    public static FixVec2 operator -(FixVec2 a, FixVec2 b) => Sub(a, b);

    public static FixVec2 operator -(FixVec2 v) => new FixVec2(-v.X, -v.Y);

    // --- Equality ---

    public static bool operator ==(FixVec2 a, FixVec2 b) => a.X == b.X && a.Y == b.Y;

    public static bool operator !=(FixVec2 a, FixVec2 b) => !(a == b);

    public bool Equals(FixVec2 other) => X == other.X && Y == other.Y;

    public override bool Equals(object obj) => obj is FixVec2 other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (X.GetHashCode() * 397) ^ Y.GetHashCode();
        }
    }

    public override string ToString() => $"({X}, {Y})";
}
