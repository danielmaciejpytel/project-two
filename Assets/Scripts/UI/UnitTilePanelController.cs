using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// The details panel in the bottom left corner: a unit (name, kind, move, strength and range, and its tags with icons; nothing about the
// tile it stands on) or a tile (name, kind and what it does). Empty, it only says where the details come from.
public class UnitTilePanelController : MonoBehaviour
{
    [Serializable]
    private struct TagIcon
    {
        public string key;
        public Sprite sprite;
    }

    [SerializeField] TMP_Text _name;
    [Tooltip("The kind under the name: Superior, Doppelganger or Tile.")]
    [SerializeField] TMP_Text _description;
    [Tooltip("Small square in the team color before the kind.")]
    [SerializeField] Image _kindSquare;
    [SerializeField] TMP_Text _moveRangeText;
    [SerializeField] TMP_Text _moveRange;
    [SerializeField] TMP_Text _attackStrengthText;
    [SerializeField] TMP_Text _attackStrength;
    [SerializeField] TMP_Text _attackRangeText;
    [SerializeField] TMP_Text _attackRange;
    [Header("Tags")]
    [Tooltip("Where the rows of the tags go (below the statistics).")]
    [SerializeField] RectTransform _tagList;
    [Tooltip("A row to copy for every tag: IconFrame, Icon, Name and Description.")]
    [SerializeField] GameObject _tagRowTemplate;
    [SerializeField] TagIcon[] _tagIcons;
    [SerializeField] float _rowHeight = 58.0f;
    [SerializeField] float _columnGap = 24.0f;
    [SerializeField] float _textOffset = 48.0f;
    [Header("Empty")]
    [Tooltip("\"Details\" and the hint, shown while nothing is chosen.")]
    [SerializeField] TMP_Text _emptyTitle;
    [SerializeField] TMP_Text _emptyHint;
    [SerializeField] Color _hotColor = new Color32(0xFF, 0x1B, 0x47, 0xFF);
    [SerializeField] Color _coldColor = new Color32(0x14, 0xC8, 0xD8, 0xFF);

    // Short explanations of what a tile does, under its effect (the key is the effect as the tile gives it).
    // None is longer than the first one (the sample).
    private static readonly Dictionary<string, string> TileExplanations = new Dictionary<string, string>
    {
        { "-1 Move Range", "A unit that enters this tile has a smaller move range this turn." },
        { "Reduce received damage by 1", "A unit that ends its turn here takes 1 less damage." },
        { "+1 Attack Strength", "A unit that enters this tile deals 1 more damage." },
        { "+1 Attack Range", "A unit that enters this tile can attack 1 tile further." },
        { "-1 HP/Turn", "A unit that ends its turn here loses 1 HP." },
        { "+1 HP/Turn", "A unit that ends its turn here heals 1 HP." },
        { "Impassable", "No unit can enter this tile." },
        { "No effect", "Units can walk and stand here freely." },
    };

    // The tile without a name or an effect in the game data, the ordinary ground.
    private const string PlainTileName = "Open Ground";
    private const string PlainTileEffect = "No effect";

    private UnitController _myUnit;
    private bool _hasContent;
    private readonly List<GameObject> _rows = new List<GameObject>();

    // Start is called before the first frame update
    void Awake()
    {
        // Long names (e.g. "Restriction Area") must stay on one line, otherwise they overlap the row below.
        // Its TextMeshPro auto-size shrinks it when it doesn't fit.
        _name.textWrappingMode = TextWrappingModes.NoWrap;
        if (_tagRowTemplate != null) _tagRowTemplate.SetActive(false);
        // Start empty instead of showing the placeholder text left in the scene
        // (unless something was already displayed while the panel was inactive).
        if (!_hasContent) ClearDisplay();
    }

    public void DisplayTile(TileController myTile)
    {
        _hasContent = true;
        _myUnit = null;
        SetEmpty(false);
        SetStatsVisible(false);
        string tileName = myTile.GetTileName();
        string effect = myTile.GetDescription();
        if (string.IsNullOrEmpty(tileName))
        {
            tileName = PlainTileName;
            effect = PlainTileEffect;
        }
        _name.text = Loc.T(tileName);
        _description.text = Loc.T("Tile");
        _kindSquare.gameObject.SetActive(false);
        List<Entry> entries = new List<Entry>();
        if (!string.IsNullOrEmpty(effect))
        {
            string explanation = TileExplanations.TryGetValue(effect, out string text) ? Loc.T(text) : "";
            entries.Add(new Entry { name = Loc.T(effect), description = explanation, key = null });
        }
        ShowEntries(entries);
    }

    public void DisplayUnit(UnitController myUnit)
    {
        _hasContent = true;
        _myUnit = myUnit;
        SetEmpty(false);
        SetStatsVisible(true);
        _name.text = myUnit.GetUnitName();
        _description.text = Loc.T(myUnit.IsKing() ? "Superior" : "Doppelganger");
        _kindSquare.gameObject.SetActive(true);
        _kindSquare.color = myUnit.GetPlayerId() == 1 ? _hotColor : _coldColor;
        _moveRange.text = myUnit.GetBaseMoveRange().ToString();
        _attackStrength.text = myUnit.GetAttackStrength().ToString();
        int range = myUnit.GetAttackRange();
        _attackRange.text = range > 1 ? "1-" + range : "1";

        List<Entry> entries = new List<Entry>();
        foreach (string tag in CollectTags(myUnit)) entries.Add(ParseEntry(tag, true));
        // Only what the unit carries itself (burn, poison...); what its tile does is shown when the tile is hovered.
        foreach (IEffect effect in myUnit.gameObject.GetComponents<IEffect>())
        {
            if (effect is ITileEffect) continue;
            entries.Add(ParseEntry(effect.GetDescription(), false));
        }
        ShowEntries(entries);
    }

    // The tags of a unit as in the unit draft: skills, ability and what its statistics say.
    private static List<string> CollectTags(UnitController unit)
    {
        List<string> tags = new List<string>();
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

    private struct Entry
    {
        public string key;          // the English name, which picks the icon; null: no icon
        public string name;
        public string description;
    }

    // "NAME [what it does]" into the name and what it does; the icon comes from the English name.
    private static Entry ParseEntry(string english, bool withIcon)
    {
        string translated = Loc.T(english);
        Entry entry = new Entry { key = withIcon ? TagKey(english) : null };
        int open = translated.IndexOf('[');
        if (open < 0) open = translated.IndexOf('(');
        if (open < 0)
        {
            entry.name = translated.Trim();
            entry.description = "";
            return entry;
        }
        entry.name = translated.Substring(0, open).Trim();
        entry.description = translated.Substring(open).Trim().TrimStart('[', '(').TrimEnd(']', ')').Trim();
        return entry;
    }

    private static string TagKey(string tag)
    {
        int end = tag.IndexOfAny(new[] { '[', '(' });
        return (end < 0 ? tag : tag.Substring(0, end)).Trim();
    }

    private Sprite IconFor(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        foreach (TagIcon icon in _tagIcons)
        {
            if (string.Equals(icon.key, key, StringComparison.OrdinalIgnoreCase)) return icon.sprite;
        }
        return null;
    }

    // One row for every entry: one column for up to two, two columns for up to four, else three.
    private void ShowEntries(List<Entry> entries)
    {
        if (_tagList == null || _tagRowTemplate == null) return;
        int columns = entries.Count <= 2 ? 1 : entries.Count <= 4 ? 2 : 3;
        float width = _tagList.rect.width;
        float columnWidth = (width - _columnGap * (columns - 1)) / columns;
        while (_rows.Count < entries.Count)
        {
            GameObject row = Instantiate(_tagRowTemplate, _tagList, false);
            _rows.Add(row);
        }
        for (int i = 0; i < _rows.Count; i++)
        {
            GameObject row = _rows[i];
            row.SetActive(i < entries.Count);
            if (i >= entries.Count) continue;
            Entry entry = entries[i];
            float rowX = (i % columns) * (columnWidth + _columnGap);
            float rowY = (i / columns) * _rowHeight;
            HudLayout.PlaceIn((RectTransform)row.transform, rowX, rowY, columnWidth, _rowHeight);
            Sprite icon = IconFor(entry.key);
            Transform frame = row.transform.Find("IconFrame");
            frame.gameObject.SetActive(icon != null);
            Image iconImage = row.transform.Find("IconFrame/Icon").GetComponent<Image>();
            iconImage.sprite = icon;
            TMP_Text nameText = row.transform.Find("Name").GetComponent<TMP_Text>();
            TMP_Text description = row.transform.Find("Description").GetComponent<TMP_Text>();
            float left = icon != null ? _textOffset : 0.0f;
            // The text stays left of the slanted edge of the panel, as far as it reaches at the bottom of the first line of the description.
            float listX = _tagList.anchoredPosition.x;
            float listY = -_tagList.anchoredPosition.y;
            float edge = HudLayout.InfoPanelSlantX(listY + rowY + 22.0f + 24.0f) - 24.0f;
            float textWidth = Mathf.Clamp(edge - listX - rowX - left, 120.0f, columnWidth - left);
            HudLayout.PlaceIn((RectTransform)nameText.transform, left, 2.0f, textWidth, 16.0f);
            // The last row may use the rest of the list, so a longer description (of a tile) can take a second line.
            int rowIndex = i / columns;
            bool lastRow = rowIndex == (entries.Count - 1) / columns;
            float descriptionHeight = lastRow ? Mathf.Max(_rowHeight - 22.0f, _tagList.rect.height - rowIndex * _rowHeight - 22.0f) : _rowHeight - 22.0f;
            HudLayout.PlaceIn((RectTransform)description.transform, left, 22.0f, textWidth, descriptionHeight);
            nameText.text = entry.name;
            description.text = entry.description;
        }
    }

    private void SetStatsVisible(bool unit)
    {
        foreach (TMP_Text text in new[] { _moveRangeText, _moveRange, _attackStrengthText, _attackStrength, _attackRangeText, _attackRange })
        {
            text.gameObject.SetActive(unit);
        }
        _kindSquare.gameObject.SetActive(true);
    }

    // The empty panel shows its title and hint; with something displayed they are hidden.
    private void SetEmpty(bool empty)
    {
        if (_emptyTitle != null) _emptyTitle.gameObject.SetActive(empty);
        if (_emptyHint != null) _emptyHint.gameObject.SetActive(empty);
        _name.gameObject.SetActive(!empty);
        _description.gameObject.SetActive(!empty);
        _kindSquare.gameObject.SetActive(!empty);
    }

    public void ClearDisplay()
    {
        SetEmpty(true);
        SetStatsVisible(false);
        _kindSquare.gameObject.SetActive(false);
        _name.text = "";
        _description.text = "";
        _moveRange.text = "";
        _attackStrength.text = "";
        _attackRange.text = "";
        foreach (GameObject row in _rows) row.SetActive(false);
    }

    public UnitController GetDisplayedUnit()
    {
        return _myUnit;
    }
}
