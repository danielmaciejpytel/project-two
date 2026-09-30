using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static GameTestUtil;

// The pause menu asks before leaving the game; the end screen sums the game up and can swap the sides against the computer.
public class PauseAndSummaryTests
{
    [SetUp]
    public void SetUp() => Reset();

    [TearDown]
    public void TearDown() => Reset();

    private static Button ScreenButton(EndGameController screen, string path) => screen.transform.Find(path).GetComponent<Button>();

    private static bool Shown(EndGameController screen, string path) => screen.transform.Find(path).gameObject.activeInHierarchy;

    private static IEnumerator KillACommanderAndWait(int playerId)
    {
        Game.GetCommander(playerId).DamageUnit(999, "test");
        yield return WaitUntil(() => Game.IsGameOver, 5.0f);
        yield return new WaitForSecondsRealtime(1.2f);
    }

    [UnityTest]
    public IEnumerator PlayAgainInThePauseMenuAsksFirst()
    {
        yield return StartGame();
        EndGameController screen = EndScreen();
        screen.HandleEscape();

        ScreenButton(screen, "Buttons/PlayAgainButton").onClick.Invoke();

        Assert.IsTrue(Shown(screen, "ConfirmPanel"), "A question appears");
        Assert.IsFalse(Shown(screen, "Buttons"), "It replaces the buttons");
        Assert.AreEqual(0.0f, Time.timeScale, "The game stays paused");
        Assert.IsInstanceOf<BeginTurnState>(Game.CurrentState, "Nothing has been left yet");
        yield return null;
    }

    [UnityTest]
    public IEnumerator SayingNoGoesBackToThePauseMenu()
    {
        yield return StartGame();
        EndGameController screen = EndScreen();
        screen.HandleEscape();
        ScreenButton(screen, "Buttons/BackToMenuButton").onClick.Invoke();

        ScreenButton(screen, "ConfirmPanel/NoButton").onClick.Invoke();

        Assert.IsFalse(Shown(screen, "ConfirmPanel"));
        Assert.IsTrue(Shown(screen, "Buttons"));
        Assert.AreEqual("BeginTurnState", Game.CurrentState.GetType().Name);
        Assert.AreEqual(0.0f, Time.timeScale, "Still paused");
        yield return null;
    }

    [UnityTest]
    public IEnumerator EscapeInTheQuestionMeansNo()
    {
        yield return StartGame();
        EndGameController screen = EndScreen();
        screen.HandleEscape();
        ScreenButton(screen, "Buttons/QuitGameButton").onClick.Invoke();
        Assert.IsTrue(Shown(screen, "ConfirmPanel"), "Quit asks too");

        screen.HandleEscape();

        Assert.IsFalse(Shown(screen, "ConfirmPanel"));
        Assert.IsTrue(Shown(screen, "Buttons"));
        Assert.IsTrue(screen.gameObject.activeSelf, "Escape only closed the question, the game is still paused");
        Assert.AreEqual(0.0f, Time.timeScale);
        yield return null;
    }

    [UnityTest]
    public IEnumerator SayingYesLeavesTheGame()
    {
        yield return StartGame();
        EndGameController screen = EndScreen();
        screen.HandleEscape();
        ScreenButton(screen, "Buttons/BackToMenuButton").onClick.Invoke();

        ScreenButton(screen, "ConfirmPanel/YesButton").onClick.Invoke();
        yield return new WaitForSecondsRealtime(2.0f);

        Assert.AreEqual("MenuScene", SceneManager.GetActiveScene().name);
        Assert.AreEqual(1.0f, Time.timeScale, "The menu does not start frozen");
    }

    [UnityTest]
    public IEnumerator TheEndScreenDoesNotAsk()
    {
        yield return StartGame();
        yield return KillACommanderAndWait(1);
        EndGameController screen = EndScreen();

        ScreenButton(screen, "Buttons/BackToMenuButton").onClick.Invoke();
        yield return new WaitForSecondsRealtime(2.0f);

        Assert.AreEqual("MenuScene", SceneManager.GetActiveScene().name, "The game is over, there is nothing to lose");
    }

    [UnityTest]
    public IEnumerator TheSummaryCountsTurnsCallsAndKills()
    {
        yield return StartGame();
        int player = Game.ActivePlayer;
        int other = GameController.GetOpponent(player);
        UnitController unit = UnitToCall(player);
        Game.DeployAction();
        EventManager.Instance.UnitClicked(unit);
        EventManager.Instance.TileClicked(FreeTileNextToCommander(player));
        yield return null;
        Game.EndTurnAction();
        yield return null;

        yield return KillACommanderAndWait(player);

        Assert.AreEqual(2, Game.Stats.Turns);
        Assert.AreEqual(1, Game.Stats.CalledBy(player));
        Assert.AreEqual(0, Game.Stats.CalledBy(other));
        Assert.AreEqual(1, Game.Stats.KilledBy(other), "The commander that died was killed by the other team");
        Assert.AreEqual(0, Game.Stats.KilledBy(player));
        string text = GetPrivate<TMPro.TMP_Text>(EndScreen(), "_summaryText").text;
        // The numbers are placed with <pos>, so the readable text has no space between the label and the number.
        StringAssert.IsMatch(@"Turns played:\s*2", GetPrivate<TMPro.TMP_Text>(EndScreen(), "_summaryText").GetParsedText());
        StringAssert.Contains("Units called:", text);
        StringAssert.Contains("Units killed:", text);
        Assert.IsTrue(Shown(EndScreen(), "Buttons/SummaryPanel"));
    }

    [UnityTest]
    public IEnumerator TheSummaryIsNotInThePauseMenu()
    {
        yield return StartGame();

        EndScreen().HandleEscape();

        Assert.IsFalse(Shown(EndScreen(), "Buttons/SummaryPanel"));
        Assert.IsFalse(Shown(EndScreen(), "Buttons/SwapSidesButton"));
        yield return null;
    }

    [UnityTest]
    public IEnumerator RematchWithSwappedSidesIsOnlyAgainstTheComputer()
    {
        yield return StartGame();
        yield return KillACommanderAndWait(1);

        Assert.IsFalse(Shown(EndScreen(), "Buttons/SwapSidesButton"), "Nothing to swap in a game of two players");
    }

    [UnityTest]
    public IEnumerator RematchSwapsTheSidesAgainstTheComputer()
    {
        yield return StartGame(2);
        yield return KillACommanderAndWait(1);
        Assert.IsTrue(Shown(EndScreen(), "Buttons/SwapSidesButton"));

        ScreenButton(EndScreen(), "Buttons/SwapSidesButton").onClick.Invoke();
        yield return new WaitForSecondsRealtime(2.0f);

        Assert.AreEqual("MainScene", SceneManager.GetActiveScene().name);
        Assert.IsNull(Game.CurrentState, "A new draft starts");
        Assert.AreEqual(1, GameSession.AiPlayerId, "The computer now leads Super Hot");
    }
}
