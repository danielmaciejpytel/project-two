using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class EffectPoison : MonoBehaviour, IDamageModifier, IEndturnable, IEffect
{
    private int _damage;
    private int _duration;
    private string _description;
    private UnitController _myUnitController;

    public void InitializeEffect(int damage, int duration, string myDescription)
    {
        _damage = damage;
        _duration = duration;
        _myUnitController = GetComponent<UnitController>();
        _description = myDescription;
    }

    public int GetDamageModifier()
    {
        return _damage;
    }

    public void EndTurnAction(int playerId)
    {
        if (playerId == _myUnitController.GetPlayerId())
        {
            _duration--;
            if (_duration == 0) Destroy(this);
        }
    }

    public string GetDescription()
    {
        return _description;
    }
}
