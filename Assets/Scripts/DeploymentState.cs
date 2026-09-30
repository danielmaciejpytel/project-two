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
        GridPosition startingPosition = startingTile.GetGridPosition();
        GridPosition checkedPosition = checkedTile.GetGridPosition();
        return Mathf.Abs(startingPosition.x - checkedPosition.x) <= 1
            && Mathf.Abs(startingPosition.y - checkedPosition.y) <= 1
            && !checkedTile.IsOccupied
            && checkedTile.isWalkable();
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
        UIController ui = myGameController.GetUI();
        BoardGrid myGrid = myGameController.GetGrid();

        SoundController.Instance?.PlayClick();
        if (_unitToDeploy == null)
        {
            ui.EndDeployment();
            return ReturnToActiveUnit(myGameController, myGrid, ui);
        }
        if (!IsTileInDeploymentZone(_kingUnit.CurrentTile, clickedTile)) return null;

        _unitToDeploy.DeployUnit(clickedTile);
        SoundController.Instance?.PlayCall();
        if (_unitToDeploy.SummoningSickness())
        {
            ui.MarkUnitUnavailable(_unitToDeploy);
            _unitToDeploy.IsAvailable = false;
        }
        _unitToDeploy.IsDeployed = true;
        EventManager.Instance.UnitDeployed(_unitToDeploy);
        clickedTile.ClearTile();
        ui.EndDeployment();
        myGrid.HideHighlight();
        return ReturnToActiveUnit(myGameController, myGrid, ui);
    }

    // Leaves the deployment and resumes whatever the player was doing before pressing "Call".
    private IGameState ReturnToActiveUnit(GameController myGameController, BoardGrid myGrid, UIController ui)
    {
        if (_activeUnit == null) return new BeginTurnState(_kingUnit.GetPlayerId());
        if (!_activeUnit.HasMoved) return new UnitSelectedState(_activeUnit, myGrid, ui);
        if (myGrid.HasPossibleAttack(_activeUnit)) return new AttackSelectedState(_activeUnit, myGrid, ui);
        myGrid.HideHighlight();
        return myGameController.FinishUnitActivation(_activeUnit);
    }

    public IGameState UnitClicked(GameController myGameController, UnitController clickedUnit)
    {
        UIController ui;
        BoardGrid myGrid;

        SoundController.Instance?.PlayClick();
        ui = myGameController.GetUI();
        myGrid = myGameController.GetGrid();
        if (clickedUnit.IsDeployed)
        {
            myGrid.HideHighlight();
            ui.EndDeployment();
            if (_activeUnit != null)
            {
                _activeUnit.SetReticle(false);
            }
            if (clickedUnit.HasMoved) return new AttackSelectedState(clickedUnit, myGrid, ui);
            else return new UnitSelectedState(clickedUnit, myGrid, ui);
        }
        else
        {
            _unitToDeploy = clickedUnit;
            ui.DisplayUnit(_unitToDeploy);
            ui.SelectUnit(_unitToDeploy);
            myGrid.ShowZone(_kingUnit.CurrentTile, HighlightType.Deployment);
        }
        return null;
    }

    public IGameState TileHovered(GameController myGameController, TileController hoveredTile)
    {
        UIController ui;

        SoundController.Instance?.PlayHover();
        if (hoveredTile.isWalkable()) hoveredTile.Highlight(HighlightType.Hover, false);
        ui = myGameController.GetUI();
        ui.DisplayTile(hoveredTile);
        return null;
    }

    public IGameState UnitHovered(GameController myGameController, UnitController hoveredUnit)
    {
        UIController ui;

        SoundController.Instance?.PlayHover();
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
        // Deployment can be opened without a selected unit, so the active player comes from the commander.
        return myGameController.ForceEndTurn(_kingUnit.GetPlayerId(), _activeUnit);
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
