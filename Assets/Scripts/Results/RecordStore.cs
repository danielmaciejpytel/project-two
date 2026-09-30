using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// The results of finished games, kept in a JSON file in the data folder of the game (the latest 200 games).
public static class RecordStore
{
    public const int MaxRecords = 200;
    public const string FileName = "games.json";
    private const int FormatVersion = 1;

    // Tests use a file of their own, so they never touch the player's results.
    public static string PathOverride { get; set; }

    public static string FilePath => PathOverride ?? Path.Combine(Application.persistentDataPath, FileName);

    [Serializable]
    private class RecordFile
    {
        public int version = FormatVersion;
        public List<GameRecord> games = new List<GameRecord>();
    }

    // All saved games, oldest first. A missing file is an empty list; a damaged one is set aside (.corrupt) and also gives an empty list.
    public static List<GameRecord> Load()
    {
        string path = FilePath;
        if (!File.Exists(path)) return new List<GameRecord>();
        try
        {
            RecordFile file = JsonUtility.FromJson<RecordFile>(File.ReadAllText(path));
            if (file != null && file.games != null) return file.games;
        }
        catch (Exception)
        {
            // Falls through to the damaged file handling.
        }
        SetDamagedFileAside(path);
        return new List<GameRecord>();
    }

    // Adds the game and keeps only the latest MaxRecords.
    public static void Add(GameRecord record)
    {
        if (string.IsNullOrEmpty(record.id)) record.id = Guid.NewGuid().ToString("N");
        List<GameRecord> games = Load();
        games.Add(record);
        if (games.Count > MaxRecords) games.RemoveRange(0, games.Count - MaxRecords);
        Save(games);
    }

    public static void Clear()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
    }

    // Won games against the computer, best score first (the leaderboard), optionally only one difficulty.
    public static List<GameRecord> Leaderboard(string difficulty = null, int count = 10)
    {
        List<GameRecord> wins = new List<GameRecord>();
        foreach (GameRecord game in Load())
        {
            if (!game.HumanWon) continue;
            if (!string.IsNullOrEmpty(difficulty) && game.difficulty != difficulty) continue;
            wins.Add(game);
        }
        // Equal scores: the later game first.
        wins.Sort((a, b) => b.HumanScore != a.HumanScore ? b.HumanScore.CompareTo(a.HumanScore) : string.CompareOrdinal(b.date, a.date));
        if (wins.Count > count) wins.RemoveRange(count, wins.Count - count);
        return wins;
    }

    // Place of a saved game on the leaderboard of all difficulties (1 = best), 0 when it is not on it.
    public static int LeaderboardPlace(string gameId)
    {
        List<GameRecord> board = Leaderboard(null, 10);
        for (int i = 0; i < board.Count; i++)
        {
            if (board[i].id == gameId) return i + 1;
        }
        return 0;
    }

    // The latest games, newest first.
    public static List<GameRecord> Latest(int count = 10)
    {
        List<GameRecord> games = Load();
        games.Reverse();
        if (games.Count > count) games.RemoveRange(count, games.Count - count);
        return games;
    }

    public static RecordStats Stats() => RecordStats.From(Load());

    private static void Save(List<GameRecord> games)
    {
        try
        {
            string path = FilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(new RecordFile { games = games }, true));
            File.Copy(temporary, path, true);
            File.Delete(temporary);
        }
        catch (Exception exception)
        {
            // A game must never fail because its result could not be saved.
            Debug.LogWarning("Could not save the results: " + exception.Message);
        }
    }

    private static void SetDamagedFileAside(string path)
    {
        try
        {
            File.Copy(path, path + ".corrupt", true);
            File.Delete(path);
        }
        catch (Exception)
        {
            // The next save overwrites it anyway.
        }
    }
}
