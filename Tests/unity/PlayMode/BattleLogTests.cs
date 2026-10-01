using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static GameTestUtil;

// The battle log: what it remembers and how it scrolls.
public class BattleLogTests
{
    [SetUp]
    public void SetUp() => Reset();

    [TearDown]
    public void TearDown() => Reset();

    private static BattleLogController Log() => Find<BattleLogController>("BattleLog");

    private static RectTransform Entries(BattleLogController log) => GetPrivate<RectTransform>(log, "_entriesRect");

    private static void AddEntries(int count)
    {
        for (int i = 0; i < count; i++) EventManager.Instance.UnitDeployed(Game.Units[i % Game.Units.Count]);
    }

    [UnityTest]
    public IEnumerator TheLogListsTheLatestActionsUnderTheTurnHeader()
    {
        yield return StartGame();
        UnitController unit = UnitToCall(Game.ActivePlayer);

        EventManager.Instance.UnitDeployed(unit);

        string text = GetPrivate<TMPro.TMP_Text>(Log(), "_entriesText").text;
        StringAssert.Contains(unit.GetUnitName(), text);
        StringAssert.Contains("Turn 1", text);
    }

    [UnityTest]
    public IEnumerator TheLogRemembersMoreThanItShows()
    {
        yield return StartGame();

        AddEntries(20);

        BattleLogController log = Log();
        float visibleHeight = ((RectTransform)GetPrivate<GameObject>(log, "_body").transform).rect.height;
        Assert.Greater(Entries(log).rect.height, visibleHeight, "Older entries are kept above the visible part");
    }

    [UnityTest]
    public IEnumerator ScrollingStaysInsideTheContent()
    {
        yield return StartGame();
        AddEntries(20);
        BattleLogController log = Log();

        CallPrivate(log, "ScrollBy", -1000.0f);
        Assert.AreEqual(0.0f, Entries(log).anchoredPosition.y, 0.01f, "Not above the oldest entry");

        CallPrivate(log, "ScrollBy", 1000.0f);
        float bottom = Entries(log).rect.height - ((RectTransform)GetPrivate<GameObject>(log, "_body").transform).rect.height;
        Assert.AreEqual(Mathf.Round(bottom), Entries(log).anchoredPosition.y, 1.0f, "Not below the newest entry");
        yield return null;
    }

    [UnityTest]
    public IEnumerator ANewEntryScrollsBackToTheLatest()
    {
        yield return StartGame();
        AddEntries(20);
        BattleLogController log = Log();
        CallPrivate(log, "ScrollBy", -1000.0f);
        Assert.AreEqual(0.0f, Entries(log).anchoredPosition.y, 0.01f);

        AddEntries(1);

        float bottom = Entries(log).rect.height - ((RectTransform)GetPrivate<GameObject>(log, "_body").transform).rect.height;
        Assert.Greater(bottom, 0.0f);
        Assert.AreEqual(Mathf.Round(bottom), Entries(log).anchoredPosition.y, 1.0f, "The player is taken to the new action");
        yield return null;
    }

    [UnityTest]
    public IEnumerator TheLogSitsUnderTheButtonsWithTheSameWidth()
    {
        yield return StartGame();

        RectTransform log = (RectTransform)Log().transform;
        RectTransform call = (RectTransform)Button("DeployMinionButton").transform;
        RectTransform endTurn = (RectTransform)Button("EndTurnButton").transform;

        Assert.AreEqual(HudLayout.ColumnWidth, log.rect.width, 0.5f, "As wide as the column of the buttons");
        Assert.AreEqual(endTurn.anchoredPosition.x - endTurn.rect.width / 2.0f, log.anchoredPosition.x - log.rect.width / 2.0f, 1.0f, "Same left edge as the main button");
        Assert.Less(log.anchoredPosition.y, call.anchoredPosition.y, "Under the buttons");
        yield return null;
    }

    [UnityTest]
    public IEnumerator CallAndEndTurnTogetherAreAsWideAsTheLog()
    {
        yield return StartGame();
        RectTransform log = (RectTransform)Log().transform;
        RectTransform call = (RectTransform)Button("DeployMinionButton").transform;
        RectTransform endTurn = (RectTransform)Button("EndTurnButton").transform;

        float left = Mathf.Min(call.anchoredPosition.x - call.rect.width / 2.0f, endTurn.anchoredPosition.x - endTurn.rect.width / 2.0f);
        float right = Mathf.Max(call.anchoredPosition.x + call.rect.width / 2.0f, endTurn.anchoredPosition.x + endTurn.rect.width / 2.0f);

        Assert.AreEqual(log.rect.width, right - left, 1.0f);
        Assert.AreEqual(log.anchoredPosition.x, (left + right) / 2.0f, 1.0f, "Centred on the log");
        yield return null;
    }
}
