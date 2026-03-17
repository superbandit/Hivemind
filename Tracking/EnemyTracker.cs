using Hivemind.Models;

namespace Hivemind.Tracking;

public sealed class EnemyTracker : IEnemyTracker
{
    private const int StaleThreshold = 75;

    private readonly Dictionary<int, TrackedEnemy> _enemies = [];
    private readonly Dictionary<int, (Vec2 Position, int Step)> _previousSighting = [];

    public int? FocusTargetId { get; private set; }

    public void SetFocusTarget(int enemyId)
    {
        FocusTargetId = enemyId;
    }

    public void UpdateFromScans(int step, IEnumerable<ScannedTank> scans)
    {
        foreach (var scan in scans)
        {
            if (!scan.IsEnemy)
                continue;

            var velocity = ComputeVelocity(scan.TankId, scan.Location, step);

            _previousSighting[scan.TankId] = (scan.Location, step);
            _enemies[scan.TankId] = new TrackedEnemy(
                scan.TankId, scan.Location, velocity, scan.Heading, step);
        }

        PruneStale(step);
    }

    public void UpdateFromIntel(int intelStep, int enemyId, Vec2 position, Vec2 velocity)
    {
        // Only accept intel that is newer than what we already have
        if (_enemies.TryGetValue(enemyId, out var existing) && existing.LastSeenStep >= intelStep)
            return;

        _previousSighting[enemyId] = (position, intelStep);
        _enemies[enemyId] = new TrackedEnemy(enemyId, position, velocity, 0, intelStep);
    }

    public void MarkDestroyed(int tankId)
    {
        _enemies.Remove(tankId);
        _previousSighting.Remove(tankId);

        if (FocusTargetId == tankId)
            FocusTargetId = null;
    }

    public TrackedEnemy? Get(int tankId)
    {
        return _enemies.GetValueOrDefault(tankId);
    }

    public IReadOnlyList<TrackedEnemy> GetAllAlive()
    {
        return [.. _enemies.Values];
    }

    private void PruneStale(int currentStep)
    {
        var staleIds = _enemies
            .Where(kvp => currentStep - kvp.Value.LastSeenStep > StaleThreshold)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var id in staleIds)
        {
            _enemies.Remove(id);
            _previousSighting.Remove(id);

            if (FocusTargetId == id)
                FocusTargetId = null;
        }
    }

    private Vec2 ComputeVelocity(int tankId, Vec2 currentPosition, int currentStep)
    {
        if (!_previousSighting.TryGetValue(tankId, out var prev))
            return Vec2.Zero;

        var dt = currentStep - prev.Step;
        if (dt <= 0)
            return Vec2.Zero;

        return (currentPosition - prev.Position) * (1.0 / dt);
    }
}
