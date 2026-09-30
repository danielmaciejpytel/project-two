using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using static GameTestUtil;

// Hints for the first turn of each player and the warning before the turn runs out.
public class HintsAndWarningTests
{
    [SetUp]
    public void SetUp() => Reset();

    [TearDown]
    public void TearDown() => Reset();

    private static GameObject HintPanel() => GetPrivate<GameObject>(Game.GetUI(), "_hintPanel");

    private static string HintText() => GetPrivate<TMP_Text>(Game.GetUI(), "_hintText").text;

    [UnityTest]
    public IEnumerator TheFirstTurnShowsAHintThatFollowsTheStepsOfTheTurn()
    {
        yield return StartGame();
        Assert.IsTrue(HintPanel().activeSelf);
        StringAssert.Contains("Click one of your units", HintText());

        Game.DeployAction();
        StringAssert.Contains("Pick a card", HintText());

        Game.DeployAction();
        StringAssert.Contains("Click one of your units", HintText());

        EventManager.Instance.UnitClicked(Game.GetCommander(Game.ActivePlayer));
        StringAssert.Contains("Click a highlighted tile", HintText());
        yield return null;
    }

    [UnityTest]
    public IEnumerator EachPlayerGetsHintsInTheFirstTurnAndNotLater()
    {
        yield return StartGame();
        Assert.IsTrue(HintPanel().activeSelf, "First turn of the game");

        Game.EndTurnAction();
        Assert.IsTrue(HintPanel().activeSelf, "First turn of the other player");

        Game.EndTurnAction();
        Assert.IsFalse(HintPanel().activeSelf, "The third turn has no hints");

        Game.DeployAction();
        Assert.IsFalse(HintPanel().activeSelf, "Nor do the steps of a later turn");
        yield return null;
    }

    [UnityTest]
    public IEnumerator TheComputersTurnHasNoHints()
    {
        yield return StartGame(2);
        int human = 1;
        Time.timeScale = 20.0f;
        if (Game.ActivePlayer != human)
        {
            // The computer starts: its turn is over quickly, wait for the human's.
            yield return WaitUntil(() => Game.ActivePlayer == human && Game.CurrentState is BeginTurnState, 20.0f);
        }
        Assert.IsTrue(HintPanel().activeSelf, "The human sees the hint");

        Game.EndTurnAction();
        yield return null;

        Assert.AreEqual(2, Game.ActivePlayer);
        Assert.IsFalse(HintPanel().activeSelf, "The computer needs no hint");
    }

    [UnityTest]
    public IEnumerator TheLastSecondsAreShownInTheWarningColor()
    {
        yield return StartGame();
        UIController ui = Game.GetUI();
        TMP_Text seconds = GetPrivate<TMP_Text>(ui, "_timerText");
        Color normal = seconds.color;
        Color warning = GetPrivate<Color>(ui, "_warningColor");
        Assert.AreNotEqual(normal, warning);

        SetPrivate(ui, "_myTimer", 60 - 12);
        yield return new WaitForSecondsRealtime(1.3f);
        Assert.AreEqual(normal, seconds.color, "More than 10 seconds left: normal color");

        SetPrivate(ui, "_myTimer", 60 - 8);
        yield return new WaitForSecondsRealtime(1.3f);
        Assert.AreEqual(warning, seconds.color, "8 seconds left: warning color");

        Game.EndTurnAction();
        yield return null;
        Assert.AreEqual(normal, seconds.color, "The next turn starts in the normal color");
    }
}
