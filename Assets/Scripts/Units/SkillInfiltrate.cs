using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class SkillInfiltrate : MonoBehaviour, IAttackModifier, IEnterTile, ISkill
{
    [SerializeField] private int _attackBonus;
    [SerializeField] private int _healthBonus;
    [SerializeField] private string _description;
    private UnitController _myUnitController;
    private bool _isInfiltrating;

    private void Awake()
    {
        _myUnitController = GetComponent<UnitController>();
        _isInfiltrating = false;
    }

    public void EnterTileAction(TileController newTile)
    {
        if(newTile.GetPlayerZone() != _myUnitController.GetPlayerId())
        {
            if (!_isInfiltrating) _myUnitController.ChangeHP(_healthBonus);
            _isInfiltrating = true;
        }
        else
        {
            if(_isInfiltrating) _myUnitController.ChangeHP(-_healthBonus);
            _isInfiltrating = false;
        }
    }

    public int GetAttackModifier(UnitController target)
    {
        if (_isInfiltrating) return _attackBonus;
        else return 0;
    }

    public string GetDescription()
    {
        return _description;
    }
}
