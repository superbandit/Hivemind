using Hivemind.Models;

namespace Hivemind.Tracking;

/// <summary>
/// Tracks last-known positions of friendly tanks for friendly-fire prevention.
/// Updated whenever friendlies appear in the scan cone.
/// Provides a geometric bullet-trajectory check that works even when
/// friendlies are no longer in the current scan cone.
/// </summary>
public sealed class FriendlyTracker
{
    /// <summary>Combined collision radius: tank (20) + bullet (5).</summary>
    private const double CollisionRadius = 25.0;

    /// <summary>Ignore tracked positions older than this — they've likely moved.</summary>
    private const int MaxStaleness = 75;

    private readonly Dictionary<int, (Vec2 Position, int Step)> _friendlies = [];

    /// <summary>
    /// Record the position of any friendly tank visible in the scan cone.
    /// </summary>
    public void Update(int step, IEnumerable<ScannedTank> scans)
    {
        foreach (var scan in scans)
        {
            if (scan.IsEnemy)
                continue;

            _friendlies[scan.TankId] = (scan.Location, step);
        }
    }

    /// <summary>
    /// Returns true if the bullet trajectory from <paramref name="shooterPos"/> toward
    /// <paramref name="aimPoint"/> passes within collision distance of any tracked friendly
    /// that is closer than <paramref name="maxRange"/>.
    /// </summary>
    public bool IsUnsafeToFire(Vec2 shooterPos, Vec2 aimPoint, double maxRange, int currentStep)
    {
        foreach (var (_, (position, step)) in _friendlies)
        {
            if (currentStep - step > MaxStaleness)
                continue;

            var friendlyDist = shooterPos.DistanceTo(position);
            if (friendlyDist > maxRange)
                continue;

            var distToLine = PointToSegmentDistance(shooterPos, aimPoint, position);
            if (distToLine < CollisionRadius)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Shortest distance from point <paramref name="p"/> to line segment AB.
    /// </summary>
    private static double PointToSegmentDistance(Vec2 a, Vec2 b, Vec2 p)
    {
        var ab = b - a;
        var ap = p - a;
        var abLenSq = ab.X * ab.X + ab.Y * ab.Y;

        if (abLenSq < 0.001)
            return a.DistanceTo(p);

        var t = Math.Clamp((ap.X * ab.X + ap.Y * ab.Y) / abLenSq, 0, 1);
        var projection = a + ab * t;
        return projection.DistanceTo(p);
    }
}
