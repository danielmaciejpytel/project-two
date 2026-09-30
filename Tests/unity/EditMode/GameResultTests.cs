using NUnit.Framework;

// How the counted numbers become the record of a game.
public class GameResultTests
{
    private static GameStats Stats()
    {
        GameStats stats = new GameStats();
        for (int i = 0; i < 12; i++) stats.RecordTurnStarted();
        stats.RecordCall(1);
        stats.RecordCall(1);
        stats.RecordCall(2);
        // Team 1 killed 3 units, team 2 killed 1.
        stats.RecordKill(1);
        stats.RecordKill(1);
        stats.RecordKill(1);
        stats.RecordKill(2);
        stats.RecordEnd(95.0f, 7, 0);
        return stats;
    }

    [Test]
    public void AGameAgainstTheComputerIsScoredWithTheDifficulty()
    {
        GameResult result = GameResult.Create(Stats(), 1, 2, AiDifficulty.Hard, new[] { "Super Hot", "Striker" }, new[] { "Super Cold" }, "log");
        GameRecord record = result.Record;

        Assert.AreEqual(GameRecord.VsComputer, record.mode);
        Assert.AreEqual("Hard", record.difficulty);
        Assert.AreEqual(1, record.humanTeam, "The computer leads team 2");
        Assert.AreEqual(1, record.winner);
        Assert.IsTrue(record.HumanWon);
        Assert.AreEqual(12, record.turns);
        Assert.AreEqual(95.0f, record.seconds);
        Assert.AreEqual(3, record.team1.killed);
        Assert.AreEqual(1, record.team1.lost);
        Assert.AreEqual(2, record.team1.called);
        Assert.AreEqual(7, record.team1.commanderHealth);
        Assert.AreEqual(1, record.team2.killed);
        Assert.AreEqual(3, record.team2.lost);
        Assert.AreEqual("log", record.log);
        CollectionAssert.AreEqual(new[] { "Super Hot", "Striker" }, record.team1.units);
        int team1 = ScoreCalculator.Score(true, 3, 1, 12, 7, 1.5f);
        Assert.AreEqual(team1, record.team1.score);
        Assert.AreEqual(ScoreCalculator.Score(false, 1, 3, 12, 0, 1.5f), record.team2.score);
        Assert.AreEqual(team1, record.HumanScore);
    }

    [Test]
    public void ThePlayersTeamCanBeTheSecondOne()
    {
        GameResult result = GameResult.Create(Stats(), 1, 1, AiDifficulty.Normal, new string[0], new string[0], "");

        Assert.AreEqual(2, result.Record.humanTeam, "The computer leads team 1, the human team 2");
        Assert.IsFalse(result.Record.HumanWon, "Team 1 won, and it was the computer");
        Assert.AreEqual(result.Record.team2.score, result.Record.HumanScore);
    }

    [Test]
    public void AGameOfTwoPlayersHasNoDifficultyAndNoHumanTeam()
    {
        GameResult result = GameResult.Create(Stats(), 2, GameSession.NoAi, AiDifficulty.Hard, new string[0], new string[0], "");
        GameRecord record = result.Record;

        Assert.AreEqual(GameRecord.TwoPlayers, record.mode);
        Assert.AreEqual("", record.difficulty);
        Assert.AreEqual(0, record.humanTeam);
        Assert.IsFalse(record.HumanWon);
        Assert.AreEqual(0, record.HumanScore, "Two players: no score for the leaderboard");
        Assert.AreEqual(ScoreCalculator.Score(true, 1, 3, 12, 0, 1.0f), record.team2.score, "Two players count as Normal");
    }

    [Test]
    public void TheHighScoreIsThePlaceOne()
    {
        GameResult result = GameResult.Create(Stats(), 1, 2, AiDifficulty.Normal, new string[0], new string[0], "");

        result.LeaderboardPlace = 1;
        Assert.IsTrue(result.IsHighScore);
        result.LeaderboardPlace = 3;
        Assert.IsFalse(result.IsHighScore);
    }
}
