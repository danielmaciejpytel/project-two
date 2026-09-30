using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

// Helpers shared by the PlayMode tests: loading the game scene, choosing units, finding HUD elements and
// reaching into private state the tests have to check.
public static class GameTestUtil
{
    private const BindingFlags NonPublic = BindingFlags.NonPublic | BindingFlags.Instance;

    public static GameController Game => GameController.Instance;

    public static IEnumerator LoadScene(string name)
    {
#if UNITY_EDITOR
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/" + name + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
        yield return SceneManager.LoadSceneAsync(name);
#endif
        yield return null;
    }

    // Loads the game scene and plays the unit draft through, so the first turn is about to start.
    public static IEnumerator StartGame(int aiPlayer = GameSession.NoAi)
    {
        GameSession.AiPlayerId = aiPlayer;
        yield return LoadScene("MainScene");
        UnitChoiceController choice = UnityEngine.Object.FindFirstObjectByType<UnitChoiceController>();
        Assert.IsNotNull(choice, "The unit draft is missing from MainScene");
        MethodInfo step = typeof(UnitChoiceController).GetMethod("Step", NonPublic);
        MethodInfo confirm = typeof(UnitChoiceController).GetMethod("ConfirmPick", NonPublic);
        for (int i = 0; i < 20 && Game.CurrentState == null; i++)
        {
            step.Invoke(choice, new object[] { 1 });
            confirm.Invoke(choice, null);
            yield return null;
        }
        Assert.IsNotNull(Game.CurrentState, "The draft did not lead to the game");
        yield return null;
    }

    public static void Reset()
    {
        Time.timeScale = 1.0f;
        GameSession.AiPlayerId = GameSession.NoAi;
        // Finished games are saved: the tests must never write into the player's results.
        RecordStore.PathOverride = System.IO.Path.Combine(Application.temporaryCachePath, "play-mode-tests-results.json");
        RecordStore.Clear();
    }

    // A finished game against the computer, played by the human as team 1, for the tests of the results.
    public static GameRecord SampleGame(int humanScore, bool won, string difficulty = "Normal", string log = "")
    {
        GameRecord record = new GameRecord
        {
            mode = GameRecord.VsComputer,
            difficulty = difficulty,
            humanTeam = 1,
            winner = won ? 1 : 2,
            date = System.DateTime.UtcNow.ToString("o"),
            turns = 12,
            seconds = 200.0f,
            log = log
        };
        record.team1.units = new[] { "Super Hot", "Striker" };
        record.team2.units = new[] { "Super Cold", "Tormentor" };
        record.team1.score = humanScore;
        record.team1.killed = 2;
        record.team1.called = 1;
        return record;
    }

    public static T Find<T>(string name) where T : Component
    {
        foreach (T candidate in Resources.FindObjectsOfTypeAll<T>())
        {
            if (candidate.name == name && candidate.gameObject.scene.IsValid()) return candidate;
        }
        Assert.Fail("Not found in the scene: " + name);
        return null;
    }

    public static Button Button(string name) => Find<Button>(name);

    public static EndGameController EndScreen() => Find<EndGameController>("EndGameOverlay");

    public static UnitController UnitToCall(int playerId)
    {
        foreach (UnitController unit in Game.Units)
        {
            if (unit.GetPlayerId() == playerId && !unit.IsDeployed && !unit.IsKilled) return unit;
        }
        return null;
    }

    // An empty tile next to the commander, where a called unit can stand.
    public static TileController FreeTileNextToCommander(int playerId)
    {
        GridPosition center = Game.GetCommander(playerId).CurrentTile.GetGridPosition();
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                TileController tile = Game.GetGrid().GetTile(center.x + dx, center.y + dy);
                if (tile != null && !tile.IsOccupied && tile.isWalkable()) return tile;
            }
        }
        return null;
    }

    // A tile far from both commanders, where a click cannot mean a deployment.
    public static TileController FarTile() => Game.GetGrid().GetTile(Game.GetGrid().GetBoardWidth() / 2, Game.GetGrid().GetBoardHeight() / 2);

    public static bool IsOnLeftHalf(RectTransform rect)
    {
        Canvas canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
        return rect.position.x < canvas.transform.position.x;
    }

    public static T GetPrivate<T>(object target, string field) => (T)target.GetType().GetField(field, NonPublic).GetValue(target);

    public static void SetPrivate(object target, string field, object value) => target.GetType().GetField(field, NonPublic).SetValue(target, value);

    public static object CallPrivate(object target, string method, params object[] args) => target.GetType().GetMethod(method, NonPublic).Invoke(target, args);

    public static Color OverlayColor(TileController tile) => GetPrivate<SpriteRenderer>(tile, "_overlayColorSpriteRenderer").color;

    // Number of tiles whose highlight differs from the given snapshot.
    public static int ChangedTiles(Color[,] before)
    {
        int changed = 0;
        BoardGrid grid = Game.GetGrid();
        for (int y = 0; y < grid.GetBoardHeight(); y++)
        {
            for (int x = 0; x < grid.GetBoardWidth(); x++)
            {
                if (OverlayColor(grid.GetTile(x, y)) != before[x, y]) changed++;
            }
        }
        return changed;
    }

    // Tiles that show an attack crosshair.
    public static int CrosshairTiles()
    {
        int count = 0;
        BoardGrid grid = Game.GetGrid();
        for (int y = 0; y < grid.GetBoardHeight(); y++)
        {
            for (int x = 0; x < grid.GetBoardWidth(); x++)
            {
                if (GetPrivate<SpriteRenderer>(grid.GetTile(x, y), "_crosshairSpriteRenderer").enabled) count++;
            }
        }
        return count;
    }

    public static Color[,] SnapshotOverlays()
    {
        BoardGrid grid = Game.GetGrid();
        Color[,] colors = new Color[grid.GetBoardWidth(), grid.GetBoardHeight()];
        for (int y = 0; y < grid.GetBoardHeight(); y++)
        {
            for (int x = 0; x < grid.GetBoardWidth(); x++) colors[x, y] = OverlayColor(grid.GetTile(x, y));
        }
        return colors;
    }

    public static IEnumerator WaitUntil(Func<bool> condition, float seconds)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < end) yield return null;
    }
}
