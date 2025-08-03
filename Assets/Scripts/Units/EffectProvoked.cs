using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class EffectProvoked : MonoBehaviour, IValidateTarget, IMoveRangeModifier, IEndturnable, IEffect
{
    private UnitController _instigator;
    private string _description;
    private UnitController _myUnitController;

    public void InitializeEffect(UnitController provokingUnit, string myDescription)
    {
        _instigator = provokingUnit;
        _myUnitController = GetComponent<UnitController>();
        _description = myDescription;
    }

    public bool IsTargetValid(UnitController targetUnit)
    {
        if (targetUnit == _instigator) return true;
        else return false;
    }

    public void EndTurnAction(int playerId)
    {
        if(_myUnitController.GetPlayerId() != playerId 
            && (_instigator._isKilled || Mathf.Abs(_instigator.GetGridPosition().x - _myUnitController.GetGridPosition().x) > 1 || Mathf.Abs(_instigator.GetGridPosition().y - _myUnitController.GetGridPosition().y) > 1))
        {
            Destroy(this);
        }
    }

    public int GetMoveRangeModifier()
    {
        return -10;
    }

    public string GetDescription()
    {
        return _description;
    }

    public GridPosition GetValidPosition()
    {
        if (!_instigator._isKilled) return _instigator.GetGridPosition();
        else return new GridPosition();
    }
}
