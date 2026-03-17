using Hivemind.Models;

namespace Hivemind.Strategy;

/// <summary>
/// Determines the role for this bot instance based on the environment.
/// The friendly tank with the lowest Id becomes the Leader (scanner).
/// All others become Followers (shooters).
/// </summary>
public static class RoleAssigner
{
    public static bool IsLeader(GameEnvironment environment)
    {
        var myTank = Array.Find(environment.Tanks, t => t.IsYou)
            ?? throw new InvalidOperationException("Could not find own tank in environment.");

        var lowestFriendlyId = environment.Tanks
            .Where(t => !t.IsEnemy)
            .Min(t => t.Id);

        return myTank.Id == lowestFriendlyId;
    }

    public static TankInfo GetMyTank(GameEnvironment environment)
    {
        return Array.Find(environment.Tanks, t => t.IsYou)
            ?? throw new InvalidOperationException("Could not find own tank in environment.");
    }
}
