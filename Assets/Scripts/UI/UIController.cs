using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.InputSystem;

public class UIController : MonoBehaviour
{
    [Tooltip("The player name in the turn banner; on the end screen the winner.")]
    [SerializeField] private TMP_Text _winnerText;
    [SerializeField] private Image _winnerImage;
    [SerializeField] private UnitTilePanelController _myInfoPanel;
    [SerializeField] private PlayerUnitsController _myUnitsPanel;
    [SerializeField] private Button _deployMinionButton;
    [SerializeField] private Button _abilityButton;
    [SerializeField] private Button _endTurnButton;
    [SerializeField] private EndGameController _endGame;
    [Tooltip("The caption and the seconds of the turn timer, on the right of the turn banner.")]
    [SerializeField] private Image _timerImage;
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private Color _superHotColor = new Color32(0xFF, 0x1B, 0x47, 0xFF);
    [SerializeField] private Color _superColdColor = new Color32(0x14, 0xC8, 0xD8, 0xFF);
    [Header("Screen corners")]
    [Tooltip("The top corners the controls of the player who has the turn move between.")]
    [SerializeField] private ScreenCorners _corners;
    [Header("Turn banner")]
    [Tooltip("\"Turn 5\" above the player name.")]
    [SerializeField] private TMP_Text _turnLabel;
    [Tooltip("The time bar under the texts of the banner: the track and the fill that shrinks with the time.")]
    [SerializeField] private GameObject _timerBar;
    [SerializeField] private Image _timerFill;
    [Tooltip("What the banner shows in the computer's turn instead of the seconds: \"Computer\" and \"Thinking...\".")]
    [SerializeField] private GameObject _computerBlock;
    [SerializeField] private TMP_Text _thinkingText;
    [Tooltip("Seconds between two steps of the dots of \"Thinking...\".")]
    [SerializeField] private float _thinkingStep = 0.35f;
    [Tooltip("The controls look like this when they can't be used (the computer's turn).")]
    [SerializeField] private float _lockedAlpha = 0.38f;
    [Tooltip("The main button is in the color of the team that has the turn.")]
    [SerializeField] private Sprite _primaryRedSprite;
    [SerializeField] private Sprite _primaryBlueSprite;
    [Header("Damage preview")]
    [Tooltip("The callout next to an enemy in range with the damage the chosen unit would do to him.")]
    [SerializeField] private DamageCalloutController _damageCallout;
    [Header("Hints for the first turns")]
    [SerializeField] private GameObject _hintPanel;
    [SerializeField] private TMP_Text _hintText;
    [Header("Time warning")]
    [Tooltip("The seconds are shown in the warning color and pulse for this many seconds before the turn ends.")]
    [SerializeField] private int _warningSeconds = 10;
    [SerializeField] private Color _warningColor = new Color32(0xFF, 0x4D, 0x4D, 0xFF);
    [Tooltip("The bar blinks between full and this opacity in the warning.")]
    [SerializeField] private float _warningBlinkAlpha = 0.35f;
    private int _myTimer;
    private Coroutine _turnTimer;
    private bool _unitDeployedThisTurn;
    private Color _timerColor;
    private bool _hintsThisTurn;
    private int _hintPlayerId;
    private int _timeLimit;
    private float _turnStartTime;
    private bool _warning;
    private bool _computerTurn;
    private int _bannerPlayer = 2;

    // Each player gets hints in the first turn of the game.
    private const int TurnsWithHints = 2;
    private const string HintChoose = "Click one of your units to choose it.\nCall (C) brings a new Doppelganger.";
    private const string HintUnit = "Click a highlighted tile to move, or an enemy in range to attack.";
    private const string HintCall = "Pick a card from the reserve, then click a flashing tile next to your Superior. Press Call (C) again to cancel.";
    // The "..." at the end of "Thinking..." that is revealed one dot at a time.
    private const int ThinkingDots = 3;


    private void Start()
    {
        _deployMinionButton.gameObject.SetActive(true);
        _abilityButton.gameObject.SetActive(false);
        _myTimer = 0;
        _unitDeployedThisTurn = false;
        _timerColor = _timerText.color;
        ShowBanner(2);
        if (_hintPanel != null) _hintPanel.SetActive(false);
    }

    private void Update()
    {
        UpdateTimerBar();
        UpdateThinking();
        if (Keyboard.current == null) return;
        if (Keyboard.current.spaceKey.wasPressedThisFrame) PressEndTurnShortcut();
        if (Keyboard.current.cKey.wasPressedThisFrame) PressCallShortcut();
        if (Keyboard.current.qKey.wasPressedThisFrame) PressAbilityShortcut();
        if (_endGame != null && Keyboard.current.escapeKey.wasPressedThisFrame && GameIsBeingPlayed()) _endGame.HandleEscape();
    }

    // Only while a game is being played: not during the unit draft and not after the game has ended.
    private static bool GameIsBeingPlayed()
    {
        GameController game = GameController.Instance;
        return game != null && game.CurrentState != null && !game.IsGameOver;
    }

    // Space does what the End Turn button does, C what Call does, Q the ability of the chosen unit; not while the pause menu is open,
    // and only when the button is there.
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

    public bool PressAbilityShortcut()
    {
        if (!ShortcutsAvailable() || !_abilityButton.gameObject.activeSelf) return false;
        GameController.Instance.AbilityAction();
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
            _myTimer += 1;
            yield return oneSecond;
        }
    }

    // The last seconds of the turn: the digits and the label turn to the warning color and the timer pulses.
    private void UpdateTimeWarning(int remainingSeconds)
    {
        bool warning = remainingSeconds > 0 && remainingSeconds <= _warningSeconds;
        _warning = warning && !_computerTurn;
        Color color = warning ? _warningColor : _timerColor;
        _timerText.color = color;
        TMP_Text label = TimerLabel();
        if (label != null) label.color = color;
        if (!warning) return;
        SoundController.Instance?.PlayTick();
        _timerText.transform.DOComplete();
        _timerText.transform.DOPunchScale(new Vector3(0.12f, 0.12f, 0.0f), 0.4f).SetLink(_timerText.gameObject);
    }

    private TMP_Text TimerLabel()
    {
        foreach (TMP_Text text in _timerImage.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text != _timerText) return text;
        }
        return null;
    }

    // The time bar shrinks smoothly with the time left of the turn; in the warning it takes the warning color and blinks.
    // In the computer's turn it stays full.
    private void UpdateTimerBar()
    {
        if (_timerFill == null || _turnTimer == null || _timeLimit <= 0) return;
        float left = _computerTurn ? 1.0f : Mathf.Clamp01((_timeLimit - (Time.time - _turnStartTime)) / _timeLimit);
        _timerFill.fillAmount = left;
        Color color = _computerTurn || !_warning ? TeamColor(_bannerPlayer) : _warningColor;
        if (_warning) color.a = Mathf.FloorToInt(Time.time * 2.0f) % 2 == 0 ? 1.0f : _warningBlinkAlpha;
        _timerFill.color = color;
    }

    // "THINKING" with dots that appear one by one (none, one, two, three) while the computer plays. The word stays in place.
    private void UpdateThinking()
    {
        if (!_computerTurn || _thinkingText == null) return;
        int dots = Mathf.FloorToInt(Time.time / Mathf.Max(0.05f, _thinkingStep)) % 4;
        _thinkingText.maxVisibleCharacters = Mathf.Max(0, _thinkingText.text.Length - ThinkingDots + dots);
    }

    // The turn banner: turn number, the player in his color, the timer or, in the computer's turn, "Thinking...".
    private void ShowBanner(int playerId)
    {
        _bannerPlayer = playerId;
        _computerTurn = GameSession.IsAiPlayer(playerId);
        _warning = false;
        int turn = GameController.Instance != null ? GameController.Instance.Stats.Turns + 1 : 1;
        _winnerText.text = PlayerLabel(playerId);
        if (_turnLabel != null) _turnLabel.text = Loc.F("TURN {0}", turn);
        _timerImage.gameObject.SetActive(!_computerTurn);
        if (_computerBlock != null) _computerBlock.SetActive(_computerTurn);
        if (_thinkingText != null && _computerTurn) _thinkingText.text = Loc.T("THINKING...");
        if (_timerBar != null) _timerBar.SetActive(true);
        if (_timerFill != null)
        {
            _timerFill.fillAmount = 1.0f;
            _timerFill.color = TeamColor(playerId);
        }
        _timerText.color = _timerColor;
        if (TimerLabel() != null) TimerLabel().color = _timerColor;
        if (_timeLimit > 0 && !_computerTurn) _timerText.text = _timeLimit.ToString();
        if (_primaryRedSprite != null && _primaryBlueSprite != null && _endTurnButton.TryGetComponent(out Image primary)) primary.sprite = playerId == 1 ? _primaryRedSprite : _primaryBlueSprite;
        SetControlsLocked(_computerTurn);
    }

    // The buttons of the column are dimmed while the computer plays: they can't be used then.
    private void SetControlsLocked(bool locked)
    {
        foreach (Button button in new[] { _endTurnButton, _deployMinionButton, _abilityButton })
        {
            if (!button.TryGetComponent(out CanvasGroup group)) group = button.gameObject.AddComponent<CanvasGroup>();
            group.alpha = locked ? _lockedAlpha : 1.0f;
        }
    }

    private Color TeamColor(int playerId) => playerId == 1 ? _superHotColor : _superColdColor;

    public void DisplayWinner(int winnerId)
    {
        // The game is over, the timer would otherwise keep ending turns (and clicking) in the background.
        if (_turnTimer != null) StopCoroutine(_turnTimer);
        _turnTimer = null;
        _computerTurn = false;
        _warning = false;
        HideDamagePreview();
        _winnerImage.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.0f), 0.5f).SetLink(_winnerImage.gameObject);
        _winnerText.text = Loc.F("Winner: {0}", PlayerLabel(winnerId));
        // Nothing is left to play: only the winner and the end screen stay.
        _endTurnButton.gameObject.SetActive(false);
        _deployMinionButton.gameObject.SetActive(false);
        _abilityButton.gameObject.SetActive(false);
        _timerImage.gameObject.SetActive(false);
        if (_timerBar != null) _timerBar.SetActive(false);
        if (_computerBlock != null) _computerBlock.SetActive(false);
        if (_turnLabel != null) _turnLabel.gameObject.SetActive(false);
        if (_hintPanel != null) _hintPanel.SetActive(false);
        if (_endGame != null) _endGame.Show(_winnerImage.rectTransform, GameController.Instance != null ? GameController.Instance.Result : null);
    }

    public void InitializeUnitsPanel(List<UnitController> units, int startingPlayer, GameController myGameController, int timeLimit)
    {
        _myUnitsPanel.InitializePanel(units, startingPlayer);
        _timeLimit = timeLimit;
        _turnStartTime = Time.time;
        // The scene is laid out for Super Cold; move the turn controls if Super Hot starts.
        MoveActivePlayerControls(startingPlayer == 1 ? -1.0f : 1.0f);
        ShowBanner(startingPlayer);
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

    public void ShowDamagePreview(UnitController attacker, UnitController target, int attackPower)
    {
        if (_damageCallout != null) _damageCallout.Show(attacker, target, attackPower);
    }

    public void HideDamagePreview()
    {
        if (_damageCallout != null) _damageCallout.Hide();
    }

    public void StartPlayerTurn(int playerId)
    {
        _myTimer = 0;
        _turnStartTime = Time.time;
        // The turn counter still holds the turns that started before this one.
        StartHints(playerId, GameController.Instance != null && GameController.Instance.Stats.Turns + 1 <= TurnsWithHints);
        _myInfoPanel.ClearDisplay();
        _myUnitsPanel.SetNewPlayer(playerId);
        if (!_myUnitsPanel.AllUnitsDeployed()) _deployMinionButton.gameObject.SetActive(true);
        else _deployMinionButton.gameObject.SetActive(false);
        _unitDeployedThisTurn = false;
        _abilityButton.gameObject.SetActive(false);
        MoveActivePlayerControls(playerId == 1 ? -1.0f : 1.0f);
        ShowBanner(playerId);
        _winnerImage.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.0f), 0.5f).SetLink(_winnerImage.gameObject);
        SoundController.Instance?.PlayEndTurn();
    }

    // Moves the turn controls to the active player's side: left column for Super Hot, right column for Super Cold
    // (the main button and Call in a row, the ability button under them; the battle log follows in BattleLogController).
    private void MoveActivePlayerControls(float direction)
    {
        RectTransform[] controls = { (RectTransform)_endTurnButton.transform, (RectTransform)_deployMinionButton.transform, (RectTransform)_abilityButton.transform };
        // The controls stick to the top corner on the active player's side.
        if (_corners != null)
        {
            Transform corner = direction < 0.0f ? _corners.TopLeft : _corners.TopRight;
            foreach (RectTransform control in controls)
            {
                if (control.parent != corner) control.SetParent(corner, false);
            }
        }
        float x = direction < 0.0f ? HudLayout.LeftColumnX : HudLayout.RightColumnX;
        HudLayout.Place(controls[0], x, HudLayout.ButtonsY, HudLayout.PrimaryButtonWidth, HudLayout.ButtonHeight);
        HudLayout.Place(controls[1], x + HudLayout.PrimaryButtonWidth + HudLayout.ButtonGap, HudLayout.ButtonsY, HudLayout.CallButtonWidth, HudLayout.ButtonHeight);
        HudLayout.Place(controls[2], x, HudLayout.ButtonsY + HudLayout.RowStep, HudLayout.ColumnWidth, HudLayout.ButtonHeight);
    }

    // Player name in the team color, e.g. "Super Cold" in cyan.
    private string PlayerLabel(int playerId)
    {
        Color color = TeamColor(playerId);
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
