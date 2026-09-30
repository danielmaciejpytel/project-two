using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The RECORDS panel of the main menu: the leaderboard, the latest games with their details and the statistics.
public class RecordsController : MonoBehaviour
{
    public enum Tab { Leaderboard = 0, History = 1, Stats = 2 }

    private const int Rows = 10;
    private static readonly string[] Difficulties = { "", "Easy", "Normal", "Hard" };

    [SerializeField] private Button[] _tabButtons;
    [SerializeField] private TMP_Text[] _tabLabels;
    [Tooltip("Space between the tabs, which follow each other by the width of their texts.")]
    [SerializeField] private float _tabGap = 40.0f;
    [SerializeField] private Button _filterButton;
    [SerializeField] private TMP_Text _filterLabel;
    [SerializeField] private Button _detailBackButton;
    [SerializeField] private Button _clearButton;
    [SerializeField] private TMP_Text _clearLabel;
    [Tooltip("The area under the tabs; its text can be scrolled with the mouse wheel when it is longer than the area.")]
    [SerializeField] private RectTransform _body;
    [SerializeField] private TMP_Text _contentText;
    [Tooltip("Shown while the text is longer than the area.")]
    [SerializeField] private TMP_Text _scrollHint;
    [SerializeField] private Button[] _historyRows;
    [SerializeField] private TMP_Text[] _historyLabels;
    [SerializeField] private int _wheelLines = 3;
    [SerializeField] private Color _selectedColor = new Color32(0xD7, 0x2E, 0x66, 0xFF);
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _superHotColor = new Color32(0xFF, 0x1B, 0x47, 0xFF);
    [SerializeField] private Color _superColdColor = new Color32(0x14, 0xC8, 0xD8, 0xFF);

    private Tab _tab;
    private int _filter;
    private List<GameRecord> _history = new List<GameRecord>();
    private GameRecord _detail;
    private bool _clearArmed;
    private float _scroll;
    private RectTransform _contentRect;

    public Tab CurrentTab => _tab;

    // What the area shows (leaderboard, statistics or the details of a game), for the tests.
    public string ContentText => _contentText.text;

    public int VisibleHistoryRows
    {
        get
        {
            int visible = 0;
            foreach (Button row in _historyRows) visible += row.gameObject.activeSelf ? 1 : 0;
            return visible;
        }
    }

    public bool ShowsDetails => _detail != null;

    private void Awake()
    {
        _contentRect = _contentText.rectTransform;
        for (int i = 0; i < _tabButtons.Length; i++)
        {
            Tab tab = (Tab)i;
            _tabButtons[i].onClick.AddListener(() => ShowTab(tab));
        }
        for (int i = 0; i < _historyRows.Length; i++)
        {
            int index = i;
            _historyRows[i].onClick.AddListener(() => ShowDetails(index));
        }
        _filterButton.onClick.AddListener(CycleFilter);
        _detailBackButton.onClick.AddListener(() => ShowTab(Tab.History));
        _clearButton.onClick.AddListener(ClearResults);
    }

    private void OnEnable()
    {
        Loc.Changed += Refresh;
        _filter = 0;
        ShowTab(Tab.Leaderboard);
    }

    private void OnDisable() => Loc.Changed -= Refresh;

    public void ShowTab(Tab tab)
    {
        SoundController.Instance?.PlayClick();
        _tab = tab;
        _detail = null;
        _clearArmed = false;
        Refresh();
    }

    public void ShowDetails(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _history.Count) return;
        SoundController.Instance?.PlayClick();
        _detail = _history[rowIndex];
        Refresh();
    }

    private void CycleFilter()
    {
        SoundController.Instance?.PlayClick();
        _filter = (_filter + 1) % Difficulties.Length;
        Refresh();
    }

    // Removing everything takes two clicks.
    private void ClearResults()
    {
        SoundController.Instance?.PlayClick();
        if (!_clearArmed)
        {
            _clearArmed = true;
            Refresh();
            return;
        }
        _clearArmed = false;
        RecordStore.Clear();
        _detail = null;
        Refresh();
    }

    private void Refresh()
    {
        for (int i = 0; i < _tabButtons.Length; i++) _tabLabels[i].color = i == (int)_tab ? _selectedColor : _normalColor;
        LayoutTabs();
        _filterButton.gameObject.SetActive(_tab == Tab.Leaderboard);
        _filterLabel.text = Loc.F("Difficulty: {0}", _filter == 0 ? Loc.T("All") : Loc.T(Difficulties[_filter]));
        _detailBackButton.gameObject.SetActive(_detail != null);
        _clearLabel.text = Loc.T(_clearArmed ? "Click again to remove all results" : "Clear results");

        bool list = _tab == Tab.History && _detail == null;
        if (list) _history = RecordStore.Latest(Rows);
        for (int i = 0; i < _historyRows.Length; i++)
        {
            bool used = list && i < _history.Count;
            _historyRows[i].gameObject.SetActive(used);
            if (used) _historyLabels[i].text = HistoryLine(_history[i]);
        }

        string text = "";
        if (_tab == Tab.Leaderboard) text = LeaderboardText();
        else if (_tab == Tab.Stats) text = StatsText();
        else if (_detail != null) text = DetailsText(_detail);
        else if (_history.Count == 0) text = Loc.T("No games yet.");
        _contentText.text = text;
        _contentText.ForceMeshUpdate();
        _contentRect.sizeDelta = new Vector2(_contentRect.sizeDelta.x, Mathf.Max(_body.rect.height, _contentText.preferredHeight));
        _scroll = 0.0f;
        _contentRect.anchoredPosition = Vector2.zero;
        _scrollHint.gameObject.SetActive(_contentText.preferredHeight > _body.rect.height + 1.0f);
        _scrollHint.text = Loc.T("Scroll with the mouse wheel");
    }

    // The tabs follow each other from the left, whatever the language makes of their widths.
    private void LayoutTabs()
    {
        float x = ((RectTransform)_tabButtons[0].transform).anchoredPosition.x - (((RectTransform)_tabButtons[0].transform).pivot.x * ((RectTransform)_tabButtons[0].transform).rect.width);
        for (int i = 0; i < _tabButtons.Length; i++)
        {
            RectTransform rect = (RectTransform)_tabButtons[i].transform;
            _tabLabels[i].ForceMeshUpdate();
            float width = _tabLabels[i].preferredWidth + 6.0f;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rect.anchoredPosition = new Vector2(x + rect.pivot.x * width, rect.anchoredPosition.y);
            x += width + _tabGap;
        }
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || _contentRect.rect.height <= _body.rect.height + 1.0f) return;
        float wheel = mouse.scroll.ReadValue().y;
        if (Mathf.Approximately(wheel, 0.0f)) return;
        if (!RectTransformUtility.RectangleContainsScreenPoint(_body, mouse.position.ReadValue(), null)) return;
        float line = _contentText.fontSize * 1.25f;
        _scroll = Mathf.Clamp(_scroll - Mathf.Sign(wheel) * _wheelLines * line, 0.0f, _contentRect.rect.height - _body.rect.height);
        _contentRect.anchoredPosition = new Vector2(0.0f, Mathf.Round(_scroll));
    }

    // ---- texts

    private string Team(int team) => $"<color=#{ColorUtility.ToHtmlStringRGB(team == 1 ? _superHotColor : _superColdColor)}>{(team == 1 ? "Super Hot" : "Super Cold")}</color>";

    private string BothTeams(int hot, int cold) => $"{Team(1)} {hot}   {Team(2)} {cold}";

    private static string DateText(GameRecord game) => game.LocalDate.ToString("yyyy-MM-dd HH:mm");

    private static string Difficulty(GameRecord game) => string.IsNullOrEmpty(game.difficulty) ? "" : Loc.T(game.difficulty);

    private string LeaderboardText()
    {
        List<GameRecord> board = RecordStore.Leaderboard(Difficulties[_filter], Rows);
        if (board.Count == 0) return Loc.T("No won games against the computer yet.");
        StringBuilder text = new StringBuilder();
        for (int i = 0; i < board.Count; i++)
        {
            GameRecord game = board[i];
            text.Append(i + 1).Append(".<pos=70>").Append(game.HumanScore).Append("<pos=210>").Append(Difficulty(game)).Append("<pos=380>").Append(DateText(game)).Append('\n');
        }
        return text.ToString().TrimEnd();
    }

    private string HistoryLine(GameRecord game)
    {
        string result = game.IsVsComputer ? Loc.T(game.HumanWon ? "Won" : "Lost") : Loc.F("{0} won", Team(game.winner));
        string mode = game.IsVsComputer ? Difficulty(game) : Loc.T("Two players");
        string score = game.IsVsComputer ? game.HumanScore.ToString() : "-";
        return DateText(game) + "<pos=250>" + result + "<pos=490>" + mode + "<pos=690>" + score;
    }

    private string DetailsText(GameRecord game)
    {
        StringBuilder text = new StringBuilder();
        string result = game.IsVsComputer ? Loc.T(game.HumanWon ? "Won" : "Lost") : Loc.F("{0} won", Team(game.winner));
        text.Append(DateText(game)).Append("  ").Append(result).Append('\n');
        text.Append(game.IsVsComputer ? Loc.F("Against the computer ({0})", Difficulty(game)) : Loc.T("Two players")).Append('\n');
        int minutes = Mathf.FloorToInt(game.seconds / 60.0f);
        text.Append(Loc.F("Turns played: {0}", game.turns)).Append("   ").Append(Loc.F("Time: {0}", minutes + ":" + Mathf.FloorToInt(game.seconds % 60.0f).ToString("00"))).Append('\n');
        text.Append(Loc.F("Units called: {0}", BothTeams(game.team1.called, game.team2.called))).Append('\n');
        text.Append(Loc.F("Units killed: {0}", BothTeams(game.team1.killed, game.team2.killed))).Append('\n');
        text.Append(Loc.F("Score: {0}", BothTeams(game.team1.score, game.team2.score))).Append('\n');
        text.Append(Team(1)).Append(": ").Append(string.Join(", ", game.team1.units)).Append('\n');
        text.Append(Team(2)).Append(": ").Append(string.Join(", ", game.team2.units)).Append('\n');
        if (!string.IsNullOrEmpty(game.log)) text.Append('\n').Append(Loc.T("Battle log")).Append(":\n").Append(game.log);
        return text.ToString();
    }

    private string StatsText()
    {
        RecordStats stats = RecordStore.Stats();
        if (stats.Games == 0) return Loc.T("No games yet.");
        StringBuilder text = new StringBuilder();
        text.Append(Loc.F("Games played: {0}", stats.Games)).Append("   ").Append(Loc.F("against the computer: {0}", stats.GamesVsComputer)).Append('\n');
        text.Append(Loc.F("Win rate: {0}", Percent(stats.WinRate, stats.WinsVsComputer, stats.GamesVsComputer))).Append('\n');
        for (int i = 1; i < Difficulties.Length; i++)
        {
            stats.GamesByDifficulty.TryGetValue(Difficulties[i], out int games);
            stats.WinsByDifficulty.TryGetValue(Difficulties[i], out int wins);
            text.Append("  ").Append(Loc.T(Difficulties[i])).Append("<pos=160>").Append(games == 0 ? "-" : Percent(stats.WinRateFor(Difficulties[i]), wins, games)).Append('\n');
        }
        text.Append(Loc.F("Best score: {0}", stats.BestScore)).Append('\n');
        text.Append(Loc.F("Longest win streak: {0}", stats.LongestWinStreak)).Append("   ").Append(Loc.F("current: {0}", stats.CurrentWinStreak)).Append('\n');
        text.Append(Loc.F("Units killed: {0}", stats.UnitsKilled)).Append("   ").Append(Loc.F("Units called: {0}", stats.UnitsCalled)).Append('\n');
        if (!string.IsNullOrEmpty(stats.FavoriteUnit)) text.Append(Loc.F("Favorite unit: {0}", stats.FavoriteUnit));
        return text.ToString().TrimEnd();
    }

    private static string Percent(float rate, int wins, int games) => Mathf.RoundToInt(rate * 100.0f) + "% (" + wins + "/" + games + ")";
}
