using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// The team panel: the cards of the player who has the turn (all five, the reserve included), the name of the team and how many units are in play.
public class PlayerUnitsController : MonoBehaviour
{
    [SerializeField] Button[] _unitButtons;
    [SerializeField] private TMP_Text _teamText;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private Color _hotColor = new Color32(0xFF, 0x1B, 0x47, 0xFF);
    [SerializeField] private Color _coldColor = new Color32(0x14, 0xC8, 0xD8, 0xFF);

    private List<UnitController> _myUnitList;

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void InitializePanel(List<UnitController> unitList, int startingPlayer)
    {
        _myUnitList = unitList;
        SetNewPlayer(startingPlayer);
        gameObject.SetActive(true);
    }

    public void SetNewPlayer(int playerId)
    {
        ButtonUnitController buttonController;
        int i = 0;
        foreach (UnitController unit in _myUnitList)
        {
            if (unit.GetPlayerId() == playerId && i < _unitButtons.Length)
            {
                buttonController = _unitButtons[i].GetComponent<ButtonUnitController>();
                buttonController.SetUnit(unit);
                buttonController.EnlargeUnit(null);
                buttonController.gameObject.SetActive(true);
                i++;
            }
        }
        if (_teamText != null)
        {
            _teamText.text = Loc.T(playerId == 1 ? "TEAM RED" : "TEAM BLUE");
            _teamText.color = playerId == 1 ? _hotColor : _coldColor;
        }
        RefreshCount();
    }

    // "In play 4/5": the units that stand on the board, not the reserve and not the killed.
    private void RefreshCount()
    {
        if (_countText == null) return;
        int inPlay = 0;
        foreach (Button myButton in _unitButtons)
        {
            if (myButton.GetComponent<ButtonUnitController>().IsInPlay()) inPlay++;
        }
        _countText.text = Loc.F("IN PLAY {0}/{1}", inPlay, _unitButtons.Length);
    }

    public void UnitSelected(UnitController unit)
    {
        ButtonUnitController buttonController;
        foreach(Button myButton in _unitButtons)
        {
            buttonController = myButton.GetComponent<ButtonUnitController>();
            buttonController.EnlargeUnit(unit);
        }
    }

    public void ShowDeployableMinions()
    {
        ButtonUnitController buttonController;
        foreach (Button myButton in _unitButtons)
        {
            buttonController = myButton.GetComponent<ButtonUnitController>();
            buttonController.MarkForDeployment();
        }
    }

    public void EndDeployment()
    {
        ButtonUnitController buttonController;
        foreach (Button myButton in _unitButtons)
        {
            buttonController = myButton.GetComponent<ButtonUnitController>();
            buttonController.MarkForAction();
        }
        RefreshCount();
    }

    public void UnitKilled(UnitController unit)
    {
        ButtonUnitController buttonController;
        foreach (Button myButton in _unitButtons)
        {
            buttonController = myButton.GetComponent<ButtonUnitController>();
            buttonController.UnitKilled(unit);
        }
        RefreshCount();
    }

    public void MarkUnitUnavailable(UnitController unit)
    {
        ButtonUnitController buttonController;
        foreach (Button myButton in _unitButtons)
        {
            buttonController = myButton.GetComponent<ButtonUnitController>();
            buttonController.DisableUnit(unit);
        }
    }

    public bool AllUnitsDeployed()
    {
        ButtonUnitController buttonController;
        foreach (Button myButton in _unitButtons)
        {
            buttonController = myButton.GetComponent<ButtonUnitController>();
            if(!buttonController.IsUnitDeployed()) return false;
        }
        return true;
    }
}
