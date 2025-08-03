using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class EffectIncreaseAttackRange : MonoBehaviour, IAttackRangeModifier, IEffect, IEnterTile
{
    private int _increaseAmount;
    private string _description;
    private UnitController _myUnitController;
    private GridPosition _effectPosition;

    public void InitializeEffect(int range, string myDescription)
    {
        _increaseAmount = range;
        _myUnitController = GetComponent<UnitController>();
        _effectPosition = _myUnitController.GetGridPosition();
        _description = myDescription;
    }

    public int GetAttackRangeModifier()
    {
        return _increaseAmount;
    }

    public void EnterTileAction(TileController newTile)
    {
        if (newTile.GetGridPosition() != _effectPosition) Destroy(this);
    }

    public string GetDescription()
    {
        return _description;
    }
}
