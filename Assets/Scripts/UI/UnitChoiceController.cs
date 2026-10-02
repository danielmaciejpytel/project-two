using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The unit choice screen: the two teams choose four doppelgangers in turn (Red Boss, Blue Boss, Red Boss,
// Blue Boss). Choosing one also gives the other team its counterpart, and the chosen kind leaves the list.
public class UnitChoiceController : MonoBehaviour
{
    private const int Picks = 4;
    private const string HotColor = "#FF2A52";
    private const string ColdColor = "#22D3E6";

    private static readonly Color Hot = new Color(1.0f, 0.165f, 0.322f);
    private static readonly Color Cold = new Color(0.133f, 0.827f, 0.902f);
    private static readonly Color DotEmpty = new Color(0.118f, 0.118f, 0.118f, 0.45f);

    [SerializeField] private ChoiceTeamView[] _teams;
    [SerializeField] private GameObject[] _player1UnitPrefabs;
    [SerializeField] private GameObject[] _player2UnitPrefabs;
    [SerializeField] private TMP_Text _title;
    [SerializeField] private TMP_Text _turnText;
    [SerializeField] private RectTransform _dotsRoot;
    [SerializeField] private Image[] _dots;
    [SerializeField] private RectTransform _dotRing;
    [SerializeField] private TMP_Text _hint;
    [SerializeField] private ChoiceButton _backButton;
    [SerializeField] private ChoiceButton _nextButton;
    [SerializeField] private GameController _myGameController;

    // Picks made so far: index into the picking player's prefab list.
    private readonly List<int> _picked = new List<int>();
    private readonly int[] _hovered = { -1, -1 };
    private int _round;
    private int _cursor;
    private Coroutine _aiPick;

    private GameObject[] Prefabs(int player) => player == 1 ? _player1UnitPrefabs : _player2UnitPrefabs;

    private static int PickerOf(int pick) => pick % 2 == 0 ? 1 : 2;

    private static UnitController Unit(GameObject prefab) => prefab.GetComponent<UnitController>();

    private void Awake()
    {
        _backButton.Clicked += BackToModeSelection;
        _nextButton.Clicked += ConfirmPick;
        for (int team = 0; team < _teams.Length; team++)
        {
            int side = team;
            ChoiceSlotView[] slots = _teams[team].Slots;
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].Index = i;
                slots[i].HoverChanged += (slot, hovered) => OnSlotHover(side, slot.Index, hovered);
                slots[i].ArrowPressed += direction => Step(direction);
            }
        }
    }

    private void Start()
    {
        _myGameController.AddUnitPrefab(_player1UnitPrefabs[0], 1);
        _myGameController.AddUnitPrefab(_player2UnitPrefabs[0], 2);
        Refresh();
        StartAiPickIfNeeded();
    }

    // Unit types that are already taken (by either team) are not offered again.
    private List<int> AvailableIndexes(int player)
    {
        var taken = new HashSet<int>();
        for (int i = 0; i < _picked.Count; i++) taken.Add(Unit(Prefabs(PickerOf(i))[_picked[i]]).GetUnitType());
        var indexes = new List<int>();
        GameObject[] prefabs = Prefabs(player);
        for (int i = 1; i < prefabs.Length; i++)
        {
            if (!taken.Contains(Unit(prefabs[i]).GetUnitType())) indexes.Add(i);
        }
        return indexes;
    }

    // The unit of the other team that goes with the given one: the same type, the other variant.
    private GameObject GetOpposingUnit(int picker, GameObject prefab)
    {
        UnitController chosen = Unit(prefab);
        foreach (GameObject candidate in Prefabs(picker == 1 ? 2 : 1))
        {
            UnitController unit = Unit(candidate);
            if (unit.GetUnitType() == chosen.GetUnitType() && unit.GetUnitName() != chosen.GetUnitName()) return candidate;
        }
        return null;
    }

    private GameObject CurrentPick(int picker)
    {
        List<int> available = AvailableIndexes(picker);
        return Prefabs(picker)[available[_cursor % available.Count]];
    }

    // What a team shows in a place: 0 is its Superior, 1 to 4 the doppelgangers, places after the current one stay empty.
    private GameObject UnitAt(int team, int slot)
    {
        int player = team + 1;
        if (slot == 0) return Prefabs(player)[0];
        if (slot > _round + 1 || slot > Picks) return null;
        int picker = PickerOf(slot - 1);
        GameObject pick = slot <= _round ? Prefabs(picker)[_picked[slot - 1]] : CurrentPick(picker);
        return player == picker ? pick : GetOpposingUnit(picker, pick);
    }

    private bool IsAiTurn() => _round < Picks && GameSession.IsAiPlayer(PickerOf(_round));

    private void Refresh()
    {
        int current = Mathf.Min(_round, Picks - 1);
        int picker = PickerOf(current);
        bool arrowsAllowed = !IsAiTurn();
        for (int team = 0; team < _teams.Length; team++)
        {
            Color color = team == 0 ? Hot : Cold;
            ChoiceTeamView view = _teams[team];
            _hovered[team] = -1;
            view.SetIdle(picker != team + 1);
            view.SetTexts(Loc.T(team == 0 ? "TEAM RED" : "TEAM BLUE"), Loc.T("Player " + (team + 1)));
            Sprite ghost = Unit(Prefabs(team + 1)[0]).GetUnitPortrait();
            for (int slot = 0; slot < view.Slots.Length; slot++)
            {
                GameObject prefab = UnitAt(team, slot);
                bool isCurrent = slot == _round + 1;
                view.Slots[slot].Show(prefab != null ? Unit(prefab) : null, ghost, slot == 0,
                    isCurrent && picker == team + 1, isCurrent && picker == team + 1 && arrowsAllowed, color);
            }
            ShowInfo(team, _round + 1);
        }
        RefreshHeader(current, picker);
        bool last = _round == Picks - 1;
        _hint.text = Loc.T("By choosing one, you also make a choice for your enemy.");
        _nextButton.SetText(Loc.T(last ? "DONE" : "NEXT"));
        _nextButton.SetPulsing(last);
        _nextButton.SetInteractable(!IsAiTurn());
        _backButton.SetText(Loc.T("BACK"));
    }

    private void RefreshHeader(int current, int picker)
    {
        _title.text = Loc.T("CHOOSE <color=#D72E66>DOPPELGANGER</color>");
        string who = "<color=" + (picker == 1 ? HotColor : ColdColor) + ">" + Loc.T("YOUR") + "</color>";
        _turnText.text = string.Format(Loc.T("PICK {0} CREW {1} OF {2}"), who, current + 1, Picks);
        _turnText.ForceMeshUpdate();
        _dotsRoot.anchoredPosition = new Vector2(_turnText.rectTransform.anchoredPosition.x + _turnText.preferredWidth + 16.0f, _dotsRoot.anchoredPosition.y);
        for (int i = 0; i < _dots.Length; i++)
        {
            _dots[i].color = i < _round ? (PickerOf(i) == 1 ? Hot : Cold) : DotEmpty;
        }
        _dotRing.gameObject.SetActive(_round < Picks);
        if (_round < Picks) _dotRing.anchoredPosition = _dots[current].rectTransform.anchoredPosition;
    }

    private void ShowInfo(int team, int slot)
    {
        GameObject prefab = UnitAt(team, slot);
        if (prefab == null) prefab = UnitAt(team, Mathf.Min(slot, Picks));
        _teams[team].Info.Show(Unit(prefab), team == 0 ? Hot : Cold);
    }

    private void OnSlotHover(int team, int slot, bool hovered)
    {
        if (hovered && UnitAt(team, slot) == null) return;
        _hovered[team] = hovered ? slot : -1;
        if (hovered) SoundController.Instance?.PlayHover();
        ShowInfo(team, hovered ? slot : _round + 1);
    }

    private void Step(int direction)
    {
        if (IsAiTurn() && _aiPick == null) return;
        int count = AvailableIndexes(PickerOf(_round)).Count;
        _cursor = (_cursor + direction + count) % count;
        SoundController.Instance?.PlayClick();
        Refresh();
    }

    private void ConfirmPick()
    {
        if (IsAiTurn() && _aiPick == null) return;
        int picker = PickerOf(_round);
        SoundController.Instance?.PlayClick();
        GameObject pick = CurrentPick(picker);
        GameObject opposing = GetOpposingUnit(picker, pick);
        _myGameController.AddUnitPrefab(pick, picker);
        _myGameController.AddUnitPrefab(opposing, picker == 1 ? 2 : 1);
        if (_round == Picks - 1)
        {
            gameObject.SetActive(false);
            _myGameController.StartGame();
            return;
        }
        List<int> available = AvailableIndexes(picker);
        _picked.Add(available[_cursor % available.Count]);
        _round++;
        _cursor = 0;
        Refresh();
        StartAiPickIfNeeded();
    }

    // Back: return to the game mode choice in the menu (works for the computer's picks too).
    private void BackToModeSelection()
    {
        StopAllCoroutines();
        GameSession.OpenPlayMenu = true;
        _myGameController.ReturnToMenu();
    }

    private void StartAiPickIfNeeded()
    {
        if (IsAiTurn()) _aiPick = StartCoroutine(AiPick());
    }

    private IEnumerator AiPick()
    {
        yield return new WaitForSeconds(0.6f);
        int steps = Random.Range(0, Mathf.Max(1, AvailableIndexes(PickerOf(_round)).Count - 1));
        for (int i = 0; i < steps; i++)
        {
            Step(1);
            yield return new WaitForSeconds(0.25f);
        }
        yield return new WaitForSeconds(0.5f);
        Coroutine self = _aiPick;
        ConfirmPick();
        if (_aiPick == self) _aiPick = null;
    }
}
