using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class SkillZeal : MonoBehaviour, IAttackModifier, IEnterTile, ISkill
{
    [SerializeField] private int _attackBonus;
    [SerializeField] private int _healthBonus;
    [SerializeField] private string _description;
    private UnitController _myUnitController;
    private bool _isAdjacentToKing;

    private void Awake()
    {
        _myUnitController = GetComponent<UnitController>();
        _isAdjacentToKing = false;
        EventManager._instance.OnExecutionEnd += OnExecutionEnded;
    }

    private void OnDestroy()
    {
        EventManager._instance.OnExecutionEnd -= OnExecutionEnded;
    }

    private void OnExecutionEnded(UnitController unit)
    {
        if(_myUnitController._isDeployed && unit.GetPlayerId() == _myUnitController.GetPlayerId() && unit.IsKing())
        {
            EnterTileAction(_myUnitController._myTile);
        }
    }

    public void EnterTileAction(TileController newTile)
    {
        bool isKingClose = false;
        TileController neighbourTile;

        neighbourTile = newTile.GetAnotherTile(_myUnitController.GetGridPosition().x - 1, _myUnitController.GetGridPosition().y -1);
        if (neighbourTile != null && neighbourTile._isOccupied && neighbourTile._myUnit.IsKing() && neighbourTile._myUnit.GetPlayerId() == _myUnitController.GetPlayerId()) isKingClose = true;
        neighbourTile = newTile.GetAnotherTile(_myUnitController.GetGridPosition().x - 1, _myUnitController.GetGridPosition().y);
        if (neighbourTile != null && neighbourTile._isOccupied && neighbourTile._myUnit.IsKing() && neighbourTile._myUnit.GetPlayerId() == _myUnitController.GetPlayerId()) isKingClose = true;
        neighbourTile = newTile.GetAnotherTile(_myUnitController.GetGridPosition().x - 1, _myUnitController.GetGridPosition().y + 1);
        if (neighbourTile != null && neighbourTile._isOccupied && neighbourTile._myUnit.IsKing() && neighbourTile._myUnit.GetPlayerId() == _myUnitController.GetPlayerId()) isKingClose = true;
        neighbourTile = newTile.GetAnotherTile(_myUnitController.GetGridPosition().x, _myUnitController.GetGridPosition().y - 1);
        if (neighbourTile != null && neighbourTile._isOccupied && neighbourTile._myUnit.IsKing() && neighbourTile._myUnit.GetPlayerId() == _myUnitController.GetPlayerId()) isKingClose = true;
        neighbourTile = newTile.GetAnotherTile(_myUnitController.GetGridPosition().x, _myUnitController.GetGridPosition().y + 1);
        if (neighbourTile != null && neighbourTile._isOccupied && neighbourTile._myUnit.IsKing() && neighbourTile._myUnit.GetPlayerId() == _myUnitController.GetPlayerId()) isKingClose = true;
        neighbourTile = newTile.GetAnotherTile(_myUnitController.GetGridPosition().x + 1, _myUnitController.GetGridPosition().y - 1);
        if (neighbourTile != null && neighbourTile._isOccupied && neighbourTile._myUnit.IsKing() && neighbourTile._myUnit.GetPlayerId() == _myUnitController.GetPlayerId()) isKingClose = true;
        neighbourTile = newTile.GetAnotherTile(_myUnitController.GetGridPosition().x + 1, _myUnitController.GetGridPosition().y);
        if (neighbourTile != null && neighbourTile._isOccupied && neighbourTile._myUnit.IsKing() && neighbourTile._myUnit.GetPlayerId() == _myUnitController.GetPlayerId()) isKingClose = true;
        neighbourTile = newTile.GetAnotherTile(_myUnitController.GetGridPosition().x + 1, _myUnitController.GetGridPosition().y + 1);
        if (neighbourTile != null && neighbourTile._isOccupied && neighbourTile._myUnit.IsKing() && neighbourTile._myUnit.GetPlayerId() == _myUnitController.GetPlayerId()) isKingClose = true;
        if(!_isAdjacentToKing && isKingClose)
        {
            _isAdjacentToKing = isKingClose;
            _myUnitController.ChangeHP(_healthBonus);
        }
        else if(_isAdjacentToKing && !isKingClose)
        {
            _isAdjacentToKing = isKingClose;
            _myUnitController.ChangeHP(-_healthBonus);
        }
    }

    public int GetAttackModifier(UnitController target)
    {
        if (_isAdjacentToKing) return _attackBonus;
        else return 0;

    }

    public string GetDescription()
    {
        return _description;
    }
}
