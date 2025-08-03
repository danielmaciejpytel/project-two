using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIController : MonoBehaviour
{
    [SerializeField] private Text _winnerText;
    [SerializeField] private Image _winnerImage;
    [SerializeField] private UnitTilePanelController _myInfoPanel;
    [SerializeField] private PlayerUnitsController _myUnitsPanel;
    [SerializeField] private Button _deployMinionButton;
    [SerializeField] private Button _abilityButton;
    [SerializeField] private Button _endTurnButton;
    [SerializeField] private Image _timerImage;
    [SerializeField] private Text _timerText;
    private int _myTimer;
    private bool _unitDeployedThisTurn;


    private void Start()
    {
        _deployMinionButton.gameObject.SetActive(true);
        _abilityButton.gameObject.SetActive(false);
        _myTimer = 0;
        _unitDeployedThisTurn = false;
        _winnerText.text = "Turn: Super Cold";
    }

    private IEnumerator TurnTimer(int timeLimit, GameController myGameController)
    {
        while (true)
        {
            if (_myTimer >= timeLimit)
            {
                myGameController.EndTurnAction();
                _myTimer = 0;
            }
            _timerText.text = (timeLimit - _myTimer).ToString();
            _myTimer += 1;
            yield return new WaitForSeconds(1.0f);
        }
    }

    public void DisplayWinner(int winnerId)
    {
        _winnerImage.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.0f), 0.5f);
        if (winnerId == 1) _winnerText.text = "Winner: Super Hot";
        else _winnerText.text = "Winner: Super Cold";
    }

    public void InitializeUnitsPanel(List<UnitController> units, int startingPlayer, GameController myGameController, int timeLimit)
    {
        _myUnitsPanel.InitializePanel(units, startingPlayer);
        StartCoroutine(TurnTimer(timeLimit, myGameController));
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
            _winnerText.text = "Turn: Super Hot";
            _timerImage.transform.Translate(new Vector3(-1670.0f, 0.0f, 0.0f));
            _endTurnButton.transform.Translate(new Vector3(-1750.0f, 0.0f, 0.0f));
            _deployMinionButton.transform.Translate(new Vector3(-1420.0f, 0.0f, 0.0f));
            _abilityButton.transform.Translate(new Vector3(-1092.0f, 0.0f, 0.0f));
        }
        else
        {
            _winnerText.text = "Turn: Super Cold";
            _timerImage.transform.Translate(new Vector3(1670.0f, 0.0f, 0.0f));
            _endTurnButton.transform.Translate(new Vector3(1750.0f, 0.0f, 0.0f));
            _deployMinionButton.transform.Translate(new Vector3(1420.0f, 0.0f, 0.0f));
            _abilityButton.transform.Translate(new Vector3(1092.0f, 0.0f, 0.0f));
        }
        _winnerImage.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.0f), 0.5f);
        SoundController._instance.PlayEndTurn();
    }

    public void SelectUnit(UnitController unit)
    {
        IAbility unitAbility;
        _myUnitsPanel.UnitSelected(unit);
        unitAbility = unit.gameObject.GetComponent<IAbility>();
        if (unitAbility != null && unitAbility.IsAvailableThisTurn())
        {
            _abilityButton.GetComponentInChildren<Text>().text = unitAbility.GetButtonDescription();
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
