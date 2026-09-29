using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityConfuse : MonoBehaviour, IAbility, IEndturnable
{
    [SerializeField] string _myButtonDescription;
    [SerializeField] string _myDescription;
    [SerializeField] string _myEffectDescription;
    [SerializeField] private AudioClip _mySound;
    [Tooltip("Own turns until Confuse can be used again (2 = every other turn).")]
    [SerializeField, Min(1)] private int _cooldownTurns = 2;
    private UnitController _myUnit;
    private bool _isAvailableThisTurn;
    private int _turnsUntilReady;

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
        if (_myUnit.HasMoved) return new AttackSelectedState(_myUnit, myGrid, ui);
        else return new UnitSelectedState(_myUnit, myGrid, ui);
    }

    public IGameState UnitClicked(GameController myGameController, UnitController clickedUnit)
    {
        UIController ui;
        BoardGrid myGrid;
        EffectConfused myEffectConfused;

        ui = myGameController.GetUI();
        myGrid = myGameController.GetGrid();
        if (clickedUnit.IsDeployed && clickedUnit.GetPlayerId() != _myUnit.GetPlayerId() && _isAvailableThisTurn)
        {
            _myUnit.StartAnimation("UseAbility");
            _myUnit.PlaySound(_mySound);
            myEffectConfused = clickedUnit.gameObject.AddComponent<EffectConfused>();
            myEffectConfused.InitializeEffect(_myEffectDescription);
            EventManager.Instance.AbilityUsed(_myUnit, "Confuse", clickedUnit);
            _isAvailableThisTurn = false;
            _turnsUntilReady = _cooldownTurns;
            myGrid.HideHighlight();
            if (_myUnit.HasMoved) return new AttackSelectedState(_myUnit, myGrid, ui);
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
        unhoveredUnit.CurrentTile.StopAnimatingHighlight();
        return null;
    }

    public void EndTurnAction(int playerId)
    {
        // The opponent ended its turn, so ours starts: count the cooldown down.
        if (playerId == _myUnit.GetPlayerId()) return;
        if (_turnsUntilReady > 0) _turnsUntilReady--;
        _isAvailableThisTurn = _turnsUntilReady == 0;
    }
}
