using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class EffectConfused : MonoBehaviour, IEffect, IEndturnable
{
    private string _description;
    private UnitController _myUnitController;

    public void InitializeEffect(string myDescription)
    {
        _description = myDescription;
        _myUnitController = GetComponent<UnitController>();
        _myUnitController._freeAttacksCount--;
    }

    public string GetDescription()
    {
        return _description;
    }

    public void EndTurnAction(int playerId)
    {
        if(_myUnitController.GetPlayerId() == playerId)
        {
            _myUnitController._freeAttacksCount++;
            Destroy(this);
        }
    }
}
