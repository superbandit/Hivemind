using Hivemind.Models;

namespace Hivemind.Navigation;

/// <summary>
/// Shared movement logic used by both Leader and Follower roles:
/// wall avoidance, speed maintenance, and periodic direction changes.
/// </summary>
public sealed class MovementHelper
{
    private const double WallMargin = 80.0;
    private const double DesiredSpeed = 7.0;
    private const int DirectionChangeInterval = 40;

    private readonly MapSize _mapSize;
    private bool _reversed;
    private bool _flippedForWall;

    public MovementHelper(MapSize mapSize)
    {
        _mapSize = mapSize;
    }

    /// <summary>
    /// If the tank is near a wall and travelling toward it, flips the travel
    /// direction (forward ↔ reverse) and brakes.  No body rotation is used,
    /// so the turret stays free to scan/fire.
    /// Should be checked EVERY step regardless of movement cadence.
    /// </summary>
    public string? GetWallAvoidance(GameState state)
    {
        var tank = state.Tank;

        if (!IsNearWall(tank.Location, out var awayAngle))
        {
            _flippedForWall = false;
            return null;
        }

        // Determine actual travel direction from heading + velocity sign
        var travelAngle = tank.Velocity >= 0 ? tank.Heading : tank.Heading + 180;
        var angleFromSafe = Math.Abs(Vec2.NormalizeAngle(awayAngle - travelAngle));

        // Actually moving toward the wall — flip once per wall encounter
        if (!_flippedForWall && angleFromSafe >= 60 && Math.Abs(tank.Velocity) > 0.5)
        {
            _reversed = !_reversed;
            _flippedForWall = true;
        }

        // Velocity is still carrying us the wrong way — brake hard
        if ((_reversed && tank.Velocity > 0.5) || (!_reversed && tank.Velocity < -0.5))
            return "brake";

        // Build up speed in the correct direction
        if (Math.Abs(tank.Velocity) < DesiredSpeed)
            return _reversed ? "reverse" : "accelerate";

        return null;
    }

    /// <summary>
    /// Returns a movement command for speed maintenance or periodic direction changes.
    /// Respects the current forward/reverse drive mode.
    /// Call this on a cadence (not every step) to leave room for other actions.
    /// </summary>
    public string? GetGeneralMovement(GameState state)
    {
        var tank = state.Tank;

        if (state.Step % DirectionChangeInterval == 0)
        {
            var turn = (state.Step % (DirectionChangeInterval * 2) == 0) ? 8.0 : -8.0;
            return $"rotate {turn}";
        }

        if (Math.Abs(tank.Velocity) < DesiredSpeed)
            return _reversed ? "reverse" : "accelerate";

        return null;
    }

    /// <summary>
    /// Returns a command to drive the tank body toward a target position.
    /// In reverse mode the body is rotated so that the back faces the target.
    /// </summary>
    public string? DriveToward(TankState tank, Vec2 target)
    {
        var angleToTarget = tank.Location.AngleTo(target);
        var desiredHeading = _reversed ? angleToTarget + 180 : angleToTarget;
        var bodyDiff = Vec2.NormalizeAngle(desiredHeading - tank.Heading);

        if (Math.Abs(bodyDiff) > 15)
            return $"rotate {Math.Clamp(bodyDiff, -10, 10)}";

        if (Math.Abs(tank.Velocity) < DesiredSpeed)
            return _reversed ? "reverse" : "accelerate";

        return null;
    }

    private bool IsNearWall(Vec2 position, out double awayAngle)
    {
        awayAngle = 0;
        var nearWall = false;
        var pushX = 0.0;
        var pushY = 0.0;

        if (position.X < WallMargin) { pushX += 1; nearWall = true; }
        if (position.X > _mapSize.Width - WallMargin) { pushX -= 1; nearWall = true; }
        if (position.Y < WallMargin) { pushY += 1; nearWall = true; }
        if (position.Y > _mapSize.Height - WallMargin) { pushY -= 1; nearWall = true; }

        if (nearWall)
            awayAngle = new Vec2(pushX, pushY).AngleTo(Vec2.Zero) + 180;

        return nearWall;
    }
}
