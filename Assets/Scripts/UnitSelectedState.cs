using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitSelectedState : IGameState
{
    private UnitController _activeUnit;

    public UnitSelectedState(UnitController uc, BoardGrid myGrid, UIController ui)
    {
        _activeUnit = uc;
        _activeUnit.SetReticle(true);
        myGrid.ShowMoveRange(_activeUnit.GetGridPosition(), _activeUnit.GetMoveRange());
        if(_activeUnit.FreeAttacksCount > 0) myGrid.ShowAttackRange(uc, uc.GetAttackRange(), _activeUnit.GetPlayerId());
        ui.DisplayUnit(uc);
        ui.SelectUnit(uc);
        Debug.Log("Stan: Wybrana jednostka gracza: " + _activeUnit.GetPlayerId());
    }

    public IGameState TileClicked(GameController myGameController, TileController clickedTile)
    {
        BoardGrid myGrid;
        // if tile in move range change state to execution, if not go back to begin turn state
        SoundController.Instance?.PlayClick();
        myGrid = myGameController.GetGrid();
        if (myGrid.IsTileInMoveRange(_activeUnit, clickedTile))
        {
            _activeUnit.MoveUnit(myGrid.FindPath(_activeUnit.GetGridPosition(), clickedTile.GetGridPosition()));
            myGrid.HideHighlight();
            return new ExecutionState(_activeUnit, false);
        }
        else
        {
            myGrid.HideHighlight();
            _activeUnit.SetReticle(false);
            return new BeginTurnState(_activeUnit.GetPlayerId());
        }
    }

    public IGameState UnitClicked(GameController myGameController, UnitController clickedUnit)
    {
        BoardGrid myGrid;
        UIController ui;
        bool attackEndsTurn;
        // if it's active player's unit, change state to selected unit if not go back to begin turn state
        SoundController.Instance?.PlayClick();
        myGrid = myGameController.GetGrid();
        ui = myGameController.GetUI();
        if (_activeUnit.GetPlayerId() == clickedUnit.GetPlayerId() && _activeUnit != clickedUnit && clickedUnit.IsAvailable && clickedUnit.IsDeployed)
        {
            myGrid.HideHighlight();
            _activeUnit.SetReticle(false);
            if (clickedUnit.HasMoved && clickedUnit.FreeAttacksCount > 0) return new AttackSelectedState(clickedUnit, myGrid, ui);
            else if (clickedUnit.FreeAttacksCount > 0) return new UnitSelectedState(clickedUnit, myGrid, ui);
            else return null;
        }
        else if(_activeUnit.GetPlayerId() != clickedUnit.GetPlayerId() && _activeUnit.FreeAttacksCount > 0)
        {
            if (_activeUnit.IsTargetValid(clickedUnit) && myGrid.IsTileInAttackRange(_activeUnit, clickedUnit.CurrentTile))
            {
                myGrid.HideHighlight();
                _activeUnit.SetReticle(false);
                clickedUnit.StopShowingPotentialDamage();
                _activeUnit.AttackUnit(clickedUnit);
                if (_activeUnit.FreeAttacksCount < 1) attackEndsTurn = true;
                else attackEndsTurn = false;
                return new ExecutionState(_activeUnit, attackEndsTurn);
            }
            else
            {
                myGrid.HideHighlight();
                _activeUnit.SetReticle(false);
                return new BeginTurnState(_activeUnit.GetPlayerId());
            }
        }
        else return null;
    }

    public IGameState TileHovered(GameController myGameController, TileController hoveredTile)
    {
        // highlight tile
        BoardGrid myGrid;
        UIController ui;

        SoundController.Instance?.PlayHover();
        myGrid = myGameController.GetGrid();
        myGrid.ShowPath(_activeUnit, hoveredTile);
        ui = myGameController.GetUI();
        if (myGrid.IsTileInMoveRange(_activeUnit, hoveredTile)) ui.DisplayTile(hoveredTile);
        else ui.DisplayUnit(_activeUnit);
        return null;
    }

    public IGameState UnitHovered(GameController myGameController, UnitController hoveredUnit)
    {
        //show units move and attack ranges
        BoardGrid myGrid;
        UIController ui;

        SoundController.Instance?.PlayHover();
        myGrid = myGameController.GetGrid();
        ui = myGameController.GetUI();
        ui.DisplayUnit(hoveredUnit);
        // Ranges are shown only for the units of the team that is playing; an enemy in reach shows the damage it would take.
        if (_activeUnit.IsTargetValid(hoveredUnit) && myGrid.IsTileInAttackRange(_activeUnit, hoveredUnit.CurrentTile) && _activeUnit.FreeAttacksCount > 0)
        {
            hoveredUnit.ShowPotentialDamage(_activeUnit.GetCalculatedAttack(hoveredUnit));
        }
        return null;
    }

    public IGameState UnitUnhovered(GameController myGameController, UnitController unhoveredUnit)
    {
        BoardGrid myGrid;
        UIController ui;

        ui = myGameController.GetUI();
        ui.ClearDisplay();
        myGrid = myGameController.GetGrid();
        if (_activeUnit.IsTargetValid(unhoveredUnit) && !myGrid.IsTileInAttackRange(_activeUnit, unhoveredUnit.CurrentTile))
        {
            myGrid.HideHighlight();
            myGrid.ShowMoveRange(_activeUnit.GetGridPosition(), _activeUnit.GetMoveRange());
            myGrid.ShowAttackRange(_activeUnit, _activeUnit.GetAttackRange(), _activeUnit.GetPlayerId());
            _activeUnit.HighlighUnitTile(HighlightType.Unit);
        }
        else if (_activeUnit.IsTargetValid(unhoveredUnit)) unhoveredUnit.StopShowingPotentialDamage();
        return null;
    }

    public IGameState ExecutionEnd(GameController myGameController)
    {
        //nothing happens
        return null;
    }

    public IGameState EndTurnPressed(GameController myGameController)
    {
        return myGameController.ForceEndTurn(_activeUnit.GetPlayerId(), _activeUnit);
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
        return new AbilityState(myGameController, _activeUnit);
    }

    public void ChangeMode(GameController myGameController)
    {
        BoardGrid myGrid;

        myGrid = myGameController.GetGrid();
        myGrid.ChangeMode();
    }
}
