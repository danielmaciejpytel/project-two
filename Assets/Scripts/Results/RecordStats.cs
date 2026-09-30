using System.Collections.Generic;

// Numbers for the statistics tab, worked out from the saved games (the human's games against the computer count for most of them).
public class RecordStats
{
    public int Games;
    public int GamesVsComputer;
    public int WinsVsComputer;
    public int BestScore;
    public int LongestWinStreak;
    public int CurrentWinStreak;
    public int UnitsKilled;
    public int UnitsCalled;
    // Unit the human picked most often (empty without games).
    public string FavoriteUnit = "";
    public readonly Dictionary<string, int> GamesByDifficulty = new Dictionary<string, int>();
    public readonly Dictionary<string, int> WinsByDifficulty = new Dictionary<string, int>();

    public float WinRate => GamesVsComputer == 0 ? 0.0f : (float)WinsVsComputer / GamesVsComputer;

    public float WinRateFor(string difficulty)
    {
        GamesByDifficulty.TryGetValue(difficulty, out int games);
        WinsByDifficulty.TryGetValue(difficulty, out int wins);
        return games == 0 ? 0.0f : (float)wins / games;
    }

    // Games in the order they were played, oldest first.
    public static RecordStats From(IReadOnlyList<GameRecord> games)
    {
        RecordStats stats = new RecordStats { Games = games.Count };
        Dictionary<string, int> picks = new Dictionary<string, int>();
        int streak = 0;
        foreach (GameRecord game in games)
        {
            if (!game.IsVsComputer) continue;
            TeamRecord human = game.Team(game.humanTeam);
            stats.GamesVsComputer++;
            stats.UnitsKilled += human.killed;
            stats.UnitsCalled += human.called;
            Increase(stats.GamesByDifficulty, game.difficulty);
            if (game.HumanWon)
            {
                stats.WinsVsComputer++;
                Increase(stats.WinsByDifficulty, game.difficulty);
                stats.BestScore = System.Math.Max(stats.BestScore, human.score);
                streak++;
                stats.LongestWinStreak = System.Math.Max(stats.LongestWinStreak, streak);
            }
            else streak = 0;
            // The commander is in every team, so only the units the human chose count.
            for (int i = 1; i < human.units.Length; i++) Increase(picks, human.units[i]);
        }
        stats.CurrentWinStreak = streak;
        int best = 0;
        foreach (KeyValuePair<string, int> pick in picks)
        {
            if (pick.Value > best) { best = pick.Value; stats.FavoriteUnit = pick.Key; }
        }
        return stats;
    }

    private static void Increase(Dictionary<string, int> counts, string key)
    {
        counts.TryGetValue(key, out int current);
        counts[key] = current + 1;
    }
}
