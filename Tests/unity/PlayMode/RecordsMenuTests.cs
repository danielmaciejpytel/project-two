using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static GameTestUtil;

// The RECORDS panel of the main menu: leaderboard, history with details, statistics.
public class RecordsMenuTests
{
    [SetUp]
    public void SetUp() => Reset();

    [TearDown]
    public void TearDown() => Reset();

    private static IEnumerator OpenRecords()
    {
        yield return LoadScene("MenuScene");
        Find<Button>("recordsButton").onClick.Invoke();
        yield return null;
    }

    private static RecordsController Panel() => Find<RecordsController>("RecordsPanel");

    [UnityTest]
    public IEnumerator TheRecordsButtonOpensThePanelAndBackClosesIt()
    {
        yield return LoadScene("MenuScene");
        Assert.IsFalse(Panel().gameObject.activeSelf);

        Find<Button>("recordsButton").onClick.Invoke();
        Assert.IsTrue(Panel().gameObject.activeSelf);
        Assert.IsFalse(Find<Button>("playButton").gameObject.activeSelf, "The main buttons make room");
        Assert.AreEqual(RecordsController.Tab.Leaderboard, Panel().CurrentTab, "It opens on the leaderboard");

        Find<Button>("RecordsBackButton").onClick.Invoke();
        Assert.IsFalse(Panel().gameObject.activeSelf);
        Assert.IsTrue(Find<Button>("playButton").gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator WithoutGamesThePanelSaysSo()
    {
        yield return OpenRecords();

        StringAssert.Contains("No won games", Panel().ContentText);
        Panel().ShowTab(RecordsController.Tab.History);
        StringAssert.Contains("No games yet", Panel().ContentText);
        Assert.AreEqual(0, Panel().VisibleHistoryRows);
        Panel().ShowTab(RecordsController.Tab.Stats);
        StringAssert.Contains("No games yet", Panel().ContentText);
    }

    [UnityTest]
    public IEnumerator TheLeaderboardListsTheBestWinsFirstAndCanBeFilteredByDifficulty()
    {
        RecordStore.Add(SampleGame(500, true, "Easy"));
        RecordStore.Add(SampleGame(3000, true, "Hard"));
        RecordStore.Add(SampleGame(1200, true, "Normal"));
        RecordStore.Add(SampleGame(9999, false, "Hard"));
        yield return OpenRecords();

        string text = Panel().ContentText;
        Assert.Less(text.IndexOf("3000"), text.IndexOf("1200"));
        Assert.Less(text.IndexOf("1200"), text.IndexOf("500"));
        StringAssert.DoesNotContain("9999", text, "A lost game is not on the leaderboard");

        Find<Button>("FilterButton").onClick.Invoke();
        Assert.AreEqual(RecordsController.Tab.Leaderboard, Panel().CurrentTab);
        StringAssert.Contains("500", Panel().ContentText);
        StringAssert.DoesNotContain("3000", Panel().ContentText);
    }

    [UnityTest]
    public IEnumerator TheHistoryShowsTheLatestTenAndTheDetailsOfAGame()
    {
        for (int i = 1; i <= 12; i++) RecordStore.Add(SampleGame(i * 100, i % 2 == 0, "Normal", "Turn 1 - Super Hot\n  Super Hot called Striker"));
        yield return OpenRecords();

        Panel().ShowTab(RecordsController.Tab.History);
        Assert.AreEqual(10, Panel().VisibleHistoryRows);
        Assert.IsFalse(Panel().ShowsDetails);

        Find<Button>("Row1").onClick.Invoke();

        Assert.IsTrue(Panel().ShowsDetails);
        StringAssert.Contains("1200", Panel().ContentText, "The newest game comes first");
        StringAssert.Contains("Super Hot called Striker", Panel().ContentText, "The battle log is in the details");
        StringAssert.Contains("Striker", Panel().ContentText, "So are the teams");
        Assert.AreEqual(0, Panel().VisibleHistoryRows, "The list gives way to the details");

        Find<Button>("DetailBackButton").onClick.Invoke();
        Assert.IsFalse(Panel().ShowsDetails);
        Assert.AreEqual(10, Panel().VisibleHistoryRows);
    }

    [UnityTest]
    public IEnumerator TheStatisticsShowTheWinRateAndTheBestScore()
    {
        RecordStore.Add(SampleGame(400, true, "Easy"));
        RecordStore.Add(SampleGame(1300, true, "Hard"));
        RecordStore.Add(SampleGame(100, false, "Hard"));
        yield return OpenRecords();

        Panel().ShowTab(RecordsController.Tab.Stats);

        string text = Panel().ContentText;
        StringAssert.Contains("Games played: 3", text);
        StringAssert.Contains("67% (2/3)", text);
        StringAssert.Contains("Best score: 1300", text);
        StringAssert.Contains("Favorite unit: Striker", text);
    }

    [UnityTest]
    public IEnumerator ClearingTheResultsTakesTwoClicks()
    {
        RecordStore.Add(SampleGame(400, true));
        yield return OpenRecords();

        Find<Button>("ClearButton").onClick.Invoke();
        Assert.AreEqual(1, RecordStore.Load().Count, "The first click only asks");

        Find<Button>("ClearButton").onClick.Invoke();
        Assert.IsEmpty(RecordStore.Load());
        StringAssert.Contains("No won games", Panel().ContentText);
    }

    [UnityTest]
    public IEnumerator SwitchingTabsForgetsAClearThatWasStarted()
    {
        RecordStore.Add(SampleGame(400, true));
        yield return OpenRecords();
        Find<Button>("ClearButton").onClick.Invoke();

        Panel().ShowTab(RecordsController.Tab.Stats);
        Find<Button>("ClearButton").onClick.Invoke();

        Assert.AreEqual(1, RecordStore.Load().Count, "It starts over with a first click");
    }
}
