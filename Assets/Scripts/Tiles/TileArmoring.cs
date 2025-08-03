using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TileController))]
public class TileArmoring : MonoBehaviour, IEndturnable
{
    [SerializeField] private int _armorBonus;
    [SerializeField] private string _effectDescription;
    private TileController _myTileController;

    private void Start()
    {
        _myTileController = GetComponent<TileController>();
    }

    public void EndTurnAction(int playerId)
    {
        UnitController myUnit;
        EffectIncreaseArmor myEffect;

        myUnit = _myTileController._myUnit;
        if (myUnit != null && myUnit.GetPlayerId() == playerId && myUnit.gameObject.GetComponent<EffectIncreaseArmor>() == null)
        {
            myEffect = myUnit.gameObject.AddComponent<EffectIncreaseArmor>();
            myEffect.InitializeEffect(_armorBonus, _effectDescription);
        }
    }
}
