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
        SoundController.Instance?.PlayClick();
        return null;
    }

    public IGameState UnitClicked(GameController myGameController, UnitController clickedUnit)
    {
        //nothing happens
        SoundController.Instance?.PlayClick();
        return null;
    }

    public IGameState TileHovered(GameController myGameController, TileController hoveredTile)
    {
        //nothing happens
        SoundController.Instance?.PlayHover();
        return null;
    }

    public IGameState UnitHovered(GameController myGameController, UnitController hoveredUnit)
    {
        //nothing happens
        SoundController.Instance?.PlayHover();
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

        myGrid = myGameController.GetGrid();
        ui = myGameController.GetUI();
        if (_executionEndsUnitTurn || !myGrid.HasPossibleAttack(_activeUnit)) return myGameController.FinishUnitActivation(_activeUnit);
        return new AttackSelectedState(_activeUnit, myGrid, ui);
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
