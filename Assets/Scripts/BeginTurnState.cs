using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeginTurnState : IGameState
{
    private int _activePlayerId;

    public BeginTurnState(int playerId)
    {
        _activePlayerId = playerId;
        Debug.Log("Stan: Początek tury dla gracza: " + _activePlayerId);
    }

    public IGameState TileClicked(GameController myGameController, TileController clickedTile)  
    {
        //nothing happens
        SoundController._instance.PlayClick();
        return null;
    }

    public IGameState UnitClicked(GameController myGameController, UnitController clickedUnit)
    {
        //if it's active player's unit and this unit is available, change state to selected unit 
        BoardGrid myGrid;
        UIController ui;
        SoundController._instance.PlayClick();
        if (clickedUnit.GetPlayerId() == _activePlayerId && clickedUnit._isAvailable && clickedUnit._isDeployed)
        {
            myGrid = myGameController.GetGrid();
            ui = myGameController.GetUI();
            if (clickedUnit._hasMoved) return new AttackSelectedState(clickedUnit, myGrid, ui);
            else return new UnitSelectedState(clickedUnit, myGrid, ui);
        }
        return null;
    }

    public IGameState TileHovered(GameController myGameController, TileController hoveredTile)
    {
        //highlight tile
        UIController ui;

        SoundController._instance.PlayHover();
        if (hoveredTile.isWalkable()) hoveredTile.Highlight(HighlightType.Hover, false);
        ui = myGameController.GetUI();
        ui.DisplayTile(hoveredTile);
        return null;
    }

    public IGameState UnitHovered(GameController myGameController, UnitController hoveredUnit)
    {
        //show units move and attack ranges
        BoardGrid myGrid;
        UIController ui;

        SoundController._instance.PlayHover();
        myGrid = myGameController.GetGrid();
        myGrid.ShowMoveRange(hoveredUnit.GetGridPosition(), hoveredUnit.GetMoveRange());
        if(hoveredUnit._freeAttacksCount > 0) myGrid.ShowAttackRange(hoveredUnit, hoveredUnit.GetAttackRange(), hoveredUnit.GetPlayerId());
        ui = myGameController.GetUI();
        ui.DisplayUnit(hoveredUnit);
        hoveredUnit.HighlighUnitTile(HighlightType.Unit);
        return null;
    }

    public IGameState UnitUnhovered(GameController myGameController, UnitController unhoveredUnit)
    {
        //clear board
        BoardGrid myGrid;
        myGrid = myGameController.GetGrid();
        myGrid.HideHighlight();
        return null;
    }

    public IGameState ExecutionEnd(GameController myGameController)
    {
        //nothing happens
        return null;
    }

    public IGameState EndTurnPressed(GameController myGameController)
    {
        //change player turn
        int newPlayer;

        myGameController.EndPlayerTurn(_activePlayerId);
        newPlayer = (_activePlayerId == 1 ? 2 : 1);
        return new BeginTurnState(newPlayer);
    }

    public IGameState DeploymentPressed(GameController myGameController)
    {
        UIController ui;
        BoardGrid myGrid;
        UnitController king;
        ui = myGameController.GetUI();
        myGrid = myGameController.GetGrid();
        king = myGameController.GetCommander(_activePlayerId);
        return new DeploymentState(null, king, myGrid, ui);
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
