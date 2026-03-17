using System.Globalization;
using Hivemind.Combat;
using Hivemind.Communication;
using Hivemind.Core;
using Hivemind.Navigation;
using Hivemind.Prediction;
using Hivemind.Strategy;
using Hivemind.Tracking;

// Ensure numeric output uses '.' as decimal separator regardless of system locale
Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

// Ensure stdout is not buffered — critical for piped I/O with the game runner
Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });

var io = new GameIO();

io.SendReady();
var environment = io.ReadEnvironment();

var tracker = new EnemyTracker();
var friendlyTracker = new FriendlyTracker();
var protocol = new TeamProtocol();
var predictor = new LinearPredictor(environment.MapSize);
var leadCalculator = new LeadCalculator();
var movement = new MovementHelper(environment.MapSize);

var isLeader = RoleAssigner.IsLeader(environment);
IBotRole role = isLeader
    ? new LeaderRole(tracker, protocol, predictor, leadCalculator, movement, friendlyTracker)
    : new FollowerRole(tracker, predictor, leadCalculator, movement, friendlyTracker);

var bot = new Bot(role, tracker, friendlyTracker, protocol, environment);

while (true)
{
    var state = io.ReadState();
    if (state is null || state.GameResult is not null)
        break;

    var commands = bot.Update(state);
    io.SendCommands(commands);
}
