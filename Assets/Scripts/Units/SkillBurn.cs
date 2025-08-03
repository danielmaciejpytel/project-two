using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class SkillBurn : MonoBehaviour, IAddEffect, ISkill
{
    [SerializeField] private int _burnAmount;
    [SerializeField] private int _burnTurns;
    [SerializeField] private string _description;
    [SerializeField] private string _effectDescription;
    private UnitController _myUnitController;

    private void Awake()
    {
        _myUnitController = GetComponent<UnitController>();
    }

    public void AddEffect(UnitController target)
    {
        EffectBurn myBurnEffect;
        if(target.gameObject.GetComponent<EffectBurn>() == null)
        {
            myBurnEffect = target.gameObject.AddComponent<EffectBurn>();
            myBurnEffect.InitializeEffect(_burnAmount, _burnTurns, _effectDescription);
        }
    }

    public string GetDescription()
    {
        return _description;
    }
}
