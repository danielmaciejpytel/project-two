using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class SkillBackstab : MonoBehaviour, IAttackModifier, ISkill
{
    [SerializeField] private int _attackBonus;
    [SerializeField] private string _description;
    private UnitController _myUnitController;

    private void Awake()
    {
        _myUnitController = GetComponent<UnitController>();
    }

    public int GetAttackModifier(UnitController target)
    {
        GridPosition targetPosition;
        targetPosition = target.GetGridPosition();
        if (_myUnitController.GetPlayerId() == 1 && _myUnitController.GetGridPosition().x == target.GetGridPosition().x + 1
            || _myUnitController.GetPlayerId() == 2 && _myUnitController.GetGridPosition().x == target.GetGridPosition().x - 1) return _attackBonus;
        else return 0;

    }

    public string GetDescription()
    {
        return _description;
    }
}
