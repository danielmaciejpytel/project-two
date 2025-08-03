using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class EffectIncreaseArmor : MonoBehaviour, IArmorModifier, IEnterTile, IEffect
{
    private int _increaseAmount;
    private string _description;
    private UnitController _myUnitController;
    private GridPosition _effectPosition;

    public void InitializeEffect(int armor, string myDescription)
    {
        _increaseAmount = armor;
        _description = myDescription;
        _myUnitController = GetComponent<UnitController>();
        _effectPosition = _myUnitController.GetGridPosition();
    }

    public int GetArmorModifier()
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
