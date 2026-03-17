using Hivemind.Models;

namespace Hivemind.Tracking;

public interface IEnemyTracker
{
    /// <summary>Update tracking from this step's scan results.</summary>
    void UpdateFromScans(int step, IEnumerable<ScannedTank> scans);

    /// <summary>Update tracking from teammate intel received via chat.</summary>
    void UpdateFromIntel(int intelStep, int enemyId, Vec2 position, Vec2 velocity);

    /// <summary>Mark an enemy as destroyed so it's excluded from targeting.</summary>
    void MarkDestroyed(int tankId);

    /// <summary>Get tracked data for a specific enemy, or null if unknown.</summary>
    TrackedEnemy? Get(int tankId);

    /// <summary>Get all alive enemies we have data on.</summary>
    IReadOnlyList<TrackedEnemy> GetAllAlive();

    /// <summary>The enemy ID the leader wants the team to focus on, or null.</summary>
    int? FocusTargetId { get; }

    /// <summary>Set the team's focus target (called when receiving leader intel).</summary>
    void SetFocusTarget(int enemyId);
}
