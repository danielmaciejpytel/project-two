using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardGrid
{
    private int _height, _width;
    private float _designerTileSize;
    private float _tileWidth;
    private float _tileHeight;
    private TileController[,] _gridArray;
    private bool _isDesignerMode;

    private Vector3 GetWorldPosition(GridPosition gp)
    {
        float x, y;
        if (_isDesignerMode) return new Vector3((gp.x) * _designerTileSize - 3.5f, gp.y * -_designerTileSize, 0.0f);
        else
        {
            x = (gp.x - gp.y) * _tileWidth/2;
            y = -(gp.x + gp.y - 2.0f) * _tileHeight / 2;
            return new Vector3(x, y, 0.0f);
        }
    }

    private void HighlightSurroundingTiles(GridPosition startingPosition, bool isAttack, HighlightType hType, int playerId = 0)
    {
        if (startingPosition.x > 0)
        {
            _gridArray[startingPosition.x - 1, startingPosition.y].Highlight(hType, isAttack, playerId);
            if (startingPosition.y > 0) _gridArray[startingPosition.x - 1, startingPosition.y - 1].Highlight(hType, isAttack, playerId);
            if (startingPosition.y < _height - 1) _gridArray[startingPosition.x - 1, startingPosition.y + 1].Highlight(hType, isAttack, playerId);
        }
        if (startingPosition.x < _width - 1)
        {
            _gridArray[startingPosition.x + 1, startingPosition.y].Highlight(hType, isAttack, playerId);
            if (startingPosition.y > 0) _gridArray[startingPosition.x + 1, startingPosition.y - 1].Highlight(hType, isAttack, playerId);
            if (startingPosition.y < _height - 1) _gridArray[startingPosition.x + 1, startingPosition.y + 1].Highlight(hType, isAttack, playerId);
        }
        if (startingPosition.y > 0) _gridArray[startingPosition.x, startingPosition.y - 1].Highlight(hType, isAttack, playerId);
        if (startingPosition.y < _height - 1) _gridArray[startingPosition.x, startingPosition.y + 1].Highlight(hType, isAttack, playerId);
    }

    #region Pathfinding

    private TileController GetLowestFCostNode(List<TileController> nodeList)
    {
        TileController lowestFCostNode = null;
        foreach(TileController gn in nodeList)
        {
            if(lowestFCostNode == null || gn._fCost < lowestFCostNode._fCost)
            {
                lowestFCostNode = gn;
            }
        }
        return lowestFCostNode;
    }

    private List<TileController> GetNeighbourList(GridPosition myPosition)
    {
        List<TileController> resultList;
        resultList = new List<TileController>();
        if (myPosition.x > 0) resultList.Add(_gridArray[myPosition.x - 1, myPosition.y]);
        if(myPosition.x < _width - 1) resultList.Add(_gridArray[myPosition.x + 1, myPosition.y]);
        if (myPosition.y > 0) resultList.Add(_gridArray[myPosition.x, myPosition.y - 1]);
        if (myPosition.y < _height - 1) resultList.Add(_gridArray[myPosition.x, myPosition.y + 1]);
        return resultList;
    }

    private List<TileController> CalculatePath(TileController endNode)
    {
        List<TileController> resultPath = new List<TileController>();
        resultPath.Add(endNode);
        TileController currentNode = endNode;
        while (currentNode._cameFromNode != null)
        {
            resultPath.Add(currentNode._cameFromNode);
            currentNode = currentNode._cameFromNode;
        }
        resultPath.Reverse();
        return resultPath;
    }

    private int CalculateDistance(GridPosition start, GridPosition end)
    {
        return Mathf.Abs(start.x - end.x) + Mathf.Abs(start.y - end.y);
    }

    private bool IsTileVisible(GridPosition startingPosition, GridPosition checkedTilePosition)
    {
        bool result = true;
        if (checkedTilePosition.x > startingPosition.x)
        {
            for (int x2 = startingPosition.x + 1; x2 < checkedTilePosition.x; x2++) if (_gridArray[x2, startingPosition.y]._isOccupied) result = false;
        }
        if (checkedTilePosition.x < startingPosition.x)
        {
            for (int x2 = startingPosition.x - 1; x2 > checkedTilePosition.x; x2--) if (_gridArray[x2, startingPosition.y]._isOccupied) result = false;
        }
        if (checkedTilePosition.y > startingPosition.y)
        {
            for (int y2 = startingPosition.y + 1; y2 < checkedTilePosition.y; y2++) if (_gridArray[startingPosition.x, y2]._isOccupied) result = false;
        }
        if (checkedTilePosition.y < startingPosition.y)
        {
            for (int y2 = startingPosition.y - 1; y2 > checkedTilePosition.y; y2--) if (_gridArray[startingPosition.x, y2]._isOccupied) result = false;
        }
        return result;
    }

    public List<TileController> FindPath(GridPosition startPosition, GridPosition endPosition)
    {
        List<TileController> openList, closedList;

        TileController startNode = _gridArray[startPosition.x, startPosition.y];
        TileController endNode = _gridArray[endPosition.x, endPosition.y];
        openList = new List<TileController> { startNode };
        closedList = new List<TileController>();
        for (int y = 0; y < _gridArray.GetLength(0); y++)
        {
            for (int x = 0; x < _gridArray.GetLength(1); x++)
            {
                TileController pathNode = _gridArray[x, y];
                pathNode._gCost = int.MaxValue;
                pathNode.CalculateFCost();
                pathNode._cameFromNode = null;
            }
        }
        startNode._gCost = 0;
        startNode._hCost = CalculateDistance(startNode.GetGridPosition(), endNode.GetGridPosition());
        startNode.CalculateFCost();
        while (openList.Count > 0)
        {
            TileController currentNode = GetLowestFCostNode(openList);
            if (currentNode == endNode)
            {
                return CalculatePath(endNode);
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            foreach (TileController neighbourNode in GetNeighbourList(currentNode.GetGridPosition()))
            {
                if (closedList.Contains(neighbourNode)) continue;
                if (!neighbourNode.isWalkable() || neighbourNode._isOccupied)
                {
                    closedList.Add(neighbourNode);
                    continue;
                }

                int tempGCost = currentNode._gCost + 1;
                if (tempGCost < neighbourNode._gCost)
                {
                    neighbourNode._cameFromNode = currentNode;
                    neighbourNode._gCost = tempGCost;
                    neighbourNode._hCost = CalculateDistance(neighbourNode.GetGridPosition(), endNode.GetGridPosition());
                    neighbourNode.CalculateFCost();

                    if (!openList.Contains(neighbourNode))
                    {
                        openList.Add(neighbourNode);
                    }
                }
            }
        }
        return null;
    }

    #endregion

    public BoardGrid(string[] gridInfo, GameObject[] tilePrefabs, float tileSize, float tileWidth, float tileHeight)
    {
        _height = gridInfo.Length;
        _width = (gridInfo[0].Length+1)/2;
        _gridArray = new TileController[_width, _height];
        _designerTileSize = tileSize;
        _tileWidth = tileWidth;
        _tileHeight = tileHeight;
        _isDesignerMode = false;
        for (int y = 0; y < _gridArray.GetLength(0); y++)
        {
            string[] gridLine = gridInfo[y].Split(',');
            for (int x = 0; x < _gridArray.GetLength(1); x++)
            {
                foreach (GameObject g in tilePrefabs)
                {
                    TileController tc = g.GetComponent<TileController>();
                    if (gridLine[x] == tc.GetLetter())// do zmiany
                    {
                        GridPosition tempGridPosition = new GridPosition(x, y);
                        TileController tempTileController = Object.Instantiate(g, GetWorldPosition(tempGridPosition), Quaternion.identity).GetComponent<TileController>();
                        tempTileController.InitializeTile(tempGridPosition, this);
                        _gridArray[x, y] = tempTileController;
                    }
                }
                if (_gridArray[x, y] == null) Debug.Log("Error: Tile"+x.ToString()+", "+y.ToString()+" not initialized");
            }
        }
    }

    public TileController GetTile(GridPosition tilePosition)
    {
        if (tilePosition.x < 0 || tilePosition.y < 0 || tilePosition.x >= _width || tilePosition.y >= _height) return null;
        else return _gridArray[tilePosition.x, tilePosition.y];
    }

    public TileController GetTile(int x, int y)
    {
        if (x < 0 || y < 0 || x >= _width || y >= _height) return null;
        return _gridArray[x, y];
    }

    public void ShowMoveRange(GridPosition startingPosition, int range)
    {
        List<TileController> pathNodeList;
        for (int y = 0; y < _gridArray.GetLength(0); y++)
        {
            for (int x = 0; x < _gridArray.GetLength(1); x++)
            {
                if(_gridArray[x, y].GetGridDistance(startingPosition) <= range)
                {
                    // skip unwalkable tiles
                    if (!_gridArray[x, y].isWalkable()) continue;
                    pathNodeList = FindPath(startingPosition, _gridArray[x, y].GetGridPosition());
                    if (pathNodeList != null && pathNodeList.Count-1 <= range && _gridArray[x, y].GetGridPosition() != startingPosition) _gridArray[x, y].Highlight(HighlightType.MoveRange, false);
                }
            }
        }
    }

    public bool IsTileInMoveRange(UnitController myUnit, TileController myTile)
    {
        List<TileController> pathNodeList;
        pathNodeList = FindPath(myUnit.GetGridPosition(), myTile.GetGridPosition());
        if (pathNodeList != null && pathNodeList.Count - 1 <= myUnit.GetMoveRange()) return true;
        else return false;
    }

    public void ShowPath(UnitController myUnit, TileController myTile)
    {
        List<TileController> pathNodeList;
        //hide previous path
        ShowMoveRange(myUnit.GetGridPosition(), myUnit.GetMoveRange());
        if (IsTileInMoveRange(myUnit, myTile) && myTile.isWalkable())
        {
            //show new path
            pathNodeList = FindPath(myUnit.GetGridPosition(), myTile.GetGridPosition());
            foreach (TileController myNode in pathNodeList)
            {
                myNode.Highlight(HighlightType.Path, false);
            }
            myTile.AnimateHighlight();
            myUnit._myTile.StopAnimatingHighlight();
        }
        else
        {
            myUnit._myTile.AnimateHighlight();
            if (myTile.isWalkable()) myTile.Highlight(HighlightType.Hover, false);
        }
    }

    public void HideHighlight()
    {
        for (int y = 0; y < _gridArray.GetLength(0); y++)
        {
            for (int x = 0; x < _gridArray.GetLength(1); x++)
            {
                _gridArray[x, y].ClearTile();
            }
        }
    }

    public int GetBoardWidth()
    {
        return _width;
    }

    public int GetBoardHeight()
    {
        return _height;
    }

    public void ShowAttackRange(UnitController myUnit, int range, int playerId)
    {
        GridPosition startingPosition;
        startingPosition = myUnit.GetGridPosition();
        if (myUnit.gameObject.GetComponent<IValidateTarget>() != null)
        {
            IValidateTarget myValidator;
            GridPosition targetPosition;
            myValidator = myUnit.gameObject.GetComponent<IValidateTarget>();
            targetPosition = myValidator.GetValidPosition();
            _gridArray[targetPosition.x, targetPosition.y].Highlight(HighlightType.AttackRange, true, playerId);
        }
        else
        {
            // highlight melee attack range
            HighlightSurroundingTiles(startingPosition, true, HighlightType.AttackRange, playerId);
            if (range > 1) // highlight range attack range
            {
                for (int i = 1; i < _width; i++)
                {
                    if (startingPosition.x + i < _width && i <= range && IsTileVisible(startingPosition, new GridPosition(startingPosition.x + i, startingPosition.y))) _gridArray[startingPosition.x + i, startingPosition.y].Highlight(HighlightType.AttackRange, true, playerId);
                    if (startingPosition.x - i >= 0 && i <= range && IsTileVisible(startingPosition, new GridPosition(startingPosition.x - i, startingPosition.y))) _gridArray[startingPosition.x - i, startingPosition.y].Highlight(HighlightType.AttackRange, true, playerId);
                }
                for (int i = 1; i < _height; i++)
                {
                    if (startingPosition.y + i < _height && i <= range && IsTileVisible(startingPosition, new GridPosition(startingPosition.x, startingPosition.y + i))) _gridArray[startingPosition.x, startingPosition.y + i].Highlight(HighlightType.AttackRange, true, playerId);
                    if (startingPosition.y - i >= 0 && i <= range && IsTileVisible(startingPosition, new GridPosition(startingPosition.x, startingPosition.y - i))) _gridArray[startingPosition.x, startingPosition.y - i].Highlight(HighlightType.AttackRange, true, playerId);
                }
            }
        }
    }

    public bool IsTileInAttackRange(UnitController myUnit, TileController targetTile)
    {
        if(myUnit.gameObject.GetComponent<IValidateTarget>() != null)
        {
            IValidateTarget myValidator;
            myValidator = myUnit.gameObject.GetComponent<IValidateTarget>();
            if (targetTile._isOccupied && myValidator.IsTargetValid(targetTile._myUnit)) return true;
            else return false;
        }
        if (Mathf.Abs(myUnit.GetGridPosition().x - targetTile.GetGridPosition().x) <= 1 && Mathf.Abs(myUnit.GetGridPosition().y - targetTile.GetGridPosition().y) <= 1) return true;
        if (myUnit.GetAttackRange() > 1)
        {
            if (myUnit.GetGridPosition().x == targetTile.GetGridPosition().x && Mathf.Abs(myUnit.GetGridPosition().y - targetTile.GetGridPosition().y) <= myUnit.GetAttackRange()
                || myUnit.GetGridPosition().y == targetTile.GetGridPosition().y && Mathf.Abs(myUnit.GetGridPosition().x - targetTile.GetGridPosition().x) <= myUnit.GetAttackRange())
            {
                return IsTileVisible(myUnit.GetGridPosition(), targetTile.GetGridPosition());
            }
            else return false;
        }
        else return false;
    }

    public void MakeEndTurnActions(int playerId)
    {
        IEndturnable myTileEndTurn;
        for (int y = 0; y < _gridArray.GetLength(0); y++)
        {
            for (int x = 0; x < _gridArray.GetLength(1); x++)
            {
                myTileEndTurn = _gridArray[x, y].gameObject.GetComponent<IEndturnable>();
                if (myTileEndTurn != null) myTileEndTurn.EndTurnAction(playerId);
            }
        }
    }

    public void ShowZone(TileController startingTile, HighlightType zoneType)
    {
        GridPosition startingPosition;
        startingPosition = startingTile.GetGridPosition();
        HighlightSurroundingTiles(startingPosition, false, zoneType);
    }

    public bool HasPossibleAttack(UnitController unit)
    {
        GridPosition startingPosition;
        int unitPlayer, range;

        if (unit._freeAttacksCount < 1) return false;
        startingPosition = unit.GetGridPosition();
        unitPlayer = unit.GetPlayerId();
        range = unit.GetAttackRange();
        if (startingPosition.x > 0)
        {
            if(_gridArray[startingPosition.x - 1, startingPosition.y]._isOccupied && _gridArray[startingPosition.x - 1, startingPosition.y]._myUnit.GetPlayerId() != unitPlayer) return true;
            if (startingPosition.y > 0 && _gridArray[startingPosition.x - 1, startingPosition.y - 1]._isOccupied && _gridArray[startingPosition.x - 1, startingPosition.y - 1]._myUnit.GetPlayerId() != unitPlayer) return true;
            if (startingPosition.y < _height - 1 && _gridArray[startingPosition.x - 1, startingPosition.y + 1]._isOccupied && _gridArray[startingPosition.x - 1, startingPosition.y + 1]._myUnit.GetPlayerId() != unitPlayer) return true;
        }
        if (startingPosition.x < _width - 1)
        {
            if(_gridArray[startingPosition.x + 1, startingPosition.y]._isOccupied && _gridArray[startingPosition.x + 1, startingPosition.y]._myUnit.GetPlayerId() != unitPlayer) return true;
            if (startingPosition.y > 0 && _gridArray[startingPosition.x + 1, startingPosition.y - 1]._isOccupied && _gridArray[startingPosition.x + 1, startingPosition.y - 1]._myUnit.GetPlayerId() != unitPlayer) return true;
            if (startingPosition.y < _height - 1 && _gridArray[startingPosition.x + 1, startingPosition.y + 1]._isOccupied && _gridArray[startingPosition.x + 1, startingPosition.y + 1]._myUnit.GetPlayerId() != unitPlayer) return true;
        }
        if(startingPosition.y > 0 && _gridArray[startingPosition.x, startingPosition.y - 1]._isOccupied && _gridArray[startingPosition.x, startingPosition.y - 1]._myUnit.GetPlayerId() != unitPlayer) return true;
        if (startingPosition.y < _height - 1 && _gridArray[startingPosition.x, startingPosition.y + 1]._isOccupied && _gridArray[startingPosition.x, startingPosition.y + 1]._myUnit.GetPlayerId() != unitPlayer) return true;
        if (range > 1)
        {
            for (int i = 1; i < _width; i++)
            {
                if (startingPosition.x + i < _width && i <= range && _gridArray[startingPosition.x + i, startingPosition.y]._isOccupied && _gridArray[startingPosition.x + i, startingPosition.y]._myUnit.GetPlayerId() != unitPlayer && IsTileVisible(startingPosition, new GridPosition(startingPosition.x + i, startingPosition.y))) return true;
                if (startingPosition.x - i >= 0 && i <= range && _gridArray[startingPosition.x - i, startingPosition.y]._isOccupied && _gridArray[startingPosition.x - i, startingPosition.y]._myUnit.GetPlayerId() != unitPlayer && IsTileVisible(startingPosition, new GridPosition(startingPosition.x - i, startingPosition.y))) return true;
            }
            for (int i = 1; i < _height; i++)
            {
                if (startingPosition.y + i < _height && i <= range && _gridArray[startingPosition.x, startingPosition.y + i]._isOccupied && _gridArray[startingPosition.x, startingPosition.y + i]._myUnit.GetPlayerId() != unitPlayer && IsTileVisible(startingPosition, new GridPosition(startingPosition.x, startingPosition.y + i))) return true;
                if (startingPosition.y - i >= 0 && i <= range && _gridArray[startingPosition.x, startingPosition.y - i]._isOccupied && _gridArray[startingPosition.x, startingPosition.y - i]._myUnit.GetPlayerId() != unitPlayer && IsTileVisible(startingPosition, new GridPosition(startingPosition.x, startingPosition.y - i))) return true;
            }
        }
        return false;
    }

    public void ChangeMode()
    {
        string mode;

        if (_isDesignerMode)
        {
            _isDesignerMode = false;
            mode = "player";
        }
        else
        {
            _isDesignerMode = true;
            mode = "designer";
        }
        for (int y = 0; y < _gridArray.GetLength(0); y++)
        {
            for (int x = 0; x < _gridArray.GetLength(1); x++)
            {
                _gridArray[x, y].ChangeMode(mode, GetWorldPosition(_gridArray[x, y].GetGridPosition()));
            }
        }
    }
}
