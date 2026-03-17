using Hivemind.Combat;
using Hivemind.Models;
using Hivemind.Navigation;
using Hivemind.Prediction;
using Hivemind.Tracking;

namespace Hivemind.Strategy;

/// <summary>
/// The Follower receives target intel from the Leader via chat (processed by Bot),
/// aims its turret at the predicted position using lead calculation, and fires.
/// During gun cooldown it drives toward the target to close distance.
/// Falls back to turret sweep if no target is known.
/// </summary>
public sealed class FollowerRole : IBotRole
{
    private const double SweepDegreesPerStep = 10.0;
    private const double FireAngleTolerance = 3.0;
    private const double GunAlmostReady = 12.0;
    private const int MovementCadence = 3;

    private readonly IEnemyTracker _tracker;
    private readonly IMovementPredictor _predictor;
    private readonly LeadCalculator _leadCalculator;
    private readonly MovementHelper _movement;
    private readonly FriendlyTracker _friendlyTracker;

    public FollowerRole(
        IEnemyTracker tracker,
        IMovementPredictor predictor,
        LeadCalculator leadCalculator,
        MovementHelper movement,
        FriendlyTracker friendlyTracker)
    {
        _tracker = tracker;
        _predictor = predictor;
        _leadCalculator = leadCalculator;
        _movement = movement;
        _friendlyTracker = friendlyTracker;
    }

    public IReadOnlyList<string> Update(GameState state)
    {
        var commands = new List<string>();
        var tank = state.Tank;

        var target = PickTarget(state);

        if (target is null)
        {
            commands.Add(ChooseIdleAction(state));
            commands.Add($"log [Follower] step {state.Step} | no target | sweeping");
            return commands;
        }

        var staleness = state.Step - target.LastSeenStep;
        var estimatedPos = _predictor.Predict(target.Position, target.Velocity, staleness);

        // Pass observed velocity directly — active enemies maintain speed by accelerating.
        // Friction decay was causing systematic under-prediction of lead.
        var aim = _leadCalculator.ComputeAim(tank.Location, tank.TurretHeading, estimatedPos, target.Velocity);

        var actionCommand = ChooseAction(state, aim);
        commands.Add(actionCommand);

        commands.Add($"log [Follower] step {state.Step} | target #{target.Id} | " +
                     $"rot {aim.TurretRotationNeeded:F1} | dist {aim.Distance:F0}");

        return commands;
    }

    private string ChooseAction(GameState state, AimResult aim)
    {
        var tank = state.Tank;
        var gunReady = tank.GunEnergy.Value >= tank.GunEnergy.Max;
        var turretOnTarget = Math.Abs(aim.TurretRotationNeeded) <= FireAngleTolerance;

        // 1. Wall avoidance — ALWAYS checked, overrides everything
        var wallCommand = _movement.GetWallAvoidance(state);
        if (wallCommand is not null)
            return wallCommand;

        // 2. Fire when aligned, gun charged, and no friendly in the way
        if (gunReady && turretOnTarget && !_friendlyTracker.IsUnsafeToFire(tank.Location, aim.AimPoint, aim.Distance, state.Step))
            return "fire-gun";

        // 3. Gun almost ready — prioritize turret alignment to fire ASAP
        if (tank.GunEnergy.Value >= GunAlmostReady)
            return RotateTurretToward(aim);

        // 4. During cooldown, alternate between movement and turret correction
        if (state.Step % MovementCadence == 0)
        {
            var driveCommand = _movement.GetGeneralMovement(state)
                            ?? _movement.DriveToward(tank, aim.AimPoint);
            if (driveCommand is not null)
                return driveCommand;
        }

        // 5. Default: keep adjusting turret toward target
        return RotateTurretToward(aim);
    }

    private string ChooseIdleAction(GameState state)
    {
        // No target known — survive and sweep to find enemies independently
        var wallCommand = _movement.GetWallAvoidance(state);
        if (wallCommand is not null)
            return wallCommand;

        if (state.Step % MovementCadence == 0)
        {
            var moveCommand = _movement.GetGeneralMovement(state);
            if (moveCommand is not null)
                return moveCommand;
        }

        return $"rotate-turret {SweepDegreesPerStep}";
    }

    private TrackedEnemy? PickTarget(GameState state)
    {
        var enemies = _tracker.GetAllAlive();
        if (enemies.Count == 0)
            return null;

        // Always obey the leader's focus target if it's still tracked
        if (_tracker.FocusTargetId is { } focusId)
        {
            var focus = _tracker.Get(focusId);
            if (focus is not null)
                return focus;
        }

        // No leader directive — fall back to closest enemy
        return enemies
            .OrderBy(e => state.Tank.Location.DistanceTo(e.Position))
            .FirstOrDefault();
    }

    private static string RotateTurretToward(AimResult aim)
    {
        var rotation = Math.Clamp(aim.TurretRotationNeeded, -10, 10);
        return $"rotate-turret {rotation}";
    }
}
