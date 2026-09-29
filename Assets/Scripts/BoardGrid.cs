using System.Collections.Generic;
using UnityEngine;

public class BoardGrid
{
    private static readonly GridPosition[] OrthogonalDirections =
    {
        new GridPosition(-1, 0), new GridPosition(1, 0), new GridPosition(0, -1), new GridPosition(0, 1)
    };

    private readonly int _height, _width;
    private readonly float _designerTileSize;
    private readonly float _tileWidth;
    private readonly float _tileHeight;
    private readonly TileController[,] _gridArray;
    private bool _isDesignerMode;

    public BoardGrid(string[] gridInfo, GameObject[] tilePrefabs, float tileSize, float tileWidth, float tileHeight)
    {
        _height = gridInfo.Length;
        _width = (gridInfo[0].Length + 1) / 2;
        _gridArray = new TileController[_width, _height];
        _designerTileSize = tileSize;
        _tileWidth = tileWidth;
        _tileHeight = tileHeight;
        _isDesignerMode = false;

        Dictionary<string, GameObject> prefabsByLetter = new Dictionary<string, GameObject>();
        foreach (GameObject prefab in tilePrefabs)
        {
            prefabsByLetter[prefab.GetComponent<TileController>().GetLetter()] = prefab;
        }

        for (int y = 0; y < _height; y++)
        {
            string[] gridLine = gridInfo[y].Trim().Split(',');
            for (int x = 0; x < _width; x++)
            {
                string letter = x < gridLine.Length ? gridLine[x].Trim() : string.Empty;
                if (!prefabsByLetter.TryGetValue(letter, out GameObject prefab))
                {
                    Debug.LogError($"Tile {x}, {y}: unknown tile letter '{letter}' in the board layout.");
                    continue;
                }
                GridPosition position = new GridPosition(x, y);
                TileController tile = Object.Instantiate(prefab, GetWorldPosition(position), Quaternion.identity).GetComponent<TileController>();
                tile.InitializeTile(position, this);
                _gridArray[x, y] = tile;
            }
        }
    }

    private Vector3 GetWorldPosition(GridPosition gp)
    {
        if (_isDesignerMode) return new Vector3(gp.x * _designerTileSize - 3.5f, gp.y * -_designerTileSize, 0.0f);

        float x = (gp.x - gp.y) * _tileWidth / 2;
        float y = -(gp.x + gp.y - 2.0f) * _tileHeight / 2;
        return new Vector3(x, y, 0.0f);
    }

    private bool IsInside(int x, int y) => x >= 0 && y >= 0 && x < _width && y < _height;

    private IEnumerable<TileController> AllTiles()
    {
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                if (_gridArray[x, y] != null) yield return _gridArray[x, y];
            }
        }
    }

    // All 8 tiles around the position (melee range, deployment and ability zones).
    private IEnumerable<TileController> SurroundingTiles(GridPosition center)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                TileController tile = GetTile(center.x + dx, center.y + dy);
                if (tile != null) yield return tile;
            }
        }
    }

    private void HighlightSurroundingTiles(GridPosition startingPosition, bool isAttack, HighlightType hType, int playerId = 0)
    {
        foreach (TileController tile in SurroundingTiles(startingPosition))
        {
            tile.Highlight(hType, isAttack, playerId);
        }
    }

    #region Pathfinding

    /// <summary>
    /// Breadth-first search over walkable, unoccupied tiles. Every move costs 1, so BFS gives the same
    /// distances as A* but computes the whole reachable area in one pass instead of one search per tile.
    /// </summary>
    private int[,] CalculateDistances(GridPosition startPosition, int maxDistance)
    {
        int[,] distances = new int[_width, _height];
        for (int y = 0; y < _height; y++)
            for (int x = 0; x < _width; x++)
                distances[x, y] = int.MaxValue;

        if (!IsInside(startPosition.x, startPosition.y)) return distances;

        Queue<GridPosition> frontier = new Queue<GridPosition>();
        distances[startPosition.x, startPosition.y] = 0;
        frontier.Enqueue(startPosition);
        while (frontier.Count > 0)
        {
            GridPosition current = frontier.Dequeue();
            int nextDistance = distances[current.x, current.y] + 1;
            if (nextDistance > maxDistance) continue;
            foreach (GridPosition direction in OrthogonalDirections)
            {
                int nx = current.x + direction.x;
                int ny = current.y + direction.y;
                if (!IsInside(nx, ny) || distances[nx, ny] <= nextDistance) continue;
                TileController neighbour = _gridArray[nx, ny];
                if (neighbour == null || !neighbour.isWalkable() || neighbour.IsOccupied) continue;
                distances[nx, ny] = nextDistance;
                frontier.Enqueue(new GridPosition(nx, ny));
            }
        }
        return distances;
    }

    private int GetPathLength(GridPosition startPosition, GridPosition endPosition, int maxDistance)
    {
        if (!IsInside(endPosition.x, endPosition.y)) return int.MaxValue;
        return CalculateDistances(startPosition, maxDistance)[endPosition.x, endPosition.y];
    }

    /// <summary>
    /// Returns the shortest path from start to end (both included), or null when the end can't be reached.
    /// </summary>
    public List<TileController> FindPath(GridPosition startPosition, GridPosition endPosition)
    {
        int[,] distances = CalculateDistances(startPosition, int.MaxValue - 1);
        if (!IsInside(endPosition.x, endPosition.y) || distances[endPosition.x, endPosition.y] == int.MaxValue) return null;

        // Walk back from the end, always stepping onto a tile one move closer to the start.
        List<TileController> path = new List<TileController>();
        GridPosition current = endPosition;
        path.Add(_gridArray[current.x, current.y]);
        while (current != startPosition)
        {
            int currentDistance = distances[current.x, current.y];
            foreach (GridPosition direction in OrthogonalDirections)
            {
                int nx = current.x + direction.x;
                int ny = current.y + direction.y;
                if (IsInside(nx, ny) && distances[nx, ny] == currentDistance - 1)
                {
                    current = new GridPosition(nx, ny);
                    break;
                }
            }
            path.Add(_gridArray[current.x, current.y]);
        }
        path.Reverse();
        return path;
    }

    #endregion

    private bool IsTileVisible(GridPosition startingPosition, GridPosition checkedTilePosition)
    {
        // Only straight lines are checked; any unit standing in between blocks the shot.
        int stepX = System.Math.Sign(checkedTilePosition.x - startingPosition.x);
        int stepY = System.Math.Sign(checkedTilePosition.y - startingPosition.y);
        if (stepX != 0 && stepY != 0) return true;

        int x = startingPosition.x + stepX;
        int y = startingPosition.y + stepY;
        while (x != checkedTilePosition.x || y != checkedTilePosition.y)
        {
            if (_gridArray[x, y].IsOccupied) return false;
            x += stepX;
            y += stepY;
        }
        return true;
    }

    public TileController GetTile(GridPosition tilePosition) => GetTile(tilePosition.x, tilePosition.y);

    public TileController GetTile(int x, int y) => IsInside(x, y) ? _gridArray[x, y] : null;

    public int GetBoardWidth() => _width;

    public int GetBoardHeight() => _height;

    public void ShowMoveRange(GridPosition startingPosition, int range)
    {
        int[,] distances = CalculateDistances(startingPosition, range);
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                if (distances[x, y] > 0 && distances[x, y] <= range) _gridArray[x, y].Highlight(HighlightType.MoveRange, false);
            }
        }
    }

    public bool IsTileInMoveRange(UnitController myUnit, TileController myTile)
    {
        int range = myUnit.GetMoveRange();
        return GetPathLength(myUnit.GetGridPosition(), myTile.GetGridPosition(), range) <= range;
    }

    public void ShowPath(UnitController myUnit, TileController myTile)
    {
        //hide previous path
        ShowMoveRange(myUnit.GetGridPosition(), myUnit.GetMoveRange());
        if (myTile.isWalkable() && IsTileInMoveRange(myUnit, myTile))
        {
            foreach (TileController myNode in FindPath(myUnit.GetGridPosition(), myTile.GetGridPosition()))
            {
                myNode.Highlight(HighlightType.Path, false);
            }
            myTile.AnimateHighlight();
            myUnit.CurrentTile.StopAnimatingHighlight();
        }
        else
        {
            myUnit.CurrentTile.AnimateHighlight();
            if (myTile.isWalkable()) myTile.Highlight(HighlightType.Hover, false);
        }
    }

    public void HideHighlight()
    {
        foreach (TileController tile in AllTiles())
        {
            tile.ClearTile();
        }
    }

    public void ShowAttackRange(UnitController myUnit, int range, int playerId)
    {
        GridPosition startingPosition = myUnit.GetGridPosition();
        IValidateTarget myValidator = myUnit.GetComponent<IValidateTarget>();
        if (myValidator != null)
        {
            // e.g. provoked units may only attack the unit that provoked them
            TileController targetTile = GetTile(myValidator.GetValidPosition());
            if (targetTile != null) targetTile.Highlight(HighlightType.AttackRange, true, playerId);
            return;
        }

        // highlight melee attack range
        HighlightSurroundingTiles(startingPosition, true, HighlightType.AttackRange, playerId);
        if (range <= 1) return;

        // highlight range attack range
        foreach (GridPosition direction in OrthogonalDirections)
        {
            for (int i = 1; i <= range; i++)
            {
                GridPosition target = new GridPosition(startingPosition.x + direction.x * i, startingPosition.y + direction.y * i);
                if (!IsInside(target.x, target.y)) break;
                if (IsTileVisible(startingPosition, target)) _gridArray[target.x, target.y].Highlight(HighlightType.AttackRange, true, playerId);
            }
        }
    }

    public bool IsTileInAttackRange(UnitController myUnit, TileController targetTile)
    {
        GridPosition unitPosition = myUnit.GetGridPosition();
        GridPosition targetPosition = targetTile.GetGridPosition();

        IValidateTarget myValidator = myUnit.GetComponent<IValidateTarget>();
        if (myValidator != null) return targetTile.IsOccupied && myValidator.IsTargetValid(targetTile.Unit);

        int dx = Mathf.Abs(unitPosition.x - targetPosition.x);
        int dy = Mathf.Abs(unitPosition.y - targetPosition.y);
        if (dx <= 1 && dy <= 1) return true;

        int range = myUnit.GetAttackRange();
        if (range <= 1) return false;
        bool inStraightLine = (dx == 0 && dy <= range) || (dy == 0 && dx <= range);
        return inStraightLine && IsTileVisible(unitPosition, targetPosition);
    }

    public void MakeEndTurnActions(int playerId)
    {
        foreach (TileController tile in AllTiles())
        {
            IEndturnable myTileEndTurn = tile.GetComponent<IEndturnable>();
            if (myTileEndTurn != null) myTileEndTurn.EndTurnAction(playerId);
        }
    }

    public void ShowZone(TileController startingTile, HighlightType zoneType)
    {
        HighlightSurroundingTiles(startingTile.GetGridPosition(), false, zoneType);
    }

    public bool HasPossibleAttack(UnitController unit)
    {
        if (unit.FreeAttacksCount < 1) return false;

        // Uses the same rules as an actual attack, so effects like "provoked" are respected
        // and the player is never left in the attack state without a legal target.
        foreach (TileController tile in AllTiles())
        {
            if (!tile.IsOccupied || tile.Unit == unit) continue;
            if (unit.IsTargetValid(tile.Unit) && IsTileInAttackRange(unit, tile)) return true;
        }
        return false;
    }

    public void ChangeMode()
    {
        _isDesignerMode = !_isDesignerMode;
        string mode = _isDesignerMode ? "designer" : "player";
        foreach (TileController tile in AllTiles())
        {
            tile.ChangeMode(mode, GetWorldPosition(tile.GetGridPosition()));
        }
    }
}
