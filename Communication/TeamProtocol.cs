using System.Globalization;
using Hivemind.Models;

namespace Hivemind.Communication;

/// <summary>
/// Fun battlefield-callout chat protocol. Encodes one enemy's intel as a
/// human-readable team callout that also parses deterministically.
/// Format: <c>{CALLOUT} #{id} @{x},{y} v{vx},{vy} s{step}</c>
/// Velocity preserved to two decimal places. Callouts rotate for variety.
/// Example: <c>CONTACT #3 @350,200 v4.25,-2.10 s42</c> (37 chars / 50 max)
/// </summary>
public sealed class TeamProtocol : ITeamProtocol
{
    private static readonly string[] Callouts =
    [
        "CONTACT", "BOGEY", "TALLY HO", "EYES ON",
        "ENGAGING", "FOCUS ON", "TARGET", "GOT EM"
    ];

    public string Encode(int step, EnemyIntel intel)
    {
        var callout = Callouts[step % Callouts.Length];
        var x = (int)Math.Round(intel.Position.X);
        var y = (int)Math.Round(intel.Position.Y);
        var vx = intel.Velocity.X.ToString("F2", CultureInfo.InvariantCulture);
        var vy = intel.Velocity.Y.ToString("F2", CultureInfo.InvariantCulture);

        return $"{callout} #{intel.Id} @{x},{y} v{vx},{vy} s{intel.ObservedAtStep}";
    }

    public (int Step, EnemyIntel Intel)? Decode(string message)
    {
        if (string.IsNullOrEmpty(message))
            return null;

        // Locate markers:  " #id @x,y vVx,Vy sStep"
        var hashIdx = message.IndexOf(" #");
        if (hashIdx < 0) return null;

        var atIdx = message.IndexOf(" @", hashIdx);
        if (atIdx < 0) return null;

        var vIdx = message.IndexOf(" v", atIdx);
        if (vIdx < 0) return null;

        var sIdx = message.IndexOf(" s", vIdx + 1);
        if (sIdx < 0) return null;

        // id
        if (!int.TryParse(
                message.AsSpan(hashIdx + 2, atIdx - hashIdx - 2),
                CultureInfo.InvariantCulture, out var id))
            return null;

        // position (x,y)
        var posSpan = message.AsSpan(atIdx + 2, vIdx - atIdx - 2);
        var comma = posSpan.IndexOf(',');
        if (comma < 0) return null;
        if (!double.TryParse(posSpan[..comma], CultureInfo.InvariantCulture, out var px) ||
            !double.TryParse(posSpan[(comma + 1)..], CultureInfo.InvariantCulture, out var py))
            return null;

        // velocity (vx,vy) — may contain negative sign, so find comma carefully
        var velSpan = message.AsSpan(vIdx + 2, sIdx - vIdx - 2);
        var velComma = velSpan.IndexOf(',');
        if (velComma < 0) return null;
        if (!double.TryParse(velSpan[..velComma], CultureInfo.InvariantCulture, out var vx) ||
            !double.TryParse(velSpan[(velComma + 1)..], CultureInfo.InvariantCulture, out var vy))
            return null;

        // observed-at step
        if (!int.TryParse(message.AsSpan(sIdx + 2), CultureInfo.InvariantCulture, out var observedStep))
            return null;

        return (observedStep, new(id, new Vec2(px, py), new Vec2(vx, vy), observedStep));
    }
}
