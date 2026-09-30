using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

// The results file, the leaderboard and the statistics. Every test uses a file of its own.
public class RecordStoreTests
{
    private string _path;

    [SetUp]
    public void SetUp()
    {
        _path = Path.Combine(Application.temporaryCachePath, "record-store-test-" + System.Guid.NewGuid().ToString("N") + ".json");
        RecordStore.PathOverride = _path;
    }

    [TearDown]
    public void TearDown()
    {
        RecordStore.PathOverride = null;
        foreach (string file in new[] { _path, _path + ".corrupt", _path + ".tmp" })
        {
            if (File.Exists(file)) File.Delete(file);
        }
    }

    // A game against the computer as the human (team 1) played it.
    private static GameRecord VsComputer(int humanScore, bool won, string difficulty = "Normal", string date = null, string[] units = null)
    {
        GameRecord record = new GameRecord
        {
            mode = GameRecord.VsComputer,
            difficulty = difficulty,
            humanTeam = 1,
            winner = won ? 1 : 2,
            date = date ?? System.DateTime.UtcNow.ToString("o"),
            turns = 12
        };
        record.team1.score = humanScore;
        record.team1.units = units ?? new[] { "Super Hot", "Assailant" };
        record.team1.killed = 2;
        record.team1.called = 1;
        return record;
    }

    [Test]
    public void AMissingFileIsAnEmptyList()
    {
        Assert.IsEmpty(RecordStore.Load());
    }

    [Test]
    public void ASavedGameIsReadBackWithAllItsData()
    {
        GameRecord record = VsComputer(1234, true);
        record.log = "Turn 1 - Super Hot\n  Super Hot called Assailant";
        record.team2.units = new[] { "Super Cold", "Striker" };

        RecordStore.Add(record);
        GameRecord loaded = RecordStore.Load().Single();

        Assert.IsNotEmpty(loaded.id);
        Assert.AreEqual(1234, loaded.HumanScore);
        Assert.IsTrue(loaded.HumanWon);
        Assert.AreEqual("Normal", loaded.difficulty);
        Assert.AreEqual(12, loaded.turns);
        Assert.AreEqual("Turn 1 - Super Hot\n  Super Hot called Assailant", loaded.log);
        CollectionAssert.AreEqual(new[] { "Super Cold", "Striker" }, loaded.team2.units);
    }

    [Test]
    public void OnlyTheLatest200GamesAreKept()
    {
        for (int i = 0; i < RecordStore.MaxRecords + 5; i++) RecordStore.Add(VsComputer(i, true));

        var games = RecordStore.Load();

        Assert.AreEqual(RecordStore.MaxRecords, games.Count);
        Assert.AreEqual(5, games[0].HumanScore, "The oldest five are gone");
        Assert.AreEqual(RecordStore.MaxRecords + 4, games[games.Count - 1].HumanScore);
    }

    [Test]
    public void ADamagedFileIsSetAsideAndDoesNotStopTheGame()
    {
        File.WriteAllText(_path, "{ this is not json");

        Assert.IsEmpty(RecordStore.Load());
        Assert.IsTrue(File.Exists(_path + ".corrupt"), "The damaged file is kept for a look");
        Assert.IsFalse(File.Exists(_path));

        RecordStore.Add(VsComputer(10, true));
        Assert.AreEqual(1, RecordStore.Load().Count);
    }

    [Test]
    public void ClearingRemovesEverything()
    {
        RecordStore.Add(VsComputer(10, true));

        RecordStore.Clear();

        Assert.IsEmpty(RecordStore.Load());
    }

    [Test]
    public void TheLeaderboardHasOnlyWonGamesAgainstTheComputerBestFirst()
    {
        RecordStore.Add(VsComputer(500, true));
        RecordStore.Add(VsComputer(900, false));
        RecordStore.Add(VsComputer(1500, true));
        RecordStore.Add(new GameRecord { mode = GameRecord.TwoPlayers, winner = 1 });
        RecordStore.Add(VsComputer(1000, true));

        var board = RecordStore.Leaderboard();

        CollectionAssert.AreEqual(new[] { 1500, 1000, 500 }, board.Select(g => g.HumanScore).ToArray());
    }

    [Test]
    public void TheLeaderboardHasTheTopTenAndCanBeFilteredByDifficulty()
    {
        for (int i = 0; i < 12; i++) RecordStore.Add(VsComputer(100 + i, true, i % 2 == 0 ? "Easy" : "Hard"));

        Assert.AreEqual(10, RecordStore.Leaderboard().Count);
        var easy = RecordStore.Leaderboard("Easy");
        Assert.AreEqual(6, easy.Count);
        Assert.IsTrue(easy.All(g => g.difficulty == "Easy"));
        Assert.AreEqual(110, easy[0].HumanScore);
    }

    [Test]
    public void AGameKnowsItsPlaceOnTheLeaderboard()
    {
        RecordStore.Add(VsComputer(1000, true));
        GameRecord newest = VsComputer(2000, true);
        RecordStore.Add(newest);
        GameRecord lost = VsComputer(5000, false);
        RecordStore.Add(lost);

        Assert.AreEqual(1, RecordStore.LeaderboardPlace(newest.id));
        Assert.AreEqual(0, RecordStore.LeaderboardPlace(lost.id), "A lost game is not on the leaderboard");
    }

    [Test]
    public void TheLatestGamesComeNewestFirst()
    {
        for (int i = 1; i <= 12; i++) RecordStore.Add(VsComputer(i, true));

        var latest = RecordStore.Latest(10);

        Assert.AreEqual(10, latest.Count);
        Assert.AreEqual(12, latest[0].HumanScore);
        Assert.AreEqual(3, latest[9].HumanScore);
    }

    [Test]
    public void TheStatisticsCountWinsStreaksAndTheFavoriteUnit()
    {
        RecordStore.Add(VsComputer(100, true, "Easy", units: new[] { "Super Hot", "Striker", "Assailant" }));
        RecordStore.Add(VsComputer(300, true, "Normal", units: new[] { "Super Hot", "Striker" }));
        RecordStore.Add(VsComputer(50, false, "Normal", units: new[] { "Super Hot", "Repeater" }));
        RecordStore.Add(VsComputer(700, true, "Hard", units: new[] { "Super Hot", "Striker" }));
        RecordStore.Add(new GameRecord { mode = GameRecord.TwoPlayers, winner = 2 });

        RecordStats stats = RecordStore.Stats();

        Assert.AreEqual(5, stats.Games);
        Assert.AreEqual(4, stats.GamesVsComputer);
        Assert.AreEqual(3, stats.WinsVsComputer);
        Assert.AreEqual(0.75f, stats.WinRate, 0.001f);
        Assert.AreEqual(700, stats.BestScore);
        Assert.AreEqual(2, stats.LongestWinStreak);
        Assert.AreEqual(1, stats.CurrentWinStreak);
        Assert.AreEqual("Striker", stats.FavoriteUnit);
        Assert.AreEqual(0.5f, stats.WinRateFor("Normal"), 0.001f);
        Assert.AreEqual(1.0f, stats.WinRateFor("Hard"), 0.001f);
        Assert.AreEqual(0.0f, stats.WinRateFor("Nothing"));
    }

    [Test]
    public void NoGamesGiveEmptyStatistics()
    {
        RecordStats stats = RecordStore.Stats();

        Assert.AreEqual(0, stats.Games);
        Assert.AreEqual(0.0f, stats.WinRate);
        Assert.AreEqual("", stats.FavoriteUnit);
    }
}
