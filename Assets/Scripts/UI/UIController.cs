using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.InputSystem;

public class UIController : MonoBehaviour
{
    [SerializeField] private TMP_Text _winnerText;
    [SerializeField] private Image _winnerImage;
    [SerializeField] private UnitTilePanelController _myInfoPanel;
    [SerializeField] private PlayerUnitsController _myUnitsPanel;
    [SerializeField] private Button _deployMinionButton;
    [SerializeField] private Button _abilityButton;
    [SerializeField] private Button _endTurnButton;
    [SerializeField] private EndGameController _endGame;
    [SerializeField] private Image _timerImage;
    [SerializeField] private TMP_Text _timerText;
    [Tooltip("Horizontal distance between the left edge of the info panel and where the controls on the left side start.")]
    [SerializeField] private float _leftEdgeInset = 6.0f;
    [SerializeField] private Color _superHotColor = new Color32(0xFF, 0x1B, 0x47, 0xFF);
    [SerializeField] private Color _superColdColor = new Color32(0x14, 0xC8, 0xD8, 0xFF);
    [Header("Hints for the first turns")]
    [SerializeField] private GameObject _hintPanel;
    [SerializeField] private TMP_Text _hintText;
    [Header("Time warning")]
    [Tooltip("The seconds are shown in the warning color and pulse for this many seconds before the turn ends.")]
    [SerializeField] private int _warningSeconds = 10;
    [SerializeField] private Color _warningColor = new Color32(0xFF, 0x4D, 0x4D, 0xFF);
    private readonly Dictionary<RectTransform, Vector2> _rightSidePositions = new Dictionary<RectTransform, Vector2>();
    private int _myTimer;
    private Coroutine _turnTimer;
    private bool _unitDeployedThisTurn;
    private Color _timerColor;
    private bool _hintsThisTurn;
    private int _hintPlayerId;

    // Each player gets hints in the first turn of the game.
    private const int TurnsWithHints = 2;
    private const string HintChoose = "Click one of your units to choose it. Call (C) brings a new Doppelganger next to your Superior.";
    private const string HintUnit = "Click a highlighted tile to move, or an enemy in range to attack. End Turn (Space) passes the turn.";
    private const string HintCall = "Pick a card below, then click a tile next to your Superior. Press Call (C) again to cancel.";


    private void Start()
    {
        _deployMinionButton.gameObject.SetActive(true);
        _abilityButton.gameObject.SetActive(false);
        _myTimer = 0;
        _unitDeployedThisTurn = false;
        _timerColor = _timerText.color;
        _winnerText.text = Loc.F("Turn: {0}", PlayerLabel(2));
        if (_hintPanel != null) _hintPanel.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.spaceKey.wasPressedThisFrame) PressEndTurnShortcut();
        if (Keyboard.current.cKey.wasPressedThisFrame) PressCallShortcut();
        if (_endGame != null && Keyboard.current.escapeKey.wasPressedThisFrame && GameIsBeingPlayed()) _endGame.HandleEscape();
    }

    // Only while a game is being played: not during the unit draft and not after the game has ended.
    private static bool GameIsBeingPlayed()
    {
        GameController game = GameController.Instance;
        return game != null && game.CurrentState != null && !game.IsGameOver;
    }

    // Space does what the End Turn button does, C what Call does; not while the pause menu is open, and only when the button is there.
    public bool PressEndTurnShortcut()
    {
        if (!ShortcutsAvailable() || !_endTurnButton.gameObject.activeSelf) return false;
        GameController.Instance.EndTurnAction();
        return true;
    }

    public bool PressCallShortcut()
    {
        if (!ShortcutsAvailable() || !_deployMinionButton.gameObject.activeSelf) return false;
        GameController.Instance.DeployAction();
        return true;
    }

    // Not while the computer plays: the buttons do not react to a human then either.
    private bool ShortcutsAvailable() => GameIsBeingPlayed() && (_endGame == null || !_endGame.IsOpen) && !GameSession.IsAiPlayer(GameController.Instance.ActivePlayer);

    private IEnumerator TurnTimer(int timeLimit, GameController myGameController)
    {
        WaitForSeconds oneSecond = new WaitForSeconds(1.0f);
        while (true)
        {
            // The turn can't end during an animation; the end is retried every second and starting the
            // next turn resets the timer.
            if (_myTimer >= timeLimit) myGameController.TurnTimeExpired();
            int remaining = Mathf.Max(0, timeLimit - _myTimer);
            _timerText.text = remaining.ToString();
            UpdateTimeWarning(remaining);
            CenterTimerTexts();
            _myTimer += 1;
            yield return oneSecond;
        }
    }

    // The last seconds of the turn: the digits and the label turn to the warning color and the timer pulses.
    private void UpdateTimeWarning(int remainingSeconds)
    {
        bool warning = remainingSeconds > 0 && remainingSeconds <= _warningSeconds;
        Color color = warning ? _warningColor : _timerColor;
        _timerText.color = color;
        TMP_Text label = TimerLabel();
        if (label != null) label.color = color;
        if (!warning) return;
        SoundController.Instance?.PlayTick();
        _timerImage.transform.DOComplete();
        _timerImage.transform.DOPunchScale(new Vector3(0.06f, 0.06f, 0.0f), 0.4f).SetLink(_timerImage.gameObject);
    }

    private TMP_Text TimerLabel()
    {
        foreach (TMP_Text text in _timerImage.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text != _timerText) return text;
        }
        return null;
    }

    // "Turn ends in:" and the seconds are two texts; together they are centred on the timer background.
    // The seconds are measured as two digits, so the label does not shift when the count drops below 10.
    private void CenterTimerTexts()
    {
        TMP_Text label = TimerLabel();
        if (label == null) return;
        const float gap = 10.0f;
        float labelWidth = label.GetPreferredValues(label.text).x;
        float secondsWidth = _timerText.GetPreferredValues("00").x;
        float left = -(labelWidth + gap + secondsWidth) * 0.5f;
        RectTransform labelRect = label.rectTransform;
        RectTransform secondsRect = _timerText.rectTransform;
        labelRect.anchoredPosition = new Vector2(left + labelWidth, labelRect.anchoredPosition.y);
        secondsRect.anchoredPosition = new Vector2(left + labelWidth + gap, secondsRect.anchoredPosition.y);
    }

    public void DisplayWinner(int winnerId)
    {
        // The game is over, the timer would otherwise keep ending turns (and clicking) in the background.
        if (_turnTimer != null) StopCoroutine(_turnTimer);
        _turnTimer = null;
        _winnerImage.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.0f), 0.5f).SetLink(_winnerImage.gameObject);
        _winnerText.text = Loc.F("Winner: {0}", PlayerLabel(winnerId));
        // Nothing is left to play: only the winner and the end screen stay.
        _endTurnButton.gameObject.SetActive(false);
        _deployMinionButton.gameObject.SetActive(false);
        _abilityButton.gameObject.SetActive(false);
        _timerImage.gameObject.SetActive(false);
        if (_hintPanel != null) _hintPanel.SetActive(false);
        if (_endGame != null) _endGame.Show(_winnerImage.rectTransform, GameController.Instance != null ? GameController.Instance.Result : null);
    }

    public void InitializeUnitsPanel(List<UnitController> units, int startingPlayer, GameController myGameController, int timeLimit)
    {
        _myUnitsPanel.InitializePanel(units, startingPlayer);
        // The scene is laid out for Super Cold; move the turn controls if Super Hot starts.
        MoveActivePlayerControls(startingPlayer == 1 ? -1.0f : 1.0f);
        _winnerText.text = Loc.F("Turn: {0}", PlayerLabel(startingPlayer));
        StartHints(startingPlayer, true);
        if (_turnTimer != null) StopCoroutine(_turnTimer);
        _turnTimer = StartCoroutine(TurnTimer(timeLimit, myGameController));
    }

    public void DisplayTile(TileController tile)
    {
        _myInfoPanel.DisplayTile(tile);
    }

    public void DisplayUnit(UnitController unit)
    {
        _myInfoPanel.DisplayUnit(unit);
    }

    public void ClearDisplay()
    {
        _myInfoPanel.ClearDisplay();
    }

    public void StartPlayerTurn(int playerId)
    {
        _myTimer = 0;
        _timerText.color = _timerColor;
        if (TimerLabel() != null) TimerLabel().color = _timerColor;
        // The turn counter still holds the turns that started before this one.
        StartHints(playerId, GameController.Instance != null && GameController.Instance.Stats.Turns + 1 <= TurnsWithHints);
        _myInfoPanel.ClearDisplay();
        _myUnitsPanel.SetNewPlayer(playerId);
        if (!_myUnitsPanel.AllUnitsDeployed()) _deployMinionButton.gameObject.SetActive(true);
        else _deployMinionButton.gameObject.SetActive(false);
        _unitDeployedThisTurn = false;
        _abilityButton.gameObject.SetActive(false);
        if (playerId == 1)
        {
            _winnerText.text = Loc.F("Turn: {0}", PlayerLabel(1));
            MoveActivePlayerControls(-1.0f);
        }
        else
        {
            _winnerText.text = Loc.F("Turn: {0}", PlayerLabel(2));
            MoveActivePlayerControls(1.0f);
        }
        _winnerImage.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.0f), 0.5f).SetLink(_winnerImage.gameObject);
        SoundController.Instance?.PlayEndTurn();
    }

    // Moves the turn controls to the active player's side: right edge for Super Cold, left edge for Super Hot.
    // The controls are laid out for the right side. For Super Hot they are mirrored around the middle of the
    // screen (not the middle of their parent, which can be offset), and the left edges line up with the info panel.
    private void MoveActivePlayerControls(float direction)
    {
        RectTransform[] controls = { _timerImage.rectTransform, (RectTransform)_endTurnButton.transform, (RectTransform)_deployMinionButton.transform, (RectTransform)_abilityButton.transform };
        foreach (RectTransform control in controls) MirrorToSide(control, direction);
        if (direction >= 0.0f) return;
        // The buttons keep their order and spacing, so they move together; the timer lines up on its own.
        AlignLeftEdge(controls[1], new[] { controls[1], controls[2], controls[3] });
        AlignLeftEdge(controls[0], new[] { controls[0] });
    }

    private void MirrorToSide(RectTransform target, float direction)
    {
        if (!_rightSidePositions.TryGetValue(target, out Vector2 right))
        {
            right = target.anchoredPosition;
            right.x = ScreenCentreX(target) + Mathf.Abs(right.x - ScreenCentreX(target));
            _rightSidePositions[target] = right;
        }
        Vector2 position = right;
        if (direction < 0.0f) position.x = 2.0f * ScreenCentreX(target) - right.x;
        target.anchoredPosition = position;
    }

    // The middle of the screen in the local coordinates of the target's parent.
    private float ScreenCentreX(RectTransform target)
    {
        return target.parent.InverseTransformPoint(target.GetComponentInParent<Canvas>().rootCanvas.transform.position).x;
    }

    // Moves the group so the left edge of the leader lines up with the left edge of the info panel.
    private void AlignLeftEdge(RectTransform leader, RectTransform[] group)
    {
        Vector3[] leaderCorners = new Vector3[4];
        Vector3[] infoCorners = new Vector3[4];
        leader.GetWorldCorners(leaderCorners);
        ((RectTransform)_myInfoPanel.transform).GetWorldCorners(infoCorners);
        float scale = leader.GetComponentInParent<Canvas>().rootCanvas.transform.lossyScale.x;
        float shift = infoCorners[0].x + _leftEdgeInset * scale - leaderCorners[0].x;
        Vector3 local = leader.parent.InverseTransformVector(new Vector3(shift, 0.0f, 0.0f));
        foreach (RectTransform member in group) member.anchoredPosition += new Vector2(local.x, 0.0f);
    }

    // Player name in the team color, e.g. "Super Cold" in cyan.
    private string PlayerLabel(int playerId)
    {
        Color color = playerId == 1 ? _superHotColor : _superColdColor;
        string name = playerId == 1 ? "Super Hot" : "Super Cold";
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{name}</color>";
    }

    // Hints are for the first turns and only for a human player; each step of the turn has its own.
    private void StartHints(int playerId, bool hintsThisTurn)
    {
        _hintPlayerId = playerId;
        _hintsThisTurn = hintsThisTurn;
        ShowHint(HintChoose);
    }

    private void ShowHint(string key)
    {
        if (_hintPanel == null) return;
        bool show = _hintsThisTurn && !GameSession.IsAiPlayer(_hintPlayerId);
        _hintPanel.SetActive(show);
        if (show) _hintText.text = Loc.T(key);
    }

    public void SelectUnit(UnitController unit)
    {
        ShowHint(HintUnit);
        IAbility unitAbility;
        _myUnitsPanel.UnitSelected(unit);
        unitAbility = unit.gameObject.GetComponent<IAbility>();
        if (unitAbility != null && unitAbility.IsAvailableThisTurn())
        {
            _abilityButton.GetComponentInChildren<TMP_Text>().text = Loc.T(unitAbility.GetButtonDescription());
            _abilityButton.gameObject.SetActive(true);
        }
        else _abilityButton.gameObject.SetActive(false);
    }

    // Picking a card in the "Call" mode: its unit isn't on the board, so it has no ability to offer.
    public void SelectUnitToDeploy(UnitController unit)
    {
        _myUnitsPanel.UnitSelected(unit);
        _abilityButton.gameObject.SetActive(false);
    }

    public void KillUnit(UnitController unit)
    {
        _myUnitsPanel.UnitKilled(unit);
    }

    public void ShowDeployableUnits()
    {
        ShowHint(HintCall);
        _myUnitsPanel.ShowDeployableMinions();
    }

    public void EndDeployment()
    {
        ShowHint(HintChoose);
        _myUnitsPanel.EndDeployment();
        _deployMinionButton.gameObject.SetActive(false);
        _unitDeployedThisTurn = true;
    }

    // Leaves the "Call" mode without calling anyone: the button stays, the turn's call is not used up.
    public void CancelDeployment()
    {
        ShowHint(HintChoose);
        _myUnitsPanel.EndDeployment();
    }

    public void MarkUnitUnavailable(UnitController unit)
    {
        ShowHint(HintChoose);
        _myUnitsPanel.MarkUnitUnavailable(unit);
    }

    public bool AllUnitsDeployed()
    {
        return _myUnitsPanel.AllUnitsDeployed();
    }

    public bool DeployedThisTurn()
    {
        return _unitDeployedThisTurn;
    }
}
