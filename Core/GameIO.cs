using System.Text.Json;
using Hivemind.Models;

namespace Hivemind.Core;

public sealed class GameIO
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public void SendReady()
    {
        Console.WriteLine("bot-start");
    }

    public GameEnvironment ReadEnvironment()
    {
        var line = Console.ReadLine()
            ?? throw new InvalidOperationException("Expected environment message but got EOF.");

        var json = line["environment ".Length..];
        return JsonSerializer.Deserialize<GameEnvironment>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize environment.");
    }

    public GameState? ReadState()
    {
        var line = Console.ReadLine();
        if (line is null || !line.StartsWith("state "))
            return null;

        var json = line["state ".Length..];
        return JsonSerializer.Deserialize<GameState>(json, JsonOptions);
    }

    public void SendCommands(IReadOnlyList<string> commands)
    {
        foreach (var command in commands)
            Console.WriteLine(command);

        Console.WriteLine("command-end");
    }

    public void EndTurn()
    {
        Console.WriteLine("command-end");
    }
}
