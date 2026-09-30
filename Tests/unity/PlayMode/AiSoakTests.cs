using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GameTestUtil;

// The computer plays whole games against a player who only passes. They catch exceptions (any logged error fails
// the test), hangs and games that never end, at every difficulty.
public class AiSoakTests
{
    private AiDifficulty _savedDifficulty;

    [SetUp]
    public void SetUp()
    {
        Reset();
        _savedDifficulty = GameSession.Difficulty;
    }

    [TearDown]
    public void TearDown()
    {
        GameSession.Difficulty = _savedDifficulty;
        Reset();
    }

    private static IEnumerator PlayAgainstAPassingPlayer(AiDifficulty difficulty, float maxRealSeconds)
    {
        GameSession.Difficulty = difficulty;
        yield return StartGame(2);
        Time.timeScale = 20.0f;

        float end = Time.realtimeSinceStartup + maxRealSeconds;
        while (!Game.IsGameOver && Time.realtimeSinceStartup < end)
        {
            // The player (Super Hot) only ends the turn.
            if (Game.ActivePlayer == 1 && Game.CurrentState is BeginTurnState) Game.EndTurnAction();
            yield return null;
        }

        Assert.IsTrue(Game.IsGameOver, $"The game ({difficulty}) did not end in {maxRealSeconds} s, state: {Game.CurrentState}, active player: {Game.ActivePlayer}");
        Assert.IsFalse(Game.GetCommander(2).IsKilled, "The computer's commander survives against a passing player");
    }

    [UnityTest, Timeout(200000)]
    public IEnumerator TheComputerWinsAgainstAPassingPlayerOnNormal()
    {
        yield return PlayAgainstAPassingPlayer(AiDifficulty.Normal, 150.0f);
    }

    [UnityTest, Timeout(200000)]
    public IEnumerator TheComputerWinsAgainstAPassingPlayerOnHard()
    {
        yield return PlayAgainstAPassingPlayer(AiDifficulty.Hard, 150.0f);
    }
}
