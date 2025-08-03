using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class SkillPoison : MonoBehaviour, IAddEffect, ISkill
{
    [SerializeField] private int _weaknessAmount;
    [SerializeField] private int _weaknessTurns;
    [SerializeField] private string _description;
    [SerializeField] private string _effectDescription;
    private UnitController _myUnitController;

    private void Awake()
    {
        _myUnitController = GetComponent<UnitController>();
    }

    public void AddEffect(UnitController target)
    {
        EffectPoison myPoisonEffect;
        if (target.gameObject.GetComponent<EffectPoison>() == null)
        {
            myPoisonEffect = target.gameObject.AddComponent<EffectPoison>();
            myPoisonEffect.InitializeEffect(_weaknessAmount, _weaknessTurns, _effectDescription);
        }
    }

    public string GetDescription()
    {
        return _description;
    }
}
