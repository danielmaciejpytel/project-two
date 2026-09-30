using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// The real board (StreamingAssets/Grid.csv with the tile prefabs) built outside of a scene.
public class BoardGridTests
{
    private BoardGrid _grid;
    private string[] _layout;
    private Scene _previousScene;
    private Scene _scratchScene;

    [SetUp]
    public void BuildBoard()
    {
        // The tiles are created in a scene of their own, so the scene the user has open stays untouched (and unmodified).
        // (An unsaved untitled scene, like the one the Test Runner leaves behind, does not allow an additive scene.)
        _previousScene = SceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(_previousScene.path))
        {
            _scratchScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(_scratchScene);
        }
        _layout = File.ReadAllLines(Path.Combine(Application.streamingAssetsPath, "Grid.csv"));
        List<GameObject> prefabs = new List<GameObject>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            TileController tile = prefab.GetComponent<TileController>();
            // The base prefabs the tile variants are made from have no tile data.
            if (tile != null && new SerializedObject(tile).FindProperty("_tile").objectReferenceValue != null) prefabs.Add(prefab);
        }
        _grid = new BoardGrid(_layout, prefabs.ToArray(), 0.64f, 1.5f, 0.87f);
    }

    [TearDown]
    public void RemoveBoard()
    {
        if (_scratchScene.IsValid())
        {
            SceneManager.SetActiveScene(_previousScene);
            EditorSceneManager.CloseScene(_scratchScene, true);
            return;
        }
        // No scratch scene: remove the tiles one by one.
        if (_grid == null) return;
        for (int y = 0; y < _grid.GetBoardHeight(); y++)
        {
            for (int x = 0; x < _grid.GetBoardWidth(); x++)
            {
                TileController tile = _grid.GetTile(x, y);
                if (tile != null) Object.DestroyImmediate(tile.gameObject);
            }
        }
    }

    [Test]
    public void EveryCellOfTheLayoutBecomesATile()
    {
        Assert.AreEqual(_layout.Length, _grid.GetBoardHeight());
        Assert.AreEqual(10, _grid.GetBoardWidth());
        for (int y = 0; y < _grid.GetBoardHeight(); y++)
        {
            for (int x = 0; x < _grid.GetBoardWidth(); x++)
            {
                Assert.IsNotNull(_grid.GetTile(x, y), $"No tile at {x}, {y}");
                Assert.AreEqual(new GridPosition(x, y), _grid.GetTile(x, y).GetGridPosition());
            }
        }
    }

    [Test]
    public void TilesOutsideTheBoardDoNotExist()
    {
        Assert.IsNull(_grid.GetTile(-1, 0));
        Assert.IsNull(_grid.GetTile(0, -1));
        Assert.IsNull(_grid.GetTile(_grid.GetBoardWidth(), 0));
        Assert.IsNull(_grid.GetTile(0, _grid.GetBoardHeight()));
    }

    [Test]
    public void CommandersStartOnWalkableCornerTiles()
    {
        Assert.IsTrue(_grid.GetTile(0, _grid.GetBoardHeight() - 1).isWalkable());
        Assert.IsTrue(_grid.GetTile(_grid.GetBoardWidth() - 1, 0).isWalkable());
    }

    [Test]
    public void PathGoesAroundObstaclesInTheShortestWay()
    {
        // Row 0 has obstacles at x = 4 and 5, so the way from one end to the other detours through row 1.
        GridPosition start = new GridPosition(0, 0);
        GridPosition end = new GridPosition(9, 0);
        List<TileController> path = _grid.FindPath(start, end);

        Assert.IsNotNull(path);
        Assert.AreEqual(start, path[0].GetGridPosition());
        Assert.AreEqual(end, path[path.Count - 1].GetGridPosition());
        Assert.AreEqual(9 + 2 + 1, path.Count, "Shortest way: 9 steps along the row and 2 to get around the obstacles, plus the start tile");
        for (int i = 0; i < path.Count; i++)
        {
            Assert.IsTrue(path[i].isWalkable(), "The path crosses an obstacle at " + path[i].GetGridPosition());
            if (i == 0) continue;
            GridPosition a = path[i - 1].GetGridPosition();
            GridPosition b = path[i].GetGridPosition();
            Assert.AreEqual(1, Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y), "Steps are orthogonal");
        }
    }

    [Test]
    public void PathToAnObstacleDoesNotExist()
    {
        Assert.IsNull(_grid.FindPath(new GridPosition(0, 0), new GridPosition(4, 0)));
    }
}
