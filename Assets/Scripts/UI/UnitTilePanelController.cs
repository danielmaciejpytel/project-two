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

    // Space between the tags text and the "Effects" heading, and to the panel's bottom edge.
    private const float SectionGap = 10.0f;
    private const float BottomMargin = 6.0f;

    private UnitController _myUnit;
    private bool _hasContent;

    // Start is called before the first frame update
    void Awake()
    {
        // Long names (e.g. "Restriction Area") must stay on one line, otherwise they overlap the description below.
        // Its TextMeshPro auto-size shrinks it when it doesn't fit.
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
        _name.text = Loc.T(myTile.GetTileName());
        _description.text = Loc.T(myTile.GetDescription());
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
        if (myUnit.IsKing()) _description.text = Loc.T("Superior");
        else _description.text = Loc.T("Doppelganger");
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
            description += FitBracket(Loc.T(skill.GetDescription()), _skills);
            counter++;
        }
        if(unitAbility != null)
        {
            if (counter > 0) description += "\n\n";
            description += FitBracket(Loc.T(unitAbility.GetDescription()), _skills);
            counter++;
        }
        if (myUnit.IsKing())
        {
            if (counter > 0) description += "\n\n";
            description += FitBracket(Loc.T("Caller [can call Doppelgangers]"), _skills);
            counter++;
        }
        if (myUnit.GetArmor() > 0)
        {
            if (counter > 0) description += "\n\n";
            description += FitBracket(Loc.T("TOUGH [reduce received damage by 1]"), _skills);
            counter++;
        }
        if (myUnit.GetAttackRange() > 1)
        {
            if (counter > 0) description += "\n\n";
            description += FitBracket(Loc.T("GUNMAN [range of attack extended by 2 tiles in a straight line]"), _skills);
            counter++;
        }
        if (myUnit.GetBaseAttacksCount() > 1)
        {
            if (counter > 0) description += "\n\n";
            description += FitBracket(Loc.T("BINARY [can attack twice in turn]"), _skills);
            counter++;
        }
        if (!myUnit.SummoningSickness())
        {
            if (counter > 0) description += "\n\n";
            description += FitBracket(Loc.T("SWIFT [can move in the turn it was called]"), _skills);
            counter++;
        }
        _skills.text = description;
        unitEffects = myUnit.gameObject.GetComponents<IEffect>();
        description = "";
        counter = 0;
        foreach (IEffect effect in unitEffects)
        {
            if(counter > 0) description += "\n\n";
            description += FitBracket(Loc.T(effect.GetDescription()), _effects);
            counter++;
        }
        if (_effectsText != null) _effects.text = description;
        LayoutEffects();
    }

    // An entry like "NAME [what it does]" that doesn't fit on one line gets the bracket on a line of
    // its own, instead of being split in the middle of the bracket.
    private static string FitBracket(string text, TMP_Text target)
    {
        int bracket = text.IndexOfAny(new[] { '[', '(' });
        if (bracket <= 0) return text;
        float lineWidth = target.rectTransform.rect.width;
        if (target.GetPreferredValues(text, 100000.0f, 100000.0f).x <= lineWidth) return text;
        return text.Substring(0, bracket).TrimEnd() + "\n" + text.Substring(bracket);
    }

    // The "Effects" heading and its text follow the tags text, however long it is. When both together
    // don't fit in the panel, the blank lines between entries are dropped, and if that isn't enough the
    // effects move up so nothing reaches below the panel's background.
    private void LayoutEffects()
    {
        if (_effectsText == null) return;
        RectTransform skills = _skills.rectTransform;
        RectTransform heading = _effectsText.rectTransform;
        RectTransform effects = _effects.rectTransform;
        RectTransform panel = (RectTransform)transform;

        float skillsTop = skills.anchoredPosition.y + (1.0f - skills.pivot.y) * skills.rect.height;
        float lowest = -panel.rect.height * panel.pivot.y + BottomMargin;
        float headingTop, effectsTop, effectsBottom;
        for (int attempt = 0; ; attempt++)
        {
            _skills.ForceMeshUpdate();
            _effects.ForceMeshUpdate();
            headingTop = skillsTop - _skills.preferredHeight - SectionGap;
            effectsTop = headingTop - heading.rect.height;
            effectsBottom = effectsTop - _effects.preferredHeight;
            if (effectsBottom >= lowest || attempt > 0) break;
            _skills.text = _skills.text.Replace("\n\n", "\n");
            _effects.text = _effects.text.Replace("\n\n", "\n");
        }
        if (effectsBottom < lowest)
        {
            float shift = lowest - effectsBottom;
            headingTop += shift;
            effectsTop += shift;
        }
        heading.anchoredPosition = new Vector2(heading.anchoredPosition.x, headingTop - (1.0f - heading.pivot.y) * heading.rect.height);
        effects.anchoredPosition = new Vector2(effects.anchoredPosition.x, effectsTop - (1.0f - effects.pivot.y) * effects.rect.height);
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
