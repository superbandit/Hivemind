namespace Hivemind.Models;

/// <summary>
/// 2D vector used for positions and velocities.
/// Angle convention: 0° = North (up), positive = clockwise, Y-down (screen coords).
/// This matches the Gearbots game engine convention.
/// </summary>
public readonly record struct Vec2(double X, double Y)
{
    public static Vec2 Zero => new(0, 0);

    public double Length => Math.Sqrt(X * X + Y * Y);

    public double DistanceTo(Vec2 other) => (this - other).Length;

    /// <summary>
    /// Returns the angle in degrees from this point toward <paramref name="target"/>.
    /// 0° = North (screen up), 90° = East, 180° = South, -90° = West.
    /// </summary>
    public double AngleTo(Vec2 target)
    {
        var delta = target - this;
        return NormalizeAngle(Math.Atan2(delta.X, -delta.Y) * (180.0 / Math.PI));
    }

    /// <summary>Wraps an angle to the range (-180, 180].</summary>
    public static double NormalizeAngle(double degrees)
    {
        degrees %= 360;
        if (degrees > 180) degrees -= 360;
        if (degrees < -180) degrees += 360;
        return degrees;
    }

    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2 operator *(Vec2 v, double s) => new(v.X * s, v.Y * s);
    public static Vec2 operator *(double s, Vec2 v) => new(v.X * s, v.Y * s);

    public override string ToString() => $"({X:F1}, {Y:F1})";
}
