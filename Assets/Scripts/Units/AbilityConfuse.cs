using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityConfuse : MonoBehaviour, IAbility, IEndturnable
{
    [SerializeField] string _myButtonDescription;
    [SerializeField] string _myDescription;
    [SerializeField] string _myEffectDescription;
    [SerializeField] private AudioClip _mySound;
    private UnitController _myUnit;
    private bool _isAvailableThisTurn;

    // Start is called before the first frame update
    void Start()
    {
        _myUnit = GetComponent<UnitController>();
        _isAvailableThisTurn = true;
    }

    public bool IsAvailableThisTurn()
    {
        return _isAvailableThisTurn;
    }

    public void StartAction(GameController myGameController)
    {
        myGameController.HighlightUnits(_myUnit.GetPlayerId() == 1 ? 2 : 1, true);
    }

    public string GetButtonDescription()
    {
        return _myButtonDescription;
    }

    public string GetDescription()
    {
        return _myDescription;
    }

    public IGameState TileClicked(GameController myGameController, TileController clickedTile)
    {
        UIController ui;
        BoardGrid myGrid;

        ui = myGameController.GetUI();
        myGrid = myGameController.GetGrid();
        if (_myUnit._hasMoved) return new AttackSelectedState(_myUnit, myGrid, ui);
        else return new UnitSelectedState(_myUnit, myGrid, ui);
    }

    public IGameState UnitClicked(GameController myGameController, UnitController clickedUnit)
    {
        UIController ui;
        BoardGrid myGrid;
        EffectConfused myEffectConfused;

        ui = myGameController.GetUI();
        myGrid = myGameController.GetGrid();
        if (clickedUnit._isDeployed && clickedUnit.GetPlayerId() != _myUnit.GetPlayerId() && _isAvailableThisTurn)
        {
            _myUnit.StartAnimation("UseAbility");
            _myUnit.PlaySound(_mySound);
            myEffectConfused = clickedUnit.gameObject.AddComponent<EffectConfused>();
            myEffectConfused.InitializeEffect(_myEffectDescription);
            _isAvailableThisTurn = false;
            myGrid.HideHighlight();
            if (_myUnit._hasMoved) return new AttackSelectedState(_myUnit, myGrid, ui);
            else return new UnitSelectedState(_myUnit, myGrid, ui);
        }
        return null;
    }

    public IGameState TileHovered(GameController myGameController, TileController hoveredTile)
    {
        UIController ui;

        if (hoveredTile.isWalkable()) hoveredTile.Highlight(HighlightType.Hover, false);
        ui = myGameController.GetUI();
        ui.DisplayTile(hoveredTile);
        return null;
    }

    public IGameState UnitHovered(GameController myGameController, UnitController hoveredUnit)
    {
        UIController ui;

        ui = myGameController.GetUI();
        ui.DisplayUnit(hoveredUnit);
        return null;
    }

    public IGameState UnitUnhovered(GameController myGameController, UnitController unhoveredUnit)
    {
        UIController ui;

        ui = myGameController.GetUI();
        ui.ClearDisplay();
        unhoveredUnit._myTile.StopAnimatingHighlight();
        return null;
    }

    public void EndTurnAction(int playerId)
    {
        if (playerId != _myUnit.GetPlayerId()) _isAvailableThisTurn = true;
    }
}
