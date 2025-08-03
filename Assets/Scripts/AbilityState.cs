using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityState : IGameState
{
    private UnitController _activeUnit;
    private IAbility _activeAbility;

    public AbilityState(GameController myGameController, UnitController unit)
    {
        BoardGrid myGrid;
        myGrid = myGameController.GetGrid();
        _activeUnit = unit;
        myGrid.HideHighlight();
        _activeAbility = _activeUnit.GetComponent<IAbility>();
        _activeAbility.StartAction(myGameController);
    }

    public IGameState TileClicked(GameController myGameController, TileController clickedTile)
    {
        SoundController._instance.PlayClick();
        return _activeAbility.TileClicked(myGameController, clickedTile);
    }

    public IGameState UnitClicked(GameController myGameController, UnitController clickedUnit)
    {
        SoundController._instance.PlayClick();
        return _activeAbility.UnitClicked(myGameController, clickedUnit);
    }

    public IGameState TileHovered(GameController myGameController, TileController hoveredTile)
    {
        SoundController._instance.PlayHover();
        return _activeAbility.TileHovered(myGameController, hoveredTile);
    }

    public IGameState UnitHovered(GameController myGameController, UnitController hoveredUnit)
    {
        SoundController._instance.PlayHover();
        hoveredUnit._myTile.AnimateHighlight();
        return _activeAbility.UnitHovered(myGameController, hoveredUnit);
    }

    public IGameState UnitUnhovered(GameController myGameController, UnitController unhoveredUnit)
    {
        return _activeAbility.UnitUnhovered(myGameController, unhoveredUnit);
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
        UIController ui;
        BoardGrid myGrid;
        UnitController king;
        ui = myGameController.GetUI();
        myGrid = myGameController.GetGrid();
        king = myGameController.GetCommander(_activeUnit.GetPlayerId());
        return new DeploymentState(_activeUnit, king, myGrid, ui);
    }

    public IGameState AbilityPressed(GameController myGameController)
    {
        //nothing happens
        return null;
    }

    public void ChangeMode(GameController myGameController)
    {
        BoardGrid myGrid;

        myGrid = myGameController.GetGrid();
        myGrid.ChangeMode();
    }
}
