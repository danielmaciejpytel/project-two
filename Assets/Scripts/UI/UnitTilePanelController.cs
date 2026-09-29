using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitTilePanelController : MonoBehaviour
{
    [SerializeField] TMP_Text _name;
    [SerializeField] TMP_Text _description;
    [SerializeField] TMP_Text _hpText;
    [SerializeField] TMP_Text _hp;
    [SerializeField] TMP_Text _moveRangeText;
    [SerializeField] TMP_Text _moveRange;
    [SerializeField] TMP_Text _attackStrengthText;
    [SerializeField] TMP_Text _attackStrength;
    [SerializeField] TMP_Text _skillsText;
    [SerializeField] TMP_Text _skills;
    [SerializeField] TMP_Text _effectsText;
    [SerializeField] TMP_Text _effects;

    private UnitController _myUnit;
    private bool _hasContent;

    // Start is called before the first frame update
    void Awake()
    {
        // Long names (e.g. "Restriction Area") must stay on one line, otherwise they overlap the description below.
        // PixelText on the name shrinks it by whole pixels when it doesn't fit.
        _name.textWrappingMode = TextWrappingModes.NoWrap;
        _hpText.enabled = false;
        _moveRangeText.enabled = false;
        _attackStrengthText.enabled = false;
        _skillsText.enabled = false;
        if(_effectsText != null) _effectsText.enabled = false;
        // Start empty instead of showing the placeholder text left in the scene
        // (unless something was already displayed while the panel was inactive).
        if (!_hasContent) ClearDisplay();
    }

    public void DisplayTile(TileController myTile)
    {
        _hasContent = true;
        _name.text = myTile.GetTileName();
        _description.text = myTile.GetDescription();
        _hpText.enabled = false;
        _moveRangeText.enabled = false;
        _attackStrengthText.enabled = false;
        _skillsText.enabled = false;
        if (_effectsText != null) _effectsText.enabled = false;
        _hp.text = "";
        _moveRange.text = "";
        _attackStrength.text = "";
        _skills.text = "";
        if (_effects != null) _effects.text = "";
    }

    public void DisplayUnit(UnitController myUnit)
    {
        IEffect[] unitEffects;
        ISkill[] unitSkills;
        IAbility unitAbility;
        string description;
        int counter;

        _hasContent = true;
        _myUnit = myUnit;
        _hpText.enabled = true;
        _moveRangeText.enabled = true;
        _attackStrengthText.enabled = true;
        _skillsText.enabled = true;
        if (_effectsText != null) _effectsText.enabled = true;
        _name.text = myUnit.GetUnitName();
        if (myUnit.IsKing()) _description.text = "Superior";
        else _description.text = "Doppelganger";
        if(myUnit.IsDeployed) _hp.text = myUnit.GetHP().ToString() + "/" + myUnit.GetMaxHP().ToString();
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
        if (_effectsText != null) _effectsText.enabled = false;
        _name.text = "";
        _description.text = "";
        _hp.text = "";
        _moveRange.text = "";
        _attackStrength.text = "";
        _skills.text = "";
        if (_effects != null) _effects.text = "";
    }

    public UnitController GetDisplayedUnit()
    {
        return _myUnit;
    }
}
