using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TileController))]
public class TileIncreasingRange : MonoBehaviour, ITileBehaviour
{
    [SerializeField] private int _attackRangeBonus;
    [SerializeField] private string _effectDescription;
    private TileController _myTileController;

    private void Start()
    {
        _myTileController = GetComponent<TileController>();
    }

    public void EnterTileAction(UnitController myUnit)
    {
        EffectIncreaseAttackRange myEffect;
        if (myUnit.gameObject.GetComponent<EffectIncreaseAttackRange>() == null)
        {
            myEffect = myUnit.gameObject.AddComponent<EffectIncreaseAttackRange>();
            myEffect.InitializeEffect(_attackRangeBonus, _effectDescription);
        }
    }
}
