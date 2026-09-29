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
    [SerializeField] private Color _superHotColor = new Color32(0xFF, 0x1B, 0x47, 0xFF);
    [SerializeField] private Color _superColdColor = new Color32(0x14, 0xC8, 0xD8, 0xFF);
    private int _myTimer;
    private Coroutine _turnTimer;
    private bool _unitDeployedThisTurn;


    private void Start()
    {
        _deployMinionButton.gameObject.SetActive(true);
        _abilityButton.gameObject.SetActive(false);
        _myTimer = 0;
        _unitDeployedThisTurn = false;
        _winnerText.text = "Turn: " + PlayerLabel(2);
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
        _winnerText.text = "Winner: " + PlayerLabel(winnerId);
    }

    public void InitializeUnitsPanel(List<UnitController> units, int startingPlayer, GameController myGameController, int timeLimit)
    {
        _myUnitsPanel.InitializePanel(units, startingPlayer);
        // The scene is laid out for Super Cold; move the turn controls if Super Hot starts.
        MoveActivePlayerControls(startingPlayer == 1 ? -1.0f : 1.0f);
        _winnerText.text = "Turn: " + PlayerLabel(startingPlayer);
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
            _winnerText.text = "Turn: " + PlayerLabel(1);
            MoveActivePlayerControls(-1.0f);
        }
        else
        {
            _winnerText.text = "Turn: " + PlayerLabel(2);
            MoveActivePlayerControls(1.0f);
        }
        _winnerImage.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.0f), 0.5f).SetLink(_winnerImage.gameObject);
        SoundController.Instance.PlayEndTurn();
    }

    // Moves the turn controls to the active player's side: left half for Super Hot, right half for Super Cold.
    // Mirroring (instead of shifting by fixed offsets) keeps them in place whatever their size.
    private void MoveActivePlayerControls(float direction)
    {
        MirrorToSide(_timerImage.rectTransform, direction);
        MirrorToSide((RectTransform)_endTurnButton.transform, direction);
        MirrorToSide((RectTransform)_deployMinionButton.transform, direction);
        MirrorToSide((RectTransform)_abilityButton.transform, direction);
    }

    private static void MirrorToSide(RectTransform target, float direction)
    {
        Vector2 position = target.anchoredPosition;
        position.x = Mathf.Abs(position.x) * Mathf.Sign(direction);
        target.anchoredPosition = position;
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
            _abilityButton.GetComponentInChildren<TMP_Text>().text = unitAbility.GetButtonDescription();
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
