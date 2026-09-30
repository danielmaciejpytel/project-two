using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Short history of the last actions, grouped by turn, shown in the HUD.
public class BattleLogController : MonoBehaviour
{
    private enum EntryKind { Normal, Kill }

    private struct Entry
    {
        public int turn;
        public int turnPlayer;
        public int actorPlayer;   // 0 = board / status effect
        public string text;
        public string value;
        public EntryKind kind;
    }

    [SerializeField] private GameObject _panel;
    [SerializeField] private GameObject _body;
    [SerializeField] private TMP_Text _entriesText;
    [SerializeField] private Button _toggleButton;
    [SerializeField] private TMP_Text _toggleLabel;
    [Tooltip("On the left side the panel lines up with the left edge of this rect (the info panel).")]
    [SerializeField] private RectTransform _leftAlignReference;
    [SerializeField] private float _leftAlignInset = 6.0f;
    [SerializeField] private int _maxEntries = 6;
    [Tooltip("Height of the panel when collapsed to the title bar.")]
    [SerializeField] private float _collapsedHeight = 44.0f;
    [Tooltip("Position of the toggle button in the title bar when collapsed.")]
    [SerializeField] private Vector2 _collapsedButtonPosition = new Vector2(-10.0f, -8.0f);
    [SerializeField] private Color _hotColor = new Color32(0xE8, 0x17, 0x3F, 0xFF);
    [SerializeField] private Color _coldColor = new Color32(0x10, 0xBF, 0xD3, 0xFF);
    [SerializeField] private Color _hotNameColor = new Color32(0xFF, 0xD0, 0xDA, 0xFF);
    [SerializeField] private Color _coldNameColor = new Color32(0xC6, 0xF5, 0xFA, 0xFF);
    [SerializeField] private Color _healColor = new Color32(0xC9, 0xF2, 0xD4, 0xFF);

    private readonly List<Entry> _entries = new List<Entry>();
    private readonly StringBuilder _builder = new StringBuilder();
    private int _turn;
    private int _turnPlayer;
    private RectTransform _rect;
    private RectTransform _buttonRect;
    private float _expandedHeight;
    private Vector2 _expandedButtonPosition;
    private float _sideMargin;

    private void Awake()
    {
        // Hidden while players are still choosing units.
        _panel.SetActive(false);
        _rect = (RectTransform)transform;
        _buttonRect = (RectTransform)_toggleButton.transform;
        _expandedHeight = _rect.sizeDelta.y;
        _expandedButtonPosition = _buttonRect.anchoredPosition;
        _sideMargin = Mathf.Abs(_rect.anchoredPosition.x);
        // The label must catch clicks too, otherwise a button without a visible background can't be pressed.
        _toggleLabel.raycastTarget = true;
        _toggleButton.onClick.AddListener(ToggleBody);
        // Open by default; the player can collapse it.
        SetExpanded(true);
    }

    private void Start()
    {
        EventManager events = EventManager.Instance;
        events.OnTurnStarted += OnTurnStarted;
        events.OnUnitDeployed += OnUnitDeployed;
        events.OnUnitMoved += OnUnitMoved;
        events.OnUnitAttacked += OnUnitAttacked;
        events.OnUnitDamaged += OnUnitDamaged;
        events.OnUnitHealed += OnUnitHealed;
        events.OnEffectApplied += OnEffectApplied;
        events.OnAbilityUsed += OnAbilityUsed;
        events.OnUnitKilled += OnUnitKilled;
    }

    private void OnDestroy()
    {
        EventManager events = EventManager.Instance;
        if (events == null) return;
        events.OnTurnStarted -= OnTurnStarted;
        events.OnUnitDeployed -= OnUnitDeployed;
        events.OnUnitMoved -= OnUnitMoved;
        events.OnUnitAttacked -= OnUnitAttacked;
        events.OnUnitDamaged -= OnUnitDamaged;
        events.OnUnitHealed -= OnUnitHealed;
        events.OnEffectApplied -= OnEffectApplied;
        events.OnAbilityUsed -= OnAbilityUsed;
        events.OnUnitKilled -= OnUnitKilled;
    }

    private void ToggleBody()
    {
        if (SoundController.Instance != null) SoundController.Instance?.PlayClick();
        SetExpanded(!_body.activeSelf);
    }

    // Follows the other turn controls: left edge for Super Hot, right edge for Super Cold.
    private void MoveToPlayerSide(int playerId)
    {
        float side = playerId == 1 ? 0.0f : 1.0f;
        _rect.anchorMin = new Vector2(side, _rect.anchorMin.y);
        _rect.anchorMax = new Vector2(side, _rect.anchorMax.y);
        _rect.pivot = new Vector2(side, _rect.pivot.y);
        _rect.anchoredPosition = new Vector2(playerId == 1 ? LeftMargin() : -_sideMargin, _rect.anchoredPosition.y);
    }

    // Distance from the left edge of the parent to where the panel starts on the left side.
    private float LeftMargin()
    {
        if (_leftAlignReference == null) return _sideMargin;
        RectTransform parent = (RectTransform)_rect.parent;
        Vector3[] corners = new Vector3[4];
        _leftAlignReference.GetWorldCorners(corners);
        float scale = parent.lossyScale.x;
        return parent.InverseTransformPoint(new Vector3(corners[0].x + _leftAlignInset * scale, 0.0f, 0.0f)).x - parent.rect.xMin;
    }

    // Collapsed: only the "Battle log" title with "Show" on its right.
    private void SetExpanded(bool expanded)
    {
        _body.SetActive(expanded);
        _rect.sizeDelta = new Vector2(_rect.sizeDelta.x, expanded ? _expandedHeight : _collapsedHeight);
        _buttonRect.anchoredPosition = expanded ? _expandedButtonPosition : _collapsedButtonPosition;
        _toggleLabel.text = Loc.T(expanded ? "Hide" : "Show");
    }

    private void OnTurnStarted(int playerId)
    {
        _turn++;
        _turnPlayer = playerId;
        MoveToPlayerSide(playerId);
        _panel.SetActive(true);
        Refresh();
    }

    private void OnUnitDeployed(UnitController unit)
    {
        UnitController commander = GameController.Instance != null ? GameController.Instance.GetCommander(unit.GetPlayerId()) : null;
        Add(unit.GetPlayerId(), commander != null ? Loc.F("{0} called {1}", Name(commander), Name(unit)) : Loc.F("Called {0}", Name(unit)));
    }

    private void OnUnitMoved(UnitController unit, TileController tile)
    {
        string tileName = Loc.T(tile.GetTileName());
        Add(unit.GetPlayerId(), string.IsNullOrEmpty(tileName) ? Loc.F("{0} moved", Name(unit)) : Loc.F("{0} moved to {1}", Name(unit), tileName));
    }

    private void OnUnitAttacked(UnitController attacker, UnitController target, int attackPower, int damageTaken)
    {
        int blocked = attackPower - damageTaken;
        string text = Loc.F("{0} hit {1}", Name(attacker), Name(target));
        if (blocked > 0) text += " <alpha=#AA>" + Loc.F("({0} blocked)", blocked) + "<alpha=#FF>";
        Add(attacker.GetPlayerId(), text, Colored($"-{damageTaken}", _hotNameColor));
    }

    private void OnUnitDamaged(UnitController unit, int damageTaken, string source)
    {
        Add(0, Loc.F("{0} hurt {1}", Loc.T(source), Name(unit)), Colored($"-{damageTaken}", _hotNameColor));
    }

    private void OnUnitHealed(UnitController unit, int amount, string source)
    {
        Add(0, Loc.F("{0} healed {1}", Loc.T(source), Name(unit)), Colored($"+{amount}", _healColor));
    }

    private void OnEffectApplied(UnitController unit, string effectName, UnitController source)
    {
        string effect = Loc.T(effectName).ToLowerInvariant();
        string text = source != null ? Loc.F("{0} {1} by {2}", Name(unit), effect, Name(source)) : Loc.F("{0} {1}", Name(unit), effect);
        Add(0, text);
    }

    private void OnAbilityUsed(UnitController user, string abilityName, UnitController target)
    {
        string ability = Loc.T(abilityName, "used");
        Add(user.GetPlayerId(), target != null ? Loc.F("{0} used {1} on {2}", Name(user), ability, Name(target)) : Loc.F("{0} used {1}", Name(user), ability));
    }

    private void OnUnitKilled(UnitController unit)
    {
        Add(GameController.GetOpponent(unit.GetPlayerId()), Loc.F("{0} was killed", Name(unit)), "x", EntryKind.Kill);
    }

    private void Add(int actorPlayer, string text, string value = "", EntryKind kind = EntryKind.Normal)
    {
        _entries.Add(new Entry { turn = _turn, turnPlayer = _turnPlayer, actorPlayer = actorPlayer, text = text, value = value, kind = kind });
        while (_entries.Count > _maxEntries) _entries.RemoveAt(0);
        Refresh();
    }

    private void Refresh()
    {
        _builder.Clear();
        int shownTurn = -1;
        for (int i = 0; i < _entries.Count; i++)
        {
            Entry entry = _entries[i];
            bool isOld = entry.turn < _turn;
            if (entry.turn != shownTurn)
            {
                shownTurn = entry.turn;
                AppendTurnHeader(entry.turn, entry.turnPlayer, isOld);
            }
            AppendEntry(entry, isOld, i == _entries.Count - 1);
        }
        // Header for a turn that has no actions yet.
        if (shownTurn != _turn && _turn > 0) AppendTurnHeader(_turn, _turnPlayer, false);
        _entriesText.text = _builder.ToString();
    }

    private void AppendTurnHeader(int turn, int playerId, bool isOld)
    {
        if (_builder.Length > 0) _builder.Append('\n');
        _builder.Append(isOld ? "<alpha=#8C>" : "<alpha=#FF>");
        _builder.Append("<size=85%>").Append(Loc.F("Turn {0} - {1}", turn, PlayerName(playerId))).Append("</size>\n");
    }

    private void AppendEntry(Entry entry, bool isOld, bool isNewest)
    {
        _builder.Append(isOld ? "<alpha=#8C>" : "<alpha=#FF>");
        if (entry.kind == EntryKind.Kill) _builder.Append("<mark=#E8173F55>");
        else if (isNewest) _builder.Append("<mark=#FFFFFF22>");

        Color dot = entry.actorPlayer == 1 ? _hotColor : entry.actorPlayer == 2 ? _coldColor : new Color(1f, 1f, 1f, 0.6f);
        _builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGBA(dot)).Append(">|</color> ");
        _builder.Append(entry.text);
        if (!string.IsNullOrEmpty(entry.value)) _builder.Append("<pos=88%>").Append(entry.value);
        if (entry.kind == EntryKind.Kill || isNewest) _builder.Append("</mark>");
        _builder.Append('\n');
    }

    private string Name(UnitController unit)
    {
        Color color = unit.GetPlayerId() == 1 ? _hotNameColor : _coldNameColor;
        return Colored(unit.GetUnitName(), color);
    }

    private static string Colored(string text, Color color) => $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";

    private static string PlayerName(int playerId) => playerId == 1 ? "Super Hot" : "Super Cold";
}
