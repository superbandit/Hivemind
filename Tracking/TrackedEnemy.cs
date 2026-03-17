using Hivemind.Models;

namespace Hivemind.Tracking;

/// <summary>Latest known state of a tracked enemy.</summary>
public record TrackedEnemy(int Id, Vec2 Position, Vec2 Velocity, double Heading, int LastSeenStep);
