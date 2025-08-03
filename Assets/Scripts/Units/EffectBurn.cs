using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class EffectBurn : MonoBehaviour, IEndturnable, IEffect
{
    private int _damage;
    private int _duration;
    private string _description;
    private UnitController _myUnitController;

    public void InitializeEffect(int damage, int duration, string description)
    {
        _damage = damage;
        _duration = duration;
        _description = description;
        _myUnitController = GetComponent<UnitController>();
    }

    public void EndTurnAction(int playerId)
    {
        if (playerId == _myUnitController.GetPlayerId())
        {
            if (_duration > 0) _myUnitController.DamageUnit(_damage);
            _duration--;
            if (_duration == 0) Destroy(this);
        }
    }

    public string GetDescription()
    {
        return _description;
    }
}
