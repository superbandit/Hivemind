using Hivemind.Combat;
using Hivemind.Communication;
using Hivemind.Models;
using Hivemind.Navigation;
using Hivemind.Prediction;
using Hivemind.Tracking;

namespace Hivemind.Strategy;

/// <summary>
/// The Leader sweeps its turret to scan the battlefield, tracks enemies,
/// and broadcasts the focus target's raw position to Followers via chat.
/// Keeps moving to avoid being an easy target.
/// Uses lead calculation for aimed fire when the gun is almost ready,
/// then returns to scanning. During cooldown the turret tracks the target's
/// actual position to keep it in the scan cone for fresh data.
/// </summary>
public sealed class LeaderRole : IBotRole
{
    private const double SweepDegreesPerStep = 10.0;
    private const double FireAngleTolerance = 3.0;
    private const double GunAlmostReady = 12.0;
    private const int MovementCadence = 4;
    private const int MaxTargetStaleness = 30;

    private readonly IEnemyTracker _tracker;
    private readonly ITeamProtocol _protocol;
    private readonly IMovementPredictor _predictor;
    private readonly LeadCalculator _leadCalculator;
    private readonly MovementHelper _movement;
    private readonly FriendlyTracker _friendlyTracker;

    private int? _focusTargetId;

    public LeaderRole(
        IEnemyTracker tracker,
        ITeamProtocol protocol,
        IMovementPredictor predictor,
        LeadCalculator leadCalculator,
        MovementHelper movement,
        FriendlyTracker friendlyTracker)
    {
        _tracker = tracker;
        _protocol = protocol;
        _predictor = predictor;
        _leadCalculator = leadCalculator;
        _movement = movement;
        _friendlyTracker = friendlyTracker;
    }

    public IReadOnlyList<string> Update(GameState state)
    {
        var commands = new List<string>();

        BroadcastFocusTarget(state, commands);

        var actionCommand = ChooseAction(state);
        commands.Add(actionCommand);

        LogStatus(state, commands);

        return commands;
    }

    private string ChooseAction(GameState state)
    {
        var tank = state.Tank;
        var gunReady = tank.GunEnergy.Value >= tank.GunEnergy.Max;
        var target = GetFocusTarget(state);

        // Compute lead aim if we have a fresh target
        AimResult? aim = null;
        if (target is not null)
        {
            var staleness = state.Step - target.LastSeenStep;
            var estimatedPos = _predictor.Predict(target.Position, target.Velocity, staleness);
            aim = _leadCalculator.ComputeAim(tank.Location, tank.TurretHeading, estimatedPos, target.Velocity);
        }

        // 1. Aimed fire: turret aligned to lead point + gun ready + safe
        if (gunReady && aim is not null)
        {
            var turretOnTarget = Math.Abs(aim.TurretRotationNeeded) <= FireAngleTolerance;
            if (turretOnTarget && !_friendlyTracker.IsUnsafeToFire(tank.Location, aim.AimPoint, aim.Distance, state.Step))
                return "fire-gun";
        }

        // 2. Wall avoidance
        var wallCommand = _movement.GetWallAvoidance(state);
        if (wallCommand is not null)
            return wallCommand;

        // 3. Gun almost ready — briefly align turret to lead point for precise shot
        if (aim is not null && tank.GunEnergy.Value >= GunAlmostReady)
            return RotateTurretToward(aim);

        // 4. Movement on cadence
        if (state.Step % MovementCadence == 0)
        {
            var moveCommand = _movement.GetGeneralMovement(state);
            if (moveCommand is not null)
                return moveCommand;
        }

        // 5. Track target's actual position (keeps it in scan cone for fresh data),
        //    or sweep to find enemies
        return GetScanCommand(state, target);
    }

    /// <summary>
    /// During cooldown: track the target's estimated position to keep it in the
    /// 10° scan cone (maintaining fresh tracker data). When no target, sweep.
    /// </summary>
    private string GetScanCommand(GameState state, TrackedEnemy? target)
    {
        if (target is null)
            return $"rotate-turret {SweepDegreesPerStep}";

        var staleness = state.Step - target.LastSeenStep;
        var estimatedPos = _predictor.Predict(target.Position, target.Velocity, staleness);

        var angleToTarget = state.Tank.Location.AngleTo(estimatedPos);
        var turretDiff = Vec2.NormalizeAngle(angleToTarget - state.Tank.TurretHeading);

        return $"rotate-turret {Math.Clamp(turretDiff, -10, 10)}";
    }

    private TrackedEnemy? GetFocusTarget(GameState state)
    {
        if (_focusTargetId.HasValue)
        {
            var current = _tracker.Get(_focusTargetId.Value);
            if (current is not null && state.Step - current.LastSeenStep <= MaxTargetStaleness)
                return current;

            _focusTargetId = null;
        }

        var best = _tracker.GetAllAlive()
            .Where(e => state.Step - e.LastSeenStep <= MaxTargetStaleness)
            .OrderBy(e => state.Tank.Location.DistanceTo(e.Position))
            .FirstOrDefault();

        if (best is not null)
            _focusTargetId = best.Id;

        return best;
    }

    private void BroadcastFocusTarget(GameState state, List<string> commands)
    {
        if (state.Tank.ChatEnergy.Value < state.Tank.ChatEnergy.Max)
            return;

        var target = GetFocusTarget(state);
        if (target is null)
            return;

        var message = _protocol.Encode(state.Step, new(target.Id, target.Position, target.Velocity, target.LastSeenStep));
        commands.Add($"chat {message}");
    }

    private static string RotateTurretToward(AimResult aim)
    {
        var rotation = Math.Clamp(aim.TurretRotationNeeded, -10, 10);
        return $"rotate-turret {rotation}";
    }

    private void LogStatus(GameState state, List<string> commands)
    {
        var focusName = _focusTargetId.HasValue ? $"focus #{_focusTargetId.Value}" : "no focus";
        var enemies = _tracker.GetAllAlive();
        commands.Add($"log [Leader] step {state.Step} | {focusName} | tracking {enemies.Count}");
    }
}
