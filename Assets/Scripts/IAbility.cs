using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAbility 
{
    void StartAction(GameController myGameController);
    string GetButtonDescription();
    string GetDescription();
    bool IsAvailableThisTurn();
    IGameState TileClicked(GameController myGameController, TileController clickedTile);
    IGameState UnitClicked(GameController myGameController, UnitController clickedUnit);
    IGameState TileHovered(GameController myGameController, TileController hoveredTile);
    IGameState UnitHovered(GameController myGameController, UnitController hoveredUnit);
    IGameState UnitUnhovered(GameController myGameController, UnitController unhoveredUnit);
}
