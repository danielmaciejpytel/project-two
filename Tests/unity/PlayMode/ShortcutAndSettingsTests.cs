using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static GameTestUtil;

// Space and C, and the difficulty in the settings of the pause menu and the end screen.
public class ShortcutAndSettingsTests
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

    [UnityTest]
    public IEnumerator SpaceEndsTheTurn()
    {
        yield return StartGame();
        int first = Game.ActivePlayer;

        bool pressed = Game.GetUI().PressEndTurnShortcut();
        yield return null;

        Assert.IsTrue(pressed);
        Assert.AreEqual(GameController.GetOpponent(first), Game.ActivePlayer);
    }

    [UnityTest]
    public IEnumerator CStartsCallAndPressingItAgainCancelsIt()
    {
        yield return StartGame();

        Assert.IsTrue(Game.GetUI().PressCallShortcut());
        Assert.IsInstanceOf<DeploymentState>(Game.CurrentState);

        Assert.IsTrue(Game.GetUI().PressCallShortcut());
        Assert.IsInstanceOf<BeginTurnState>(Game.CurrentState);
        Assert.IsTrue(Button("DeployMinionButton").gameObject.activeSelf, "Cancelling does not use up the call");
        yield return null;
    }

    [UnityTest]
    public IEnumerator CDoesNothingOnceTheCallIsUsedUp()
    {
        yield return StartGame();
        int player = Game.ActivePlayer;
        Game.DeployAction();
        EventManager.Instance.UnitClicked(UnitToCall(player));
        EventManager.Instance.TileClicked(FreeTileNextToCommander(player));
        yield return null;
        Assert.IsFalse(Button("DeployMinionButton").gameObject.activeSelf);

        Assert.IsFalse(Game.GetUI().PressCallShortcut());
        Assert.IsInstanceOf<BeginTurnState>(Game.CurrentState);
    }

    [UnityTest]
    public IEnumerator TheShortcutsAreOffInThePauseMenu()
    {
        yield return StartGame();
        int first = Game.ActivePlayer;
        EndScreen().HandleEscape();

        Assert.IsFalse(Game.GetUI().PressEndTurnShortcut());
        Assert.IsFalse(Game.GetUI().PressCallShortcut());
        Assert.AreEqual(first, Game.ActivePlayer);
        yield return null;
    }

    [UnityTest]
    public IEnumerator TheShortcutsAreOffAfterTheGame()
    {
        yield return StartGame();
        Game.GetCommander(1).DamageUnit(999, "test");
        yield return WaitUntil(() => Game.IsGameOver, 5.0f);

        Assert.IsFalse(Game.GetUI().PressEndTurnShortcut());
        Assert.IsFalse(Game.GetUI().PressCallShortcut());
    }

    [UnityTest]
    public IEnumerator TheShortcutsAreOffInTheComputersTurn()
    {
        yield return StartGame(2);
        Time.timeScale = 20.0f;
        yield return WaitUntil(() => Game.ActivePlayer == 2, 20.0f);
        Assert.AreEqual(2, Game.ActivePlayer, "The computer is playing");

        Assert.IsFalse(Game.GetUI().PressEndTurnShortcut());
        Assert.IsFalse(Game.GetUI().PressCallShortcut());
    }

    [UnityTest]
    public IEnumerator TheSettingsOfThePauseMenuHaveTheDifficultyThatCyclesAndIsKept()
    {
        GameSession.Difficulty = AiDifficulty.Easy;
        yield return StartGame();
        EndGameController screen = EndScreen();
        screen.HandleEscape();
        screen.transform.Find("Buttons/SettingsButton").GetComponent<Button>().onClick.Invoke();
        Button difficulty = screen.transform.Find("SettingsPanel/DifficultyButton").GetComponent<Button>();
        TMP_Text label = GetPrivate<TMP_Text>(screen, "_difficultyLabel");
        StringAssert.Contains("Easy", label.text);

        difficulty.onClick.Invoke();
        Assert.AreEqual(AiDifficulty.Normal, GameSession.Difficulty);
        StringAssert.Contains("Normal", label.text);

        difficulty.onClick.Invoke();
        Assert.AreEqual(AiDifficulty.Hard, GameSession.Difficulty);
        StringAssert.Contains("Hard", label.text);

        difficulty.onClick.Invoke();
        Assert.AreEqual(AiDifficulty.Easy, GameSession.Difficulty, "After Hard it starts again with Easy");
        yield return null;
    }

    [UnityTest]
    public IEnumerator TheSettingsAfterTheGameHaveTheDifficultyToo()
    {
        GameSession.Difficulty = AiDifficulty.Normal;
        yield return StartGame(2);
        Game.GetCommander(1).DamageUnit(999, "test");
        yield return WaitUntil(() => Game.IsGameOver, 5.0f);
        yield return new WaitForSecondsRealtime(1.2f);
        EndGameController screen = EndScreen();

        screen.transform.Find("Buttons/SettingsButton").GetComponent<Button>().onClick.Invoke();
        screen.transform.Find("SettingsPanel/DifficultyButton").GetComponent<Button>().onClick.Invoke();

        Assert.AreEqual(AiDifficulty.Hard, GameSession.Difficulty);
        Assert.IsTrue(screen.transform.Find("SettingsPanel/DifficultyButton").gameObject.activeInHierarchy);
    }
}
