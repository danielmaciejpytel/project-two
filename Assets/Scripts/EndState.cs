using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndState : IGameState
{
    public EndState(GameController myGameController, int winnerId)
    {
        UIController myUIController;
        myUIController = myGameController.GetUI();
        myUIController.DisplayWinner(winnerId);
    }

    public IGameState TileClicked(GameController myGameController, TileController clickedTile)
    {
        //nothing happens
        return null;
    }

    public IGameState UnitClicked(GameController myGameController, UnitController clickedUnit)
    {
        //nothing happens
        return null;
    }

    public IGameState TileHovered(GameController myGameController, TileController hoveredTile)
    {
        //nothing happens
        return null;
    }

    public IGameState UnitHovered(GameController myGameController, UnitController hoveredUnit)
    {
        //nothing happens
        return null;
    }

    public IGameState UnitUnhovered(GameController myGameController, UnitController unhoveredUnit)
    {
        //nothing happens
        return null;
    }

    public IGameState ExecutionEnd(GameController myGameController)
    {
        //nothing happens
        return null;
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
        BoardGrid myGrid;

        myGrid = myGameController.GetGrid();
        myGrid.ChangeMode();
    }
}
