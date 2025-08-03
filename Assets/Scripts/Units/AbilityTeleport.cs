using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityTeleport : MonoBehaviour, IAbility, IEndturnable
{
    [SerializeField] private string _myButtonDescription;
    [SerializeField] private string _myDescription;
    [SerializeField] private AudioClip _mySound;
    private UnitController _myUnit;
    private UnitController _unitToTeleport;
    private bool _isAvailableThisTurn;

    // Start is called before the first frame update
    void Start()
    {
        _unitToTeleport = null;
        _myUnit = GetComponent<UnitController>();
        _isAvailableThisTurn = true;
    }

    private bool IsTileInTeleportZone(TileController startingTile, TileController checkedTile)
    {
        GridPosition startingPosition, checkedPosition;
        startingPosition = startingTile.GetGridPosition();
        checkedPosition = checkedTile.GetGridPosition();
        if (Mathf.Abs(startingPosition.x - checkedPosition.x) <= 1 && Mathf.Abs(startingPosition.y - checkedPosition.y) <= 1 && !checkedTile._isOccupied && checkedTile.isWalkable()) return true;
        else return false;
    }

    public bool IsAvailableThisTurn()
    {
        return _isAvailableThisTurn;
    }

    public void StartAction(GameController myGameController)
    {
        myGameController.HighlightUnits(_myUnit.GetPlayerId(), false);
    }

    public string GetButtonDescription()
    {
        return _myButtonDescription;
    }

    public string GetDescription()
    {
        return _myDescription;
    }

    public IGameState TileClicked(GameController myGameController, TileController clickedTile)
    {
        UIController ui;
        BoardGrid myGrid;
        TileController unitTile;

        ui = myGameController.GetUI();
        myGrid = myGameController.GetGrid();
        if (_unitToTeleport == null)
        {
            if (_myUnit._hasMoved) return new AttackSelectedState(_myUnit, myGrid, ui);
            else return new UnitSelectedState(_myUnit, myGrid, ui);
        }
        else if(IsTileInTeleportZone(_unitToTeleport._myTile, clickedTile) && !clickedTile._isOccupied)
        {
            _myUnit.StartAnimation("UseAbility");
            _myUnit.PlaySound(_mySound);
            unitTile = _unitToTeleport._myTile;
            _unitToTeleport.DeployUnit(clickedTile);
            unitTile._isOccupied = false;
            unitTile._myUnit = null;
            myGrid.HideHighlight();
            _isAvailableThisTurn = false;
            if (_myUnit._hasMoved) return new AttackSelectedState(_myUnit, myGrid, ui);
            else return new UnitSelectedState(_myUnit, myGrid, ui);
        }
        return null;
    }

    public IGameState UnitClicked(GameController myGameController, UnitController clickedUnit)
    {
        UIController ui;
        BoardGrid myGrid;
        ui = myGameController.GetUI();
        myGrid = myGameController.GetGrid();
        if (clickedUnit._isDeployed && clickedUnit.GetPlayerId() == _myUnit.GetPlayerId() && clickedUnit != _myUnit)
        { 
            _unitToTeleport = clickedUnit;
            myGrid.ShowZone(clickedUnit._myTile, HighlightType.MoveRange);
        }
        return null;
    }

    public IGameState TileHovered(GameController myGameController, TileController hoveredTile)
    {
        UIController ui;

        if (hoveredTile.isWalkable()) hoveredTile.Highlight(HighlightType.Hover, false);
        ui = myGameController.GetUI();
        ui.DisplayTile(hoveredTile);
        return null;
    }

    public IGameState UnitHovered(GameController myGameController, UnitController hoveredUnit)
    {
        UIController ui;

        ui = myGameController.GetUI();
        ui.DisplayUnit(hoveredUnit);
        return null;
    }

    public IGameState UnitUnhovered(GameController myGameController, UnitController unhoveredUnit)
    {
        UIController ui;

        ui = myGameController.GetUI();
        ui.ClearDisplay();
        unhoveredUnit._myTile.StopAnimatingHighlight();
        return null;
    }

    public void EndTurnAction(int playerId)
    {
        if (playerId != _myUnit.GetPlayerId()) _isAvailableThisTurn = true;
    }
}
