// Counts what happened during one game, for the summary on the end screen.
// Players are numbered like everywhere else: 1 = Red Boss, 2 = Blue Boss.
public class GameStats
{
    private readonly int[] _called = new int[3];
    private readonly int[] _killed = new int[3];

    // Turns of both players together: every turn that started, like the numbering in the battle log.
    public int Turns { get; private set; }

    // Game time in seconds (a pause does not count) and the health the commanders had at the end.
    public float Seconds { get; private set; }

    private readonly int[] _commanderHealth = new int[3];

    public int CommanderHealth(int playerId) => _commanderHealth[playerId];

    public int CalledBy(int playerId) => _called[playerId];

    // Units of the opponent that the player killed.
    public int KilledBy(int playerId) => _killed[playerId];

    public void RecordTurnStarted() => Turns++;

    public void RecordEnd(float seconds, int team1CommanderHealth, int team2CommanderHealth)
    {
        Seconds = seconds;
        _commanderHealth[1] = team1CommanderHealth;
        _commanderHealth[2] = team2CommanderHealth;
    }

    public void RecordCall(int playerId) => _called[playerId]++;

    public void RecordKill(int killerPlayerId) => _killed[killerPlayerId]++;
}
