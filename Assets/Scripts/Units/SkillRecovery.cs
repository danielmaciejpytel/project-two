using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class SkillRecovery : MonoBehaviour, IEndturnable, ISkill
{
    [SerializeField] private int _recoveryRate;
    [SerializeField] private string _description;
    private UnitController _myUnitController;

    private void Awake()
    {
        _myUnitController = GetComponent<UnitController>();
    }

    public void EndTurnAction(int playerId)
    {
        if(_myUnitController.GetPlayerId() == playerId) _myUnitController.HealUnit(_recoveryRate);
    }

    public string GetDescription()
    {
        return _description;
    }
}
