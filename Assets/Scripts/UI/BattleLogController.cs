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
    [SerializeField] private int _maxEntries = 6;
    [SerializeField] private Color _hotColor = new Color32(0xE8, 0x17, 0x3F, 0xFF);
    [SerializeField] private Color _coldColor = new Color32(0x10, 0xBF, 0xD3, 0xFF);
    [SerializeField] private Color _hotNameColor = new Color32(0xFF, 0xD0, 0xDA, 0xFF);
    [SerializeField] private Color _coldNameColor = new Color32(0xC6, 0xF5, 0xFA, 0xFF);
    [SerializeField] private Color _healColor = new Color32(0xC9, 0xF2, 0xD4, 0xFF);

    private readonly List<Entry> _entries = new List<Entry>();
    private readonly StringBuilder _builder = new StringBuilder();
    private int _turn;
    private int _turnPlayer;

    private void Awake()
    {
        // Hidden while players are still choosing units.
        _panel.SetActive(false);
        _toggleButton.onClick.AddListener(ToggleBody);
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
        if (SoundController.Instance != null) SoundController.Instance.PlayClick();
        _body.SetActive(!_body.activeSelf);
        _toggleLabel.text = _body.activeSelf ? "Hide" : "Show";
    }

    private void OnTurnStarted(int playerId)
    {
        _turn++;
        _turnPlayer = playerId;
        _panel.SetActive(true);
        Refresh();
    }

    private void OnUnitDeployed(UnitController unit)
    {
        UnitController commander = GameController.Instance != null ? GameController.Instance.GetCommander(unit.GetPlayerId()) : null;
        string caller = commander != null ? Name(commander) + " called " : "Called ";
        Add(unit.GetPlayerId(), caller + Name(unit));
    }

    private void OnUnitMoved(UnitController unit, TileController tile)
    {
        string tileName = tile.GetTileName();
        Add(unit.GetPlayerId(), string.IsNullOrEmpty(tileName) ? Name(unit) + " moved" : $"{Name(unit)} moved to {tileName}");
    }

    private void OnUnitAttacked(UnitController attacker, UnitController target, int attackPower, int damageTaken)
    {
        int blocked = attackPower - damageTaken;
        string text = $"{Name(attacker)} hit {Name(target)}";
        if (blocked > 0) text += $" <alpha=#AA>({blocked} blocked)<alpha=#FF>";
        Add(attacker.GetPlayerId(), text, Colored($"-{damageTaken}", _hotNameColor));
    }

    private void OnUnitDamaged(UnitController unit, int damageTaken, string source)
    {
        Add(0, $"{source} hurt {Name(unit)}", Colored($"-{damageTaken}", _hotNameColor));
    }

    private void OnUnitHealed(UnitController unit, int amount, string source)
    {
        Add(0, $"{source} healed {Name(unit)}", Colored($"+{amount}", _healColor));
    }

    private void OnEffectApplied(UnitController unit, string effectName, UnitController source)
    {
        string text = source != null ? $"{Name(unit)} {effectName.ToLowerInvariant()} by {Name(source)}" : $"{Name(unit)} {effectName.ToLowerInvariant()}";
        Add(0, text);
    }

    private void OnAbilityUsed(UnitController user, string abilityName, UnitController target)
    {
        Add(user.GetPlayerId(), target != null ? $"{Name(user)} used {abilityName} on {Name(target)}" : $"{Name(user)} used {abilityName}");
    }

    private void OnUnitKilled(UnitController unit)
    {
        Add(GameController.GetOpponent(unit.GetPlayerId()), $"{Name(unit)} was killed", "x", EntryKind.Kill);
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
        _builder.Append("<size=85%>Turn ").Append(turn).Append(" - ").Append(PlayerName(playerId)).Append("</size>\n");
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
