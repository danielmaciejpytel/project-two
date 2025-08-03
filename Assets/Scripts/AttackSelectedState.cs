using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackSelectedState : IGameState
{
    private UnitController _activeUnit;

    public AttackSelectedState(UnitController uc, BoardGrid myGrid, UIController ui)
    {
        _activeUnit = uc;
        _activeUnit.SetReticle(true);
        ui.DisplayUnit(uc);
        ui.SelectUnit(uc);
        myGrid.HideHighlight();
        myGrid.ShowAttackRange(uc, uc.GetAttackRange(), _activeUnit.GetPlayerId());
        Debug.Log("Stan: Wybrany atak jednostki gracza: " + _activeUnit.GetPlayerId());
    }

    public IGameState TileClicked(GameController myGameController, TileController clickedTile)
    {
        //nothing happens
        SoundController._instance.PlayClick();
        return null;
    }

    public IGameState UnitClicked(GameController myGameController, UnitController clickedUnit)
    {
        BoardGrid myGrid;
        UIController ui;
        bool attackEndsTurn;

        SoundController._instance.PlayClick();
        ui = myGameController.GetUI();
        myGrid = myGameController.GetGrid();
        if (_activeUnit.IsTargetValid(clickedUnit) && myGrid.IsTileInAttackRange(_activeUnit, clickedUnit._myTile))
        {
            myGrid.HideHighlight();
            _activeUnit.SetReticle(false);
            clickedUnit.StopShowingPotentialDamage();
            _activeUnit.AttackUnit(clickedUnit);
            if (_activeUnit._freeAttacksCount < 1) attackEndsTurn = true;
            else attackEndsTurn = false;
            return new ExecutionState(_activeUnit, attackEndsTurn);
        }
        else if (_activeUnit.GetPlayerId() == clickedUnit.GetPlayerId() && _activeUnit != clickedUnit && clickedUnit._isAvailable && clickedUnit._isDeployed)
        {
            myGrid.HideHighlight();
            _activeUnit.SetReticle(false);
            if (clickedUnit._hasMoved) return new AttackSelectedState(clickedUnit, myGrid, ui);
            else return new UnitSelectedState(clickedUnit, myGrid, ui);
        }
        else return null;
    }

    public IGameState TileHovered(GameController myGameController, TileController hoveredTile)
    {
        //highlight tile
        SoundController._instance.PlayHover();
        if (hoveredTile.isWalkable()) hoveredTile.Highlight(HighlightType.Hover, false);
        return null;
    }

    public IGameState UnitHovered(GameController myGameController, UnitController hoveredUnit)
    {
        //show units move and attack ranges
        BoardGrid myGrid;
        UIController ui;

        SoundController._instance.PlayHover();
        ui = myGameController.GetUI();
        myGrid = myGameController.GetGrid();
        ui.DisplayUnit(hoveredUnit);
        if (_activeUnit.IsTargetValid(hoveredUnit) && !myGrid.IsTileInAttackRange(_activeUnit, hoveredUnit._myTile))
        {
            myGrid.HideHighlight();
            myGrid.ShowMoveRange(hoveredUnit.GetGridPosition(), hoveredUnit.GetMoveRange());
            if (hoveredUnit._freeAttacksCount > 0) myGrid.ShowAttackRange(hoveredUnit, hoveredUnit.GetAttackRange(), hoveredUnit.GetPlayerId());
        }
        else if(_activeUnit.IsTargetValid(hoveredUnit) && _activeUnit._freeAttacksCount > 0) hoveredUnit.ShowPotentialDamage(_activeUnit.GetCalculatedAttack(hoveredUnit));
        return null;
    }

    public IGameState UnitUnhovered(GameController myGameController, UnitController unhoveredUnit)
    {
        //clear board
        BoardGrid myGrid;
        UIController ui;

        ui = myGameController.GetUI();
        myGrid = myGameController.GetGrid();
        ui.DisplayUnit(_activeUnit);
        if (_activeUnit.IsTargetValid(unhoveredUnit) && !myGrid.IsTileInAttackRange(_activeUnit, unhoveredUnit._myTile))
        {
            myGrid.HideHighlight();
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
        // disable unit reticle and turn off highlight
        BoardGrid myGrid;
        UIController ui;
        int newPlayer;

        ui = myGameController.GetUI();
        myGrid = myGameController.GetGrid();
        myGrid.HideHighlight();
        _activeUnit.SetReticle(false);
        _activeUnit._isAvailable = false;
        ui.MarkUnitUnavailable(_activeUnit);
        myGameController.EndPlayerTurn(_activeUnit.GetPlayerId());
        newPlayer = (_activeUnit.GetPlayerId() == 1 ? 2 : 1);
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
        return new AbilityState(myGameController, _activeUnit);
    }

    public void ChangeMode(GameController myGameController)
    {
        BoardGrid myGrid;

        myGrid = myGameController.GetGrid();
        myGrid.ChangeMode();
    }
}
