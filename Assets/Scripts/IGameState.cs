using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGameState
{
    IGameState TileClicked(GameController myGameController, TileController clickedTile);
    IGameState UnitClicked(GameController myGameController, UnitController clickedUnit);
    IGameState TileHovered(GameController myGameController, TileController hoveredTile);
    IGameState UnitHovered(GameController myGameController, UnitController hoveredUnit);
    IGameState UnitUnhovered(GameController myGameController, UnitController unhoveredUnit);
    IGameState ExecutionEnd(GameController myGameController);
    IGameState EndTurnPressed(GameController myGameController);
    IGameState DeploymentPressed(GameController myGameController);
    IGameState AbilityPressed(GameController myGameController);
    void ChangeMode(GameController myGameController);
}
