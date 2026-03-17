using Hivemind.Models;

namespace Hivemind.Prediction;

public interface IMovementPredictor
{
    /// <summary>
    /// Predict where an enemy will be at <paramref name="stepsAhead"/> steps
    /// from the given position and velocity.
    /// </summary>
    Vec2 Predict(Vec2 position, Vec2 velocity, int stepsAhead);
}
