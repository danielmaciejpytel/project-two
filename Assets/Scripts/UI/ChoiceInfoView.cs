using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Information about a unit under the places of a team: name, statistics, tag icons, move and attack range,
// Health Points and the tags with their descriptions.
public class ChoiceInfoView : MonoBehaviour
{
    private const int GridRadius = 4;
    private const float RowPitch = 27.6f;
    private const float TagGap = 4.0f;
    private const string TagNameColor = "<color=#D72E66>";

    private static readonly Color BaseCell = new Color(0.259f, 0.259f, 0.259f);
    private static readonly Color PanelGrey = new Color(0.227f, 0.227f, 0.227f);
    private static readonly Color Ink = new Color(0.957f, 0.957f, 0.957f);
    private static readonly Color InkSoft = new Color(0.898f, 0.898f, 0.898f);

    [Serializable]
    private struct TagIcon
    {
        public string key;
        public Sprite sprite;
    }

    [SerializeField] private TMP_Text _name;
    [SerializeField] private TMP_Text _kind;
    [SerializeField] private TMP_Text[] _statLabels;
    [SerializeField] private TMP_Text[] _statValues;
    [SerializeField] private Image[] _icons;
    [SerializeField] private Image[] _gridCells;
    [SerializeField] private Image[] _gridInner;
    [SerializeField] private Image _moveSwatch;
    [SerializeField] private Image _attackSwatch;
    [SerializeField] private Image _attackSwatchInner;
    [SerializeField] private TMP_Text _moveLabel;
    [SerializeField] private TMP_Text _moveValue;
    [SerializeField] private TMP_Text _attackLabel;
    [SerializeField] private TMP_Text _attackValue;
    [SerializeField] private Image[] _pips;
    [SerializeField] private TMP_Text _tagsHeader;
    [SerializeField] private TMP_Text[] _tags;
    [SerializeField] private TagIcon[] _tagIcons;

    private bool _ready;

    // Texts for a unit as in the unit panels of the battle: skills, ability, and the tags worked out from its statistics.
    private static List<string> CollectTags(UnitController unit)
    {
        var tags = new List<string>();
        foreach (ISkill skill in unit.gameObject.GetComponents<ISkill>()) tags.Add(skill.GetDescription());
        IAbility ability = unit.gameObject.GetComponent<IAbility>();
        if (ability != null) tags.Add(ability.GetDescription());
        if (unit.IsKing()) tags.Add("Caller [can call Doppelgangers]");
        if (unit.GetArmor() > 0) tags.Add("TOUGH [reduce received damage by 1]");
        if (unit.GetAttackRange() > 1) tags.Add("GUNMAN [range of attack extended by 2 tiles in a straight line]");
        if (unit.GetBaseAttacksCount() > 1) tags.Add("BINARY [can attack twice in turn]");
        if (!unit.SummoningSickness()) tags.Add("SWIFT [can move in the turn it was called]");
        return tags;
    }

    private static string TagKey(string tag)
    {
        int end = tag.IndexOfAny(new[] { '[', '(' });
        return (end < 0 ? tag : tag.Substring(0, end)).Trim();
    }

    private Sprite IconFor(string key)
    {
        foreach (TagIcon icon in _tagIcons)
        {
            if (icon.key == key) return icon.sprite;
        }
        return null;
    }

    private void Prepare()
    {
        if (_ready) return;
        _ready = true;
        // Every tag line sits level with a row of the statistics.
        foreach (TMP_Text tag in _tags)
        {
            tag.ForceMeshUpdate();
            tag.lineSpacing = (RowPitch - NaturalLineHeight(tag)) / (0.01f * tag.fontSize);
        }
    }

    private static float NaturalLineHeight(TMP_Text text) => text.font.faceInfo.lineHeight * text.fontSize / text.font.faceInfo.pointSize;

    public void Show(UnitController unit, Color team)
    {
        Prepare();
        bool superior = unit.IsKing();
        _name.text = unit.GetUnitName();
        _name.color = superior ? team : Ink;
        _kind.text = Loc.T(superior ? "Superior" : "Doppelganger");
        _kind.color = superior ? team : InkSoft;
        _statLabels[0].text = Loc.T("Health Points:");
        _statLabels[1].text = Loc.T("Move Range:");
        _statLabels[2].text = Loc.T("Attack Strength:");
        _statValues[0].text = unit.GetMaxHP().ToString();
        _statValues[1].text = unit.GetBaseMoveRange().ToString();
        _statValues[2].text = unit.GetAttackStrength().ToString();

        List<string> tags = CollectTags(unit);
        ShowIcons(tags, team);
        ShowRange(unit, team);
        for (int i = 0; i < _pips.Length; i++)
        {
            _pips[i].gameObject.SetActive(i < unit.GetMaxHP());
            _pips[i].color = team;
        }
        _tagsHeader.text = Loc.T("Tags:");
        ShowTags(tags);
    }

    private void ShowIcons(List<string> tags, Color team)
    {
        for (int i = 0; i < _icons.Length; i++)
        {
            Sprite sprite = i < tags.Count ? IconFor(TagKey(tags[i])) : null;
            _icons[i].transform.parent.gameObject.SetActive(sprite != null);
            if (sprite == null) continue;
            string key = TagKey(tags[i]);
            _icons[i].sprite = sprite;
            _icons[i].color = key == "Caller" || key == "Teleporter" || key == "Confuser" ? team : Ink;
        }
    }

    private void ShowRange(UnitController unit, Color team)
    {
        int move = unit.GetBaseMoveRange();
        int range = unit.GetAttackRange();
        bool gunman = range > 1;
        Color moveColor = Color.Lerp(PanelGrey, team, 0.55f);
        int side = GridRadius * 2 + 1;
        for (int i = 0; i < _gridCells.Length; i++)
        {
            int x = i % side - GridRadius;
            int y = i / side - GridRadius;
            int distance = Mathf.Abs(x) + Mathf.Abs(y);
            bool center = x == 0 && y == 0;
            bool moves = !center && distance <= move;
            bool attacks = !center && (gunman ? (x == 0 || y == 0) && Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) <= range : distance == 1);
            Color body = moves ? moveColor : BaseCell;
            _gridCells[i].color = center || attacks ? Ink : body;
            _gridInner[i].gameObject.SetActive(attacks);
            _gridInner[i].color = body;
        }
        _moveSwatch.color = moveColor;
        _attackSwatch.color = Ink;
        _attackSwatchInner.color = PanelGrey;
        _moveLabel.text = Loc.T("Move");
        _moveValue.text = move.ToString();
        _attackLabel.text = Loc.T("Attack");
        _attackValue.text = gunman ? "1-" + range : "1";
    }

    // The first tag starts level with Health Points, the second level with Move, or with Attack when the
    // first is too long for that; any further tag follows the one before.
    private void ShowTags(List<string> tags)
    {
        float top = RowTop(_statLabels[0]);
        float previousEnd = float.NegativeInfinity;
        for (int i = 0; i < _tags.Length; i++)
        {
            TMP_Text text = _tags[i];
            text.gameObject.SetActive(i < tags.Count);
            if (i >= tags.Count) continue;
            string translated = Loc.T(tags[i]);
            int bracket = translated.IndexOfAny(new[] { '[', '(' });
            text.text = bracket < 0 ? translated : TagNameColor + translated.Substring(0, bracket).Trim() + "</color>\n" + translated.Substring(bracket);
            text.ForceMeshUpdate();
            float start;
            if (i == 0) start = top;
            else
            {
                float move = RowTop(_moveLabel), attack = RowTop(_attackLabel);
                start = previousEnd + TagGap <= move ? move : previousEnd + TagGap <= attack ? attack : previousEnd + RowPitch * 0.5f;
            }
            SetTop(text, start + (RowPitch - NaturalLineHeight(text)) * 0.5f);
            previousEnd = start + text.textInfo.lineCount * RowPitch;
        }
    }

    private static float RowTop(TMP_Text row) => -row.rectTransform.anchoredPosition.y;

    private static void SetTop(TMP_Text text, float top)
    {
        RectTransform rect = text.rectTransform;
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -top);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, text.textInfo.lineCount * RowPitch);
    }
}
