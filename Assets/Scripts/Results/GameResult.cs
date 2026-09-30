using System;

// The outcome of a finished game: the record that is saved and where it stands on the leaderboard.
public class GameResult
{
    public GameStats Stats { get; private set; }
    public GameRecord Record { get; private set; }
    // Place on the leaderboard (1 = best), 0 when the game is not on it (lost, or a game of two players).
    public int LeaderboardPlace { get; set; }

    public bool IsHighScore => LeaderboardPlace == 1;

    // Builds the record from what was counted. Nothing is saved here.
    public static GameResult Create(GameStats stats, int winnerId, int aiPlayer, AiDifficulty difficulty, string[] team1Units, string[] team2Units, string log)
    {
        bool vsComputer = aiPlayer != GameSession.NoAi;
        float multiplier = vsComputer ? ScoreCalculator.DifficultyMultiplier(difficulty) : 1.0f;
        GameRecord record = new GameRecord
        {
            id = Guid.NewGuid().ToString("N"),
            date = DateTime.UtcNow.ToString("o"),
            mode = vsComputer ? GameRecord.VsComputer : GameRecord.TwoPlayers,
            difficulty = vsComputer ? difficulty.ToString() : "",
            humanTeam = vsComputer ? GameController.GetOpponent(aiPlayer) : 0,
            winner = winnerId,
            turns = stats.Turns,
            seconds = stats.Seconds,
            log = log ?? ""
        };
        for (int team = 1; team <= 2; team++)
        {
            int opponent = GameController.GetOpponent(team);
            TeamRecord teamRecord = record.Team(team);
            teamRecord.units = team == 1 ? team1Units : team2Units;
            teamRecord.called = stats.CalledBy(team);
            teamRecord.killed = stats.KilledBy(team);
            teamRecord.lost = stats.KilledBy(opponent);
            teamRecord.commanderHealth = stats.CommanderHealth(team);
            teamRecord.score = ScoreCalculator.Score(winnerId == team, teamRecord.killed, teamRecord.lost, stats.Turns, teamRecord.commanderHealth, multiplier);
        }
        return new GameResult { Stats = stats, Record = record };
    }
}
