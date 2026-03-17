using Hivemind.Models;

namespace Hivemind.Prediction;

/// <summary>
/// Extrapolates enemy position using velocity with 2% friction per step.
/// Velocity stops entirely below 0.5 (matches game physics).
/// Clamps to map bounds — tank velocity becomes zero on wall contact.
/// </summary>
public sealed class LinearPredictor : IMovementPredictor
{
    private const double FrictionFactor = 0.98;
    private const double StopThreshold = 0.5;

    private readonly double _maxX;
    private readonly double _maxY;

    public LinearPredictor(MapSize mapSize)
    {
        _maxX = mapSize.Width;
        _maxY = mapSize.Height;
    }

    public Vec2 Predict(Vec2 position, Vec2 velocity, int stepsAhead)
    {
        var pos = position;
        var vel = velocity;

        for (var i = 0; i < stepsAhead; i++)
        {
            vel = vel * FrictionFactor;

            if (vel.Length < StopThreshold)
                break;

            pos = pos + vel;

            // Tank stops on wall contact
            if (pos.X <= 0 || pos.X >= _maxX || pos.Y <= 0 || pos.Y >= _maxY)
            {
                pos = new Vec2(Math.Clamp(pos.X, 0, _maxX), Math.Clamp(pos.Y, 0, _maxY));
                break;
            }
        }

        return pos;
    }
}
