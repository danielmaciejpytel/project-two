using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExecutionState : IGameState
{
    private UnitController _activeUnit;
    private bool _executionEndsUnitTurn;

    public ExecutionState(UnitController activeUnit, bool endTurn)
    {
        _activeUnit = activeUnit;
        _executionEndsUnitTurn = endTurn;
        _activeUnit.SetReticle(false);
        Debug.Log("Stan: Wykonuję akcję ruchu jednostki gracza: " + _activeUnit.GetPlayerId());
    }

    public IGameState TileClicked(GameController myGameController, TileController clickedTile)
    {
        //nothing happens
        SoundController._instance.PlayClick();
        return null;
    }

    public IGameState UnitClicked(GameController myGameController, UnitController clickedUnit)
    {
        //nothing happens
        SoundController._instance.PlayClick();
        return null;
    }

    public IGameState TileHovered(GameController myGameController, TileController hoveredTile)
    {
        //nothing happens
        SoundController._instance.PlayHover();
        return null;
    }

    public IGameState UnitHovered(GameController myGameController, UnitController hoveredUnit)
    {
        //nothing happens
        SoundController._instance.PlayHover();
        return null;
    }

    public IGameState UnitUnhovered(GameController myGameController, UnitController unhoveredUnit)
    {
        //nothing happens
        return null;
    }

    public IGameState ExecutionEnd(GameController myGameController)
    {
        //if move ended change state to Attack Selected State, if attack ended change state to Begin Turn State
        BoardGrid myGrid;
        UIController ui;
        int newPlayer;

        myGrid = myGameController.GetGrid();
        ui = myGameController.GetUI();
        if (_executionEndsUnitTurn || (!myGrid.HasPossibleAttack(_activeUnit) /*&& (ui.AllUnitsDeployed() || myGameController.DeployedThisTurn())*/))
        {
            _activeUnit.SetReticle(false);
            _activeUnit._isAvailable = false;
            ui.MarkUnitUnavailable(_activeUnit);
            if (myGameController.MovesDepleted(_activeUnit.GetPlayerId()))
            {
                myGameController.EndPlayerTurn(_activeUnit.GetPlayerId());
                newPlayer = (_activeUnit.GetPlayerId() == 1 ? 2 : 1);
            }
            else
            {
                newPlayer = _activeUnit.GetPlayerId();
            }
            return new BeginTurnState(newPlayer);
        }
        else return new AttackSelectedState(_activeUnit, myGrid, ui);
    }

    public IGameState EndTurnPressed(GameController myGameController)
    {
        //nothing happens
        return null;
    }

    public IGameState DeploymentPressed(GameController myGameController)
    {
        //nothing happens
        return null;
    }

    public IGameState AbilityPressed(GameController myGameController)
    {
        //nothing happens
        return null;
    }

    public void ChangeMode(GameController myGameController)
    {

    }
}
