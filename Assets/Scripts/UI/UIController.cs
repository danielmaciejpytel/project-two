using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UIController : MonoBehaviour
{
    [SerializeField] private TMP_Text _winnerText;
    [SerializeField] private Image _winnerImage;
    [SerializeField] private UnitTilePanelController _myInfoPanel;
    [SerializeField] private PlayerUnitsController _myUnitsPanel;
    [SerializeField] private Button _deployMinionButton;
    [SerializeField] private Button _abilityButton;
    [SerializeField] private Button _endTurnButton;
    [SerializeField] private Image _timerImage;
    [SerializeField] private TMP_Text _timerText;
    [Tooltip("Horizontal distance between the left edge of the info panel and where the controls on the left side start.")]
    [SerializeField] private float _leftEdgeInset = 6.0f;
    [SerializeField] private Color _superHotColor = new Color32(0xFF, 0x1B, 0x47, 0xFF);
    [SerializeField] private Color _superColdColor = new Color32(0x14, 0xC8, 0xD8, 0xFF);
    private readonly Dictionary<RectTransform, Vector2> _rightSidePositions = new Dictionary<RectTransform, Vector2>();
    private int _myTimer;
    private Coroutine _turnTimer;
    private bool _unitDeployedThisTurn;


    private void Start()
    {
        _deployMinionButton.gameObject.SetActive(true);
        _abilityButton.gameObject.SetActive(false);
        _myTimer = 0;
        _unitDeployedThisTurn = false;
        _winnerText.text = Loc.F("Turn: {0}", PlayerLabel(2));
    }

    private IEnumerator TurnTimer(int timeLimit, GameController myGameController)
    {
        WaitForSeconds oneSecond = new WaitForSeconds(1.0f);
        while (true)
        {
            if (_myTimer >= timeLimit)
            {
                myGameController.TurnTimeExpired();
                _myTimer = 0;
            }
            _timerText.text = (timeLimit - _myTimer).ToString();
            _myTimer += 1;
            yield return oneSecond;
        }
    }

    public void DisplayWinner(int winnerId)
    {
        // The game is over, the timer would otherwise keep ending turns (and clicking) in the background.
        if (_turnTimer != null) StopCoroutine(_turnTimer);
        _turnTimer = null;
        _winnerImage.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.0f), 0.5f).SetLink(_winnerImage.gameObject);
        _winnerText.text = Loc.F("Winner: {0}", PlayerLabel(winnerId));
    }

    public void InitializeUnitsPanel(List<UnitController> units, int startingPlayer, GameController myGameController, int timeLimit)
    {
        _myUnitsPanel.InitializePanel(units, startingPlayer);
        // The scene is laid out for Super Cold; move the turn controls if Super Hot starts.
        MoveActivePlayerControls(startingPlayer == 1 ? -1.0f : 1.0f);
        _winnerText.text = Loc.F("Turn: {0}", PlayerLabel(startingPlayer));
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

    public void SelectUnit(UnitController unit)
    {
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

    public void KillUnit(UnitController unit)
    {
        _myUnitsPanel.UnitKilled(unit);
    }

    public void ShowDeployableUnits()
    {
        _myUnitsPanel.ShowDeployableMinions();
    }

    public void EndDeployment()
    {
        _myUnitsPanel.EndDeployment();
        _deployMinionButton.gameObject.SetActive(false);
        _unitDeployedThisTurn = true;
    }

    public void MarkUnitUnavailable(UnitController unit)
    {
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
