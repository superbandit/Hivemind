using Hivemind.Models;

namespace Hivemind.Strategy;

/// <summary>
/// Role behavior executed each step. Returns a list of commands to send.
/// </summary>
public interface IBotRole
{
    IReadOnlyList<string> Update(GameState state);
}
