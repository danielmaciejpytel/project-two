using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static GameTestUtil;

// A finished game is scored and saved, and the end screen shows the score and the place on the leaderboard.
public class ResultsTests
{
    private AiDifficulty _savedDifficulty;

    [SetUp]
    public void SetUp()
    {
        Reset();
        _savedDifficulty = GameSession.Difficulty;
        GameSession.Difficulty = AiDifficulty.Normal;
    }

    [TearDown]
    public void TearDown()
    {
        GameSession.Difficulty = _savedDifficulty;
        Reset();
    }

    private static IEnumerator KillACommanderAndWait(int playerId)
    {
        Game.GetCommander(playerId).DamageUnit(999, "test");
        yield return WaitUntil(() => Game.IsGameOver, 5.0f);
        yield return new WaitForSecondsRealtime(1.2f);
    }

    private static string SummaryText() => GetPrivate<TMPro.TMP_Text>(EndScreen(), "_summaryText").text;

    // The record of the game (a new high score, the place on the leaderboard) is written under the summary.
    private static string RecordText() => GetPrivate<TMPro.TMP_Text>(EndScreen(), "_recordText").text;

    [UnityTest]
    public IEnumerator AWonGameAgainstTheComputerIsSavedWithItsScoreAndPlace()
    {
        yield return StartGame(2);

        yield return KillACommanderAndWait(2);

        var saved = RecordStore.Load();
        Assert.AreEqual(1, saved.Count);
        GameRecord record = saved[0];
        Assert.AreEqual(GameRecord.VsComputer, record.mode);
        Assert.AreEqual("Normal", record.difficulty);
        Assert.AreEqual(1, record.humanTeam);
        Assert.AreEqual(1, record.winner);
        Assert.IsTrue(record.HumanWon);
        Assert.GreaterOrEqual(record.HumanScore, ScoreCalculator.WinPoints, "A win is worth at least the points for the win");
        Assert.AreEqual(1, Game.Result.LeaderboardPlace);
        string text = SummaryText();
        StringAssert.Contains("Score", text);
        StringAssert.Contains("New high score!", RecordText());
    }

    [UnityTest]
    public IEnumerator TheSavedScoreFollowsTheFormula()
    {
        yield return StartGame(2);
        yield return KillACommanderAndWait(2);

        GameRecord record = RecordStore.Load().Single();
        TeamRecord human = record.team1;

        int expected = ScoreCalculator.Score(true, human.killed, human.lost, record.turns, human.commanderHealth, ScoreCalculator.DifficultyMultiplier(AiDifficulty.Normal));
        Assert.AreEqual(expected, human.score);
        Assert.Greater(human.commanderHealth, 0, "The winner's commander is alive");
        Assert.AreEqual(0, record.team2.commanderHealth, "The killed commander has no health");
        Assert.AreEqual(1, human.killed, "The commander that was killed is a kill");
    }

    [UnityTest]
    public IEnumerator ALostGameIsSavedButIsNotOnTheLeaderboard()
    {
        yield return StartGame(2);

        yield return KillACommanderAndWait(1);

        GameRecord record = RecordStore.Load().Single();
        Assert.AreEqual(2, record.winner);
        Assert.IsFalse(record.HumanWon);
        Assert.AreEqual(0, Game.Result.LeaderboardPlace);
        Assert.IsEmpty(RecordStore.Leaderboard());
        string text = SummaryText();
        StringAssert.Contains("Score", text);
        StringAssert.DoesNotContain("high score", RecordText());
        StringAssert.DoesNotContain("Leaderboard place", RecordText());
    }

    [UnityTest]
    public IEnumerator AGameOfTwoPlayersIsOnlyInTheHistory()
    {
        yield return StartGame();

        yield return KillACommanderAndWait(1);

        GameRecord record = RecordStore.Load().Single();
        Assert.AreEqual(GameRecord.TwoPlayers, record.mode);
        Assert.AreEqual("", record.difficulty);
        Assert.IsEmpty(RecordStore.Leaderboard());
        Assert.AreEqual(0, Game.Result.LeaderboardPlace);
        StringAssert.Contains("Score", SummaryText());
    }

    [UnityTest]
    public IEnumerator AGoodScoreTakesAPlaceOnTheLeaderboard()
    {
        RecordStore.Add(SampleGame(99999, true));
        RecordStore.Add(SampleGame(50, true));
        yield return StartGame(2);

        yield return KillACommanderAndWait(2);

        int place = Game.Result.LeaderboardPlace;
        Assert.AreEqual(2, place, "Between the 99999 and the 50");
        StringAssert.Contains("Leaderboard place 2", RecordText());
        StringAssert.DoesNotContain("New high score!", RecordText());
    }

    [UnityTest]
    public IEnumerator TheSavedGameHasTheUnitsOfBothTeamsAndTheBattleLog()
    {
        yield return StartGame(2);
        int human = 1;
        // The computer may have the first turn; the human calls a unit in the first turn of their own.
        Time.timeScale = 20.0f;
        yield return WaitUntil(() => Game.ActivePlayer == human && Game.CurrentState is BeginTurnState, 20.0f);
        Time.timeScale = 1.0f;
        UnitController unit = UnitToCall(human);
        Game.DeployAction();
        EventManager.Instance.UnitClicked(unit);
        EventManager.Instance.TileClicked(FreeTileNextToCommander(human));
        yield return null;

        yield return KillACommanderAndWait(2);

        GameRecord record = RecordStore.Load().Single();
        Assert.AreEqual(Game.GetCommander(1).GetUnitName(), record.team1.units[0], "The commander is first");
        Assert.AreEqual(Game.GetCommander(2).GetUnitName(), record.team2.units[0]);
        CollectionAssert.Contains(record.team1.units, unit.GetUnitName());
        Assert.AreEqual(1, record.team1.called);
        StringAssert.Contains("called " + unit.GetUnitName(), record.log);
        StringAssert.Contains("Turn ", record.log);
        Assert.Greater(record.turns, 0);
        Assert.Greater(record.seconds, 0.0f);
    }

    [UnityTest]
    public IEnumerator AGameThatIsLeftIsNotSaved()
    {
        yield return StartGame(2);
        EndGameController screen = EndScreen();
        screen.HandleEscape();
        screen.transform.Find("Buttons/BackToMenuButton").GetComponent<Button>().onClick.Invoke();
        screen.transform.Find("ConfirmPanel/YesButton").GetComponent<Button>().onClick.Invoke();
        yield return new WaitForSecondsRealtime(2.0f);

        Assert.IsEmpty(RecordStore.Load());
    }

    // Left and right ends (in world x) of the characters of a line of a text, without the space around them.
    private static void LineEnds(TMPro.TMP_Text text, int line, out float left, out float right)
    {
        text.ForceMeshUpdate();
        TMPro.TMP_LineInfo info = text.textInfo.lineInfo[line];
        left = float.MaxValue;
        right = float.MinValue;
        for (int i = info.firstVisibleCharacterIndex; i <= info.lastVisibleCharacterIndex; i++)
        {
            TMPro.TMP_CharacterInfo c = text.textInfo.characterInfo[i];
            if (!c.isVisible) continue;
            left = Mathf.Min(left, text.transform.TransformPoint(c.bottomLeft).x);
            right = Mathf.Max(right, text.transform.TransformPoint(c.topRight).x);
        }
    }

    [UnityTest]
    public IEnumerator TheSummaryHasTheTeamNumbersInTwoColumnsInsideItsPanel()
    {
        yield return StartGame();
        yield return KillACommanderAndWait(1);
        TMPro.TMP_Text summary = GetPrivate<TMPro.TMP_Text>(EndScreen(), "_summaryText");
        RectTransform panel = (RectTransform)EndScreen().transform.Find("Buttons/SummaryPanel");
        summary.ForceMeshUpdate();
        Assert.GreaterOrEqual(summary.textInfo.lineCount, 4);
        // World distances grow with the size of the Game view, the tolerances are in canvas units.
        float unit = summary.canvas.rootCanvas.scaleFactor;
        Vector3[] corners = new Vector3[4];
        panel.GetWorldCorners(corners);

        float[] labelStarts = new float[4];
        float[] secondEnds = new float[3];
        for (int line = 0; line < 4; line++)
        {
            LineEnds(summary, line, out float left, out float right);
            labelStarts[line] = left;
            Assert.Greater(left, corners[0].x, "Inside the panel, line " + line);
            Assert.Less(right, corners[2].x, "Inside the panel, line " + line);
            if (line > 0) secondEnds[line - 1] = right;
        }
        for (int line = 1; line < 4; line++) Assert.AreEqual(labelStarts[0], labelStarts[line], 1.0f * unit, "The labels start at one edge");
        Assert.AreEqual(secondEnds[0], secondEnds[1], 1.0f * unit, "The second numbers end at one edge");
        Assert.AreEqual(secondEnds[0], secondEnds[2], 1.0f * unit, "The second numbers end at one edge");
    }
}
