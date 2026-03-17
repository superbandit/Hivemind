using Hivemind.Models;

namespace Hivemind.Combat;

/// <summary>
/// Computes where to aim so a bullet (15 units/step) hits a moving target.
/// Accounts for both turret rotation time and bullet travel time.
/// Uses iterative convergence: estimates total lead steps, linearly
/// extrapolates target position, recalculates angle and rotation time,
/// repeats until stable.
/// Linear extrapolation is used because active enemies maintain speed
/// by accelerating — friction-based prediction systematically undershoots.
/// </summary>
public sealed class LeadCalculator
{
    private const double BulletSpeed = 15.0;
    private const double MaxTurretRotationPerStep = 10.0;
    private const int MaxIterations = 8;

    /// <summary>
    /// Compute the aim point and required turret angle for a moving target,
    /// accounting for the steps needed to rotate the turret into position.
    /// </summary>
    public AimResult ComputeAim(Vec2 shooterPos, double currentTurretAngle, Vec2 targetPos, Vec2 targetVelocity)
    {
        var aimPoint = targetPos;

        for (var i = 0; i < MaxIterations; i++)
        {
            var desiredAngle = shooterPos.AngleTo(aimPoint);
            var angleDiff = Math.Abs(Vec2.NormalizeAngle(desiredAngle - currentTurretAngle));
            var rotationSteps = (int)Math.Ceiling(angleDiff / MaxTurretRotationPerStep);

            var distance = shooterPos.DistanceTo(aimPoint);
            var bulletTravelSteps = (int)Math.Ceiling(distance / BulletSpeed);

            var totalSteps = rotationSteps + Math.Max(bulletTravelSteps, 1);

            // Linear extrapolation — assume enemy maintains observed speed
            var newAimPoint = targetPos + targetVelocity * totalSteps;

            // Converged — aim point barely changed
            if (newAimPoint.DistanceTo(aimPoint) < 0.5)
            {
                aimPoint = newAimPoint;
                break;
            }

            aimPoint = newAimPoint;
        }

        var finalAngle = shooterPos.AngleTo(aimPoint);
        var finalDistance = shooterPos.DistanceTo(aimPoint);
        var finalRotationNeeded = Vec2.NormalizeAngle(finalAngle - currentTurretAngle);

        return new AimResult(aimPoint, finalAngle, finalDistance, finalRotationNeeded);
    }
}

/// <summary>
/// Result of a lead calculation.
/// </summary>
/// <param name="AimPoint">The predicted position to aim at.</param>
/// <param name="DesiredTurretAngle">The absolute turret angle to face the aim point.</param>
/// <param name="Distance">Distance from shooter to the aim point.</param>
/// <param name="TurretRotationNeeded">Signed degrees the turret still needs to rotate (+ = clockwise).</param>
public record AimResult(Vec2 AimPoint, double DesiredTurretAngle, double Distance, double TurretRotationNeeded);
