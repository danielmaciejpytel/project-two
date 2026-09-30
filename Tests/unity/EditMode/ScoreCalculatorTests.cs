using NUnit.Framework;

// The formula behind the score of a team.
public class ScoreCalculatorTests
{
    [Test]
    public void ALostGameScoresOnlyKillsMinusLosses()
    {
        // 3 kills, 4 lost units, no win bonuses.
        Assert.AreEqual(3 * 100 - 4 * 50, ScoreCalculator.Score(false, 3, 4, 20, 0, 1.0f));
    }

    [Test]
    public void AScoreNeverGoesBelowZero()
    {
        Assert.AreEqual(0, ScoreCalculator.Score(false, 0, 5, 40, 0, 1.0f));
    }

    [Test]
    public void AWinAddsThePointsForTheWinTheSpeedAndTheCommandersHealth()
    {
        // Win 1000 + 5 kills 500 - 1 lost 50 + 10 turns under the limit of 30 (200) + 8 health left (240).
        int expected = 1000 + 500 - 50 + (30 - 20) * 20 + 8 * 30;
        Assert.AreEqual(expected, ScoreCalculator.Score(true, 5, 1, 20, 8, 1.0f));
    }

    [Test]
    public void ASlowWinGetsNoSpeedBonus()
    {
        int slow = ScoreCalculator.Score(true, 2, 0, 30, 5, 1.0f);
        int slower = ScoreCalculator.Score(true, 2, 0, 60, 5, 1.0f);
        Assert.AreEqual(slow, slower, "Turns above the limit cost nothing more");
        Assert.AreEqual(1000 + 200 + 150, slow);
    }

    [Test]
    public void ALossGetsNoBonusesEvenWithAHealthyCommander()
    {
        Assert.AreEqual(100, ScoreCalculator.Score(false, 1, 0, 5, 10, 1.0f));
    }

    [Test]
    public void TheDifficultyMultipliesTheScore()
    {
        Assert.AreEqual(0.5f, ScoreCalculator.DifficultyMultiplier(AiDifficulty.Easy));
        Assert.AreEqual(1.0f, ScoreCalculator.DifficultyMultiplier(AiDifficulty.Normal));
        Assert.AreEqual(1.5f, ScoreCalculator.DifficultyMultiplier(AiDifficulty.Hard));
        int normal = ScoreCalculator.Score(true, 3, 0, 20, 6, 1.0f);
        Assert.AreEqual(normal * 1.5f, ScoreCalculator.Score(true, 3, 0, 20, 6, 1.5f), 1.0f);
        Assert.AreEqual(normal * 0.5f, ScoreCalculator.Score(true, 3, 0, 20, 6, 0.5f), 1.0f);
    }
}
