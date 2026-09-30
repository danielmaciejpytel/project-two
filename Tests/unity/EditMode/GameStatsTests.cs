using NUnit.Framework;

// The numbers on the summary of the end screen.
public class GameStatsTests
{
    [Test]
    public void ANewGameHasNothingCounted()
    {
        GameStats stats = new GameStats();

        Assert.AreEqual(0, stats.Turns);
        Assert.AreEqual(0, stats.CalledBy(1));
        Assert.AreEqual(0, stats.CalledBy(2));
        Assert.AreEqual(0, stats.KilledBy(1));
        Assert.AreEqual(0, stats.KilledBy(2));
    }

    [Test]
    public void TurnsCallsAndKillsAreCountedPerTeam()
    {
        GameStats stats = new GameStats();

        stats.RecordTurnStarted();
        stats.RecordTurnStarted();
        stats.RecordTurnStarted();
        stats.RecordCall(1);
        stats.RecordCall(1);
        stats.RecordCall(2);
        stats.RecordKill(2);

        Assert.AreEqual(3, stats.Turns);
        Assert.AreEqual(2, stats.CalledBy(1));
        Assert.AreEqual(1, stats.CalledBy(2));
        Assert.AreEqual(0, stats.KilledBy(1));
        Assert.AreEqual(1, stats.KilledBy(2));
    }

    [Test]
    public void SwappingSidesOnlyChangesAGameAgainstTheComputer()
    {
        int saved = GameSession.AiPlayerId;
        try
        {
            GameSession.AiPlayerId = 2;
            GameSession.SwapSides();
            Assert.AreEqual(1, GameSession.AiPlayerId);
            GameSession.SwapSides();
            Assert.AreEqual(2, GameSession.AiPlayerId);

            GameSession.AiPlayerId = GameSession.NoAi;
            GameSession.SwapSides();
            Assert.AreEqual(GameSession.NoAi, GameSession.AiPlayerId, "Two players: nothing to swap");
        }
        finally
        {
            GameSession.AiPlayerId = saved;
        }
    }
}
