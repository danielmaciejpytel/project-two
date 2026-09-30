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

    private static IEnumerator PlayAgainstAPassingPlayer(AiDifficulty difficulty, float maxRealSeconds, int aiPlayer = 2)
    {
        GameSession.Difficulty = difficulty;
        int human = GameController.GetOpponent(aiPlayer);
        yield return StartGame(aiPlayer);
        Time.timeScale = 20.0f;

        float end = Time.realtimeSinceStartup + maxRealSeconds;
        while (!Game.IsGameOver && Time.realtimeSinceStartup < end)
        {
            // The human player only ends the turn.
            if (Game.ActivePlayer == human && Game.CurrentState is BeginTurnState) Game.EndTurnAction();
            yield return null;
        }

        Assert.IsTrue(Game.IsGameOver, $"The game ({difficulty}) did not end in {maxRealSeconds} s, state: {Game.CurrentState}, active player: {Game.ActivePlayer}");
        Assert.IsFalse(Game.GetCommander(aiPlayer).IsKilled, "The computer's commander survives against a passing player");
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

    // After a rematch with swapped sides the computer leads Super Hot and picks its units first in the draft.
    [UnityTest, Timeout(200000)]
    public IEnumerator TheComputerCanPlayTheOtherSideToo()
    {
        yield return PlayAgainstAPassingPlayer(AiDifficulty.Normal, 150.0f, 1);
    }
}
