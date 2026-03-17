using Hivemind.Models;

namespace Hivemind.Communication;

/// <summary>Intel about one enemy shared via chat.</summary>
public record EnemyIntel(int Id, Vec2 Position, Vec2 Velocity, int ObservedAtStep);

public interface ITeamProtocol
{
    /// <summary>Encode enemy intel into a chat message (max 50 chars).</summary>
    string Encode(int step, EnemyIntel intel);

    /// <summary>Decode a chat message. Returns null if it's not our protocol.</summary>
    (int Step, EnemyIntel Intel)? Decode(string message);
}
