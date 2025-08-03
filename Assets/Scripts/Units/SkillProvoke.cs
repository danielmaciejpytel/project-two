using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class SkillProvoke : MonoBehaviour, IEnterTile, ISkill
{
    [SerializeField] private string _description;
    [SerializeField] private string _effectDescription;
    private UnitController _myUnitController;

    private void Awake()
    {
        _myUnitController = GetComponent<UnitController>();
    }

    private void AddEffect(TileController neighbourTile)
    {
        EffectProvoked myEffect;
        UnitController neighbourUnit;

        if (neighbourTile != null && neighbourTile._isOccupied)
        {
            neighbourUnit = neighbourTile._myUnit;
            if (neighbourUnit.GetPlayerId() != _myUnitController.GetPlayerId() && neighbourUnit.gameObject.GetComponent<EffectProvoked>() == null)
            {
                myEffect = neighbourTile._myUnit.gameObject.AddComponent<EffectProvoked>();
                myEffect.InitializeEffect(_myUnitController, _effectDescription);
            }
        }
    }

    public void EnterTileAction(TileController newTile)
    {
        TileController neighbourTile;

        neighbourTile = _myUnitController._myTile.GetAnotherTile(_myUnitController.GetGridPosition().x - 1, _myUnitController.GetGridPosition().y - 1);
        AddEffect(neighbourTile);
        neighbourTile = _myUnitController._myTile.GetAnotherTile(_myUnitController.GetGridPosition().x - 1, _myUnitController.GetGridPosition().y);
        AddEffect(neighbourTile);
        neighbourTile = _myUnitController._myTile.GetAnotherTile(_myUnitController.GetGridPosition().x - 1, _myUnitController.GetGridPosition().y + 1);
        AddEffect(neighbourTile);
        neighbourTile = _myUnitController._myTile.GetAnotherTile(_myUnitController.GetGridPosition().x, _myUnitController.GetGridPosition().y - 1);
        AddEffect(neighbourTile);
        neighbourTile = _myUnitController._myTile.GetAnotherTile(_myUnitController.GetGridPosition().x, _myUnitController.GetGridPosition().y + 1);
        AddEffect(neighbourTile);
        neighbourTile = _myUnitController._myTile.GetAnotherTile(_myUnitController.GetGridPosition().x + 1, _myUnitController.GetGridPosition().y - 1);
        AddEffect(neighbourTile);
        neighbourTile = _myUnitController._myTile.GetAnotherTile(_myUnitController.GetGridPosition().x + 1, _myUnitController.GetGridPosition().y);
        AddEffect(neighbourTile);
        neighbourTile = _myUnitController._myTile.GetAnotherTile(_myUnitController.GetGridPosition().x + 1, _myUnitController.GetGridPosition().y + 1);
        AddEffect(neighbourTile);
    }

    public string GetDescription()
    {
        return _description;
    }
}
