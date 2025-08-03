using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeploymentState : IGameState
{
    private UnitController _activeUnit;
    private UnitController _kingUnit;
    private UnitController _unitToDeploy;

    private bool IsTileInDeploymentZone(TileController startingTile, TileController checkedTile)
    {
        GridPosition startingPosition, checkedPosition;
        startingPosition = startingTile.GetGridPosition();
        checkedPosition = checkedTile.GetGridPosition();
        if (Mathf.Abs(startingPosition.x - checkedPosition.x) <= 1 && Mathf.Abs(startingPosition.y - checkedPosition.y) <= 1 && !checkedTile._isOccupied && checkedTile.isWalkable()) return true;
        else return false;
    }

    public DeploymentState(UnitController unit, UnitController king, BoardGrid myGrid, UIController ui)
    {
        _unitToDeploy = null;
        _activeUnit = unit;
        _kingUnit = king;
        myGrid.HideHighlight();
        ui.ShowDeployableUnits();
    }

    public IGameState TileClicked(GameController myGameController, TileController clickedTile)
    {
        UIController ui;
        BoardGrid myGrid;
        int newPlayer;

        SoundController._instance.PlayClick();
        ui = myGameController.GetUI();
        myGrid = myGameController.GetGrid();
        GridPosition tilePosition;
        tilePosition = clickedTile.GetGridPosition();
        if (_unitToDeploy == null)
        {
            ui.EndDeployment();
            if (_activeUnit == null) return new BeginTurnState(_kingUnit.GetPlayerId());
            else
            {
                if (_activeUnit._hasMoved)
                {
                    if (!myGrid.HasPossibleAttack(_activeUnit))
                    {
                        _activeUnit._isAvailable = false;
                        myGrid.HideHighlight();
                        _activeUnit.SetReticle(false);
                        ui.MarkUnitUnavailable(_activeUnit);
                    }
                    else return new AttackSelectedState(_activeUnit, myGrid, ui);
                    if(myGameController.MovesDepleted(_activeUnit.GetPlayerId()))
                    {
                        myGameController.EndPlayerTurn(_activeUnit.GetPlayerId());
                        newPlayer = (_activeUnit.GetPlayerId() == 1 ? 2 : 1);
                        return new BeginTurnState(newPlayer);
                    }
                    else return new BeginTurnState(_activeUnit.GetPlayerId());
                }
                else return new UnitSelectedState(_activeUnit, myGrid, ui);
            }
        }
        else if (IsTileInDeploymentZone(_kingUnit._myTile, clickedTile) && !clickedTile._isOccupied)
        {
            _unitToDeploy.DeployUnit(clickedTile);
            SoundController._instance.PlayCall();
            if (_unitToDeploy.SummoningSickness())
            {
                ui.MarkUnitUnavailable(_unitToDeploy);
                _unitToDeploy._isAvailable = false;
            }
            _unitToDeploy._isDeployed = true;
            clickedTile.ClearTile();
            ui.EndDeployment();
            myGrid.HideHighlight();
            if (_activeUnit == null) return new BeginTurnState(_kingUnit.GetPlayerId());
            else
            {
                if (_activeUnit._hasMoved)
                {
                    if (!myGrid.HasPossibleAttack(_activeUnit))
                    {
                        _activeUnit._isAvailable = false;
                        myGrid.HideHighlight();
                        _activeUnit.SetReticle(false);
                        ui.MarkUnitUnavailable(_activeUnit);
                    }
                    else return new AttackSelectedState(_activeUnit, myGrid, ui);
                    if (myGameController.MovesDepleted(_activeUnit.GetPlayerId()))
                    {
                        myGameController.EndPlayerTurn(_activeUnit.GetPlayerId());
                        newPlayer = (_activeUnit.GetPlayerId() == 1 ? 2 : 1);
                        return new BeginTurnState(newPlayer);
                    }
                    else return new BeginTurnState(_activeUnit.GetPlayerId());
                }
                else return new UnitSelectedState(_activeUnit, myGrid, ui);
            }
        }
        return null;
    }

    public IGameState UnitClicked(GameController myGameController, UnitController clickedUnit)
    {
        UIController ui;
        BoardGrid myGrid;

        SoundController._instance.PlayClick();
        ui = myGameController.GetUI();
        myGrid = myGameController.GetGrid();
        if (clickedUnit._isDeployed)
        {
            myGrid.HideHighlight();
            ui.EndDeployment();
            if (_activeUnit != null)
            {
                _activeUnit.SetReticle(false);
            }
            if (clickedUnit._hasMoved) return new AttackSelectedState(clickedUnit, myGrid, ui);
            else return new UnitSelectedState(clickedUnit, myGrid, ui);
        }
        else
        {
            _unitToDeploy = clickedUnit;
            ui.DisplayUnit(_unitToDeploy);
            ui.SelectUnit(_unitToDeploy);
            myGrid.ShowZone(_kingUnit._myTile, HighlightType.Deployment);
        }
        return null;
    }

    public IGameState TileHovered(GameController myGameController, TileController hoveredTile)
    {
        UIController ui;

        SoundController._instance.PlayHover();
        if (hoveredTile.isWalkable()) hoveredTile.Highlight(HighlightType.Hover, false);
        ui = myGameController.GetUI();
        ui.DisplayTile(hoveredTile);
        return null;
    }

    public IGameState UnitHovered(GameController myGameController, UnitController hoveredUnit)
    {
        UIController ui;

        SoundController._instance.PlayHover();
        ui = myGameController.GetUI();
        ui.DisplayUnit(hoveredUnit);
        return null;
    }

    public IGameState UnitUnhovered(GameController myGameController, UnitController unhoveredUnit)
    {
        UIController ui;

        ui = myGameController.GetUI();
        ui.ClearDisplay();
        return null;
    }

    public IGameState ExecutionEnd(GameController myGameController)
    {
        //nothing happens
        return null;
    }

    public IGameState EndTurnPressed(GameController myGameController)
    {
        // disable unit reticle
        BoardGrid myGrid;
        int newPlayer;
        UIController ui;

        ui = myGameController.GetUI();
        myGrid = myGameController.GetGrid();
        myGrid.HideHighlight();
        _activeUnit.SetReticle(false);
        _activeUnit._isAvailable = false;
        ui.MarkUnitUnavailable(_activeUnit);
        myGameController.EndPlayerTurn(_activeUnit.GetPlayerId());
        newPlayer = (_activeUnit.GetPlayerId() == 1 ? 2 : 1);
        ui.EndDeployment();
        return new BeginTurnState(newPlayer);
    }

    public IGameState DeploymentPressed(GameController myGameController)
    {
        //nothing happens
        return null;
    }

    public IGameState AbilityPressed(GameController myGameController)
    {
        if (_activeUnit != null) return new AbilityState(myGameController, _activeUnit);
        else return null;
    }

    public void ChangeMode(GameController myGameController)
    {
        BoardGrid myGrid;

        myGrid = myGameController.GetGrid();
        myGrid.ChangeMode();
    }
}
