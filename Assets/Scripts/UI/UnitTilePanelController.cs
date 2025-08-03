using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitTilePanelController : MonoBehaviour
{
    [SerializeField] Text _name;
    [SerializeField] Text _description;
    [SerializeField] Text _hpText;
    [SerializeField] Text _hp;
    [SerializeField] Text _moveRangeText;
    [SerializeField] Text _moveRange;
    [SerializeField] Text _attackStrengthText;
    [SerializeField] Text _attackStrength;
    [SerializeField] Text _skillsText;
    [SerializeField] Text _skills;
    [SerializeField] Text _effectsText;
    [SerializeField] Text _effects;

    private UnitController _myUnit;

    // Start is called before the first frame update
    void Awake()
    {
        _hpText.enabled = false;
        _moveRangeText.enabled = false;
        _attackStrengthText.enabled = false;
        _skillsText.enabled = false;
        if(_effectsText != null) _effectsText.enabled = false;
    }

    public void DisplayTile(TileController myTile)
    {
        _name.text = myTile.GetTileName();
        _description.text = myTile.GetDescription();
        _hpText.enabled = false;
        _moveRangeText.enabled = false;
        _attackStrengthText.enabled = false;
        _skillsText.enabled = false;
        _effectsText.enabled = false;
        _hp.text = "";
        _moveRange.text = "";
        _attackStrength.text = "";
        _skills.text = "";
        _effects.text = "";
    }

    public void DisplayUnit(UnitController myUnit)
    {
        IEffect[] unitEffects;
        ISkill[] unitSkills;
        IAbility unitAbility;
        string description;
        int counter;

        _myUnit = myUnit;
        _hpText.enabled = true;
        _moveRangeText.enabled = true;
        _attackStrengthText.enabled = true;
        _skillsText.enabled = true;
        if (_effectsText != null) _effectsText.enabled = true;
        _name.text = myUnit.GetUnitName();
        if (myUnit.IsKing()) _description.text = "Superior";
        else _description.text = "Doppelganger";
        if(myUnit._isDeployed) _hp.text = myUnit.GetHP().ToString() + "/" + myUnit.GetMaxHP().ToString();
        else _hp.text = myUnit.GetMaxHP().ToString();
        _moveRange.text = myUnit.GetBaseMoveRange().ToString();
        _attackStrength.text = myUnit.GetAttackStrength().ToString();
        unitSkills = myUnit.gameObject.GetComponents<ISkill>();
        unitAbility = myUnit.gameObject.GetComponent<IAbility>();
        description = "";
        counter = 0;
        foreach(ISkill skill in unitSkills)
        {
            if (counter > 0) description += "\n\n";
            description += skill.GetDescription();
            counter++;
        }
        if(unitAbility != null)
        {
            if (counter > 0) description += "\n\n";
            description += unitAbility.GetDescription();
            counter++;
        }
        if (myUnit.IsKing())
        {
            if (counter > 0) description += "\n\n";
            description += "Caller [can call Doppelgangers]";
            counter++;
        }
        if (myUnit.GetArmor() > 0)
        {
            if (counter > 0) description += "\n\n";
            description += "TOUGH [reduce received damage by 1]";
            counter++;
        }
        if (myUnit.GetAttackRange() > 1)
        {
            if (counter > 0) description += "\n\n";
            description += "GUNMAN [range of attack extended by 2 tiles in a straight line]";
            counter++;
        }
        if (myUnit.GetBaseAttacksCount() > 1)
        {
            if (counter > 0) description += "\n\n";
            description += "BINARY [can attack twice in turn]";
            counter++;
        }
        if (!myUnit.SummoningSickness())
        {
            if (counter > 0) description += "\n\n";
            description += "SWIFT [can move in the turn it was called]";
            counter++;
        }
        _skills.text = description;
        unitEffects = myUnit.gameObject.GetComponents<IEffect>();
        description = "";
        counter = 0;
        foreach (IEffect effect in unitEffects)
        {
            if(counter > 0) description += "\n\n";
            description += effect.GetDescription();
            counter++;
        }
        if (_effectsText != null) _effects.text = description;
    }

    public void ClearDisplay()
    {
        _hpText.enabled = false;
        _moveRangeText.enabled = false;
        _attackStrengthText.enabled = false;
        _skillsText.enabled = false;
        _effectsText.enabled = false;
        _name.text = "";
        _description.text = "";
        _hp.text = "";
        _moveRange.text = "";
        _attackStrength.text = "";
        _skills.text = "";
        _effects.text = "";
    }

    public UnitController GetDisplayedUnit()
    {
        return _myUnit;
    }
}
