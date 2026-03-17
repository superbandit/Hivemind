using Hivemind.Communication;
using Hivemind.Models;
using Hivemind.Strategy;
using Hivemind.Tracking;

namespace Hivemind.Core;

/// <summary>
/// Central orchestrator. Each step it updates shared state (tracker, chat intel)
/// then delegates command decisions to the active <see cref="IBotRole"/>.
/// </summary>
public sealed class Bot
{
    private readonly IBotRole _role;
    private readonly IEnemyTracker _tracker;
    private readonly FriendlyTracker _friendlyTracker;
    private readonly ITeamProtocol _protocol;
    private readonly int _myTankId;
    private readonly int _myTeamId;

    public Bot(IBotRole role, IEnemyTracker tracker, FriendlyTracker friendlyTracker, ITeamProtocol protocol, GameEnvironment environment)
    {
        _role = role;
        _tracker = tracker;
        _friendlyTracker = friendlyTracker;
        _protocol = protocol;

        var me = RoleAssigner.GetMyTank(environment);
        _myTankId = me.Id;
        _myTeamId = me.TeamId;
    }

    public IReadOnlyList<string> Update(GameState state)
    {
        MarkDestroyedEnemies(state);
        _tracker.UpdateFromScans(state.Step, state.TankScans);
        _friendlyTracker.Update(state.Step, state.TankScans);
        ProcessTeamChat(state);

        var commands = new List<string>(_role.Update(state));

        LogHitsReceived(state, commands);

        return commands;
    }

    private void ProcessTeamChat(GameState state)
    {
        foreach (var msg in state.ChatMessages)
        {
            if (msg.TeamId != _myTeamId || msg.TankId == _myTankId)
                continue;

            var decoded = _protocol.Decode(msg.Message);
            if (decoded is null)
                continue;

            var isFirstIntel = true;
            _tracker.UpdateFromIntel(decoded.Value.Step, decoded.Value.Intel.Id, decoded.Value.Intel.Position, decoded.Value.Intel.Velocity);
            if (isFirstIntel)
            {
                _tracker.SetFocusTarget(decoded.Value.Intel.Id);
                isFirstIntel = false;
            }
        }
    }

    private void MarkDestroyedEnemies(GameState state)
    {
        foreach (var destroyed in state.DestroyedTankScans)
        {
            if (destroyed.IsEnemy)
                _tracker.MarkDestroyed(destroyed.TankId);
        }
    }

    private static void LogHitsReceived(GameState state, List<string> commands)
    {
        foreach (var hit in state.Hits)
        {
            commands.Add($"log [HIT] took {hit.Damage} dmg from {hit.Name} (#{hit.TankId}) | hp={state.Tank.Health.Value}/{state.Tank.Health.Max}");
        }
    }
}
