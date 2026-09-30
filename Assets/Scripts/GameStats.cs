// Counts what happened during one game, for the summary on the end screen.
// Players are numbered like everywhere else: 1 = Super Hot, 2 = Super Cold.
public class GameStats
{
    private readonly int[] _called = new int[3];
    private readonly int[] _killed = new int[3];

    // Turns of both players together: every turn that started, like the numbering in the battle log.
    public int Turns { get; private set; }

    public int CalledBy(int playerId) => _called[playerId];

    // Units of the opponent that the player killed.
    public int KilledBy(int playerId) => _killed[playerId];

    public void RecordTurnStarted() => Turns++;

    public void RecordCall(int playerId) => _called[playerId]++;

    public void RecordKill(int killerPlayerId) => _killed[killerPlayerId]++;
}
