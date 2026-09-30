using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static GameTestUtil;

// The end screen after the game and the pause menu (Escape): they are the same overlay.
public class EndScreenTests
{
    [SetUp]
    public void SetUp() => Reset();

    [TearDown]
    public void TearDown() => Reset();

    private static IEnumerator EndTheGameNow()
    {
        Game.GetCommander(1).DamageUnit(999, "test");
        // The commander dies with an animation, then the end screen fades in.
        yield return WaitUntil(() => Game.IsGameOver, 5.0f);
        yield return new WaitForSecondsRealtime(1.2f);
    }

    private static IEnumerator EndTheGame()
    {
        yield return StartGame();
        yield return EndTheGameNow();
    }

    [UnityTest]
    public IEnumerator KillingACommanderEndsTheGameAndShowsTheEndScreen()
    {
        yield return EndTheGame();

        Assert.IsTrue(Game.IsGameOver);
        Assert.IsInstanceOf<EndState>(Game.CurrentState);
        EndGameController screen = EndScreen();
        Assert.IsTrue(screen.gameObject.activeSelf);
        foreach (string name in new[] { "PlayAgainButton", "BackToMenuButton", "SettingsButton", "QuitGameButton" })
        {
            Assert.IsTrue(screen.transform.Find("Buttons/" + name).gameObject.activeInHierarchy, name);
        }
        Assert.IsFalse(Button("EndTurnButton").gameObject.activeSelf, "Nothing is left to play");
        Assert.IsFalse(Button("DeployMinionButton").gameObject.activeSelf);
        Assert.IsNotNull(screen.transform.Find("WinnerBackgroundImage"), "The winner banner is above the dimming");
        Assert.AreEqual(1.0f, Time.timeScale, "The end screen does not stop the game");
    }

    [UnityTest]
    public IEnumerator TheBannerIsAsWideAsTheSettingsPanelAndTheButtonsAsTheBanner()
    {
        yield return EndTheGame();
        EndGameController screen = EndScreen();
        RectTransform banner = (RectTransform)screen.transform.Find("WinnerBackgroundImage");
        RectTransform settings = (RectTransform)screen.transform.Find("SettingsPanel");
        RectTransform again = (RectTransform)screen.transform.Find("Buttons/PlayAgainButton");
        RectTransform menu = (RectTransform)screen.transform.Find("Buttons/BackToMenuButton");

        // The settings panel image has a transparent margin, so the banner is a little narrower than its rect.
        Assert.AreEqual(settings.rect.width * 792.0f / 812.0f, banner.rect.width, 1.0f);
        float left = again.anchoredPosition.x - again.rect.width / 2.0f;
        float right = menu.anchoredPosition.x + menu.rect.width / 2.0f;
        Assert.AreEqual(banner.rect.width, right - left, 1.5f, "The two button columns span the banner");
        Assert.AreEqual(0.0f, banner.anchoredPosition.x, 0.5f, "The banner is centred");
    }

    [UnityTest]
    public IEnumerator SettingsReplacesTheButtonsAndBackBringsThemBack()
    {
        yield return EndTheGame();
        EndGameController screen = EndScreen();

        screen.transform.Find("Buttons/SettingsButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsTrue(screen.transform.Find("SettingsPanel").gameObject.activeSelf);
        Assert.IsFalse(screen.transform.Find("Buttons").gameObject.activeSelf);

        screen.transform.Find("SettingsPanel/BackButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(screen.transform.Find("SettingsPanel").gameObject.activeSelf);
        Assert.IsTrue(screen.transform.Find("Buttons").gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator PlayAgainStartsANewDraftInTheSameMode()
    {
        yield return StartGame(2);
        yield return EndTheGameNow();

        EndScreen().transform.Find("Buttons/PlayAgainButton").GetComponent<Button>().onClick.Invoke();
        yield return new WaitForSecondsRealtime(2.0f);

        Assert.AreEqual("MainScene", SceneManager.GetActiveScene().name);
        Assert.IsNull(Game.CurrentState, "A new game starts with the unit draft");
        Assert.IsFalse(Game.IsGameOver);
        Assert.AreEqual(2, GameSession.AiPlayerId, "The mode is kept");
        Assert.AreEqual(1.0f, Time.timeScale);
    }

    [UnityTest]
    public IEnumerator BackToMenuLoadsTheMenu()
    {
        yield return EndTheGame();

        EndScreen().transform.Find("Buttons/BackToMenuButton").GetComponent<Button>().onClick.Invoke();
        yield return new WaitForSecondsRealtime(2.0f);

        Assert.AreEqual("MenuScene", SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator EscapeOpensThePauseMenuAndStopsTheGame()
    {
        yield return StartGame();
        EndGameController screen = EndScreen();
        Assert.IsFalse(screen.gameObject.activeSelf);

        screen.HandleEscape();
        yield return null;

        Assert.IsTrue(screen.gameObject.activeSelf);
        Assert.AreEqual(0.0f, Time.timeScale, "The game stops");
        Assert.IsNull(screen.transform.Find("WinnerBackgroundImage"), "The pause menu has no winner banner");
        Assert.IsTrue(screen.transform.Find("Buttons/PlayAgainButton").gameObject.activeInHierarchy);

        screen.HandleEscape();
        Assert.IsFalse(screen.gameObject.activeSelf);
        Assert.AreEqual(1.0f, Time.timeScale, "Escape again resumes the game");
    }

    [UnityTest]
    public IEnumerator EscapeInTheSettingsGoesBackToTheButtonsFirst()
    {
        yield return StartGame();
        EndGameController screen = EndScreen();
        screen.HandleEscape();
        screen.transform.Find("Buttons/SettingsButton").GetComponent<Button>().onClick.Invoke();

        screen.HandleEscape();
        Assert.IsTrue(screen.gameObject.activeSelf, "Still paused");
        Assert.IsTrue(screen.transform.Find("Buttons").gameObject.activeSelf);

        screen.HandleEscape();
        Assert.IsFalse(screen.gameObject.activeSelf);
        Assert.AreEqual(1.0f, Time.timeScale);
        yield return null;
    }

    [UnityTest]
    public IEnumerator PausingStopsTheTurnTimer()
    {
        yield return StartGame();
        UIController ui = Game.GetUI();
        EndScreen().HandleEscape();
        int before = GetPrivate<int>(ui, "_myTimer");

        yield return new WaitForSecondsRealtime(2.5f);

        Assert.AreEqual(before, GetPrivate<int>(ui, "_myTimer"));
        EndScreen().HandleEscape();
    }
}
