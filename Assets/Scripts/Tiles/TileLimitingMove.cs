using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TileController))]
public class TileLimitingMove : MonoBehaviour, IEndturnable
{
    [SerializeField] private int _movementRangeBonus;
    [SerializeField] private string _effectDescription;
    private TileController _myTileController;

    private void Start()
    {
        _myTileController = GetComponent<TileController>();
    }

    public void EndTurnAction(int playerId)
    {
        UnitController myUnit;
        EffectIncreaseMoveRange myEffect;

        myUnit = _myTileController._myUnit;
        if (myUnit != null && myUnit.GetPlayerId() == playerId && myUnit.gameObject.GetComponent<EffectIncreaseMoveRange>() == null)
        {
            myEffect = myUnit.gameObject.AddComponent<EffectIncreaseMoveRange>();
            myEffect.InitializeEffect(_movementRangeBonus, _effectDescription);
        }
    }
}
