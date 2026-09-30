using System;

// What is remembered about one team of a finished game.
[Serializable]
public class TeamRecord
{
    // The team's units, the commander first.
    public string[] units = new string[0];
    public int called;
    public int killed;
    public int lost;
    public int commanderHealth;
    public int score;
}

// One finished game, as it is saved in the results file.
[Serializable]
public class GameRecord
{
    public const string TwoPlayers = "TwoPlayers";
    public const string VsComputer = "VsComputer";

    public string id;
    // UTC time in the round-trip format ("o").
    public string date;
    public string mode = TwoPlayers;
    // Easy, Normal or Hard; empty in a game of two players.
    public string difficulty = "";
    // The team of the human against the computer (1 or 2), 0 in a game of two players.
    public int humanTeam;
    public int winner;
    public int turns;
    public float seconds;
    public TeamRecord team1 = new TeamRecord();
    public TeamRecord team2 = new TeamRecord();
    // The battle log as text.
    public string log = "";

    public bool IsVsComputer => mode == VsComputer;

    public bool HumanWon => IsVsComputer && winner == humanTeam;

    public TeamRecord Team(int teamId) => teamId == 1 ? team1 : team2;

    // The score that counts for the leaderboard: the human's.
    public int HumanScore => IsVsComputer ? Team(humanTeam).score : 0;

    public DateTime LocalDate => DateTime.TryParse(date, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsed) ? parsed.ToLocalTime() : DateTime.MinValue;
}
