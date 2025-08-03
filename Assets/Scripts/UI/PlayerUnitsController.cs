using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUnitsController : MonoBehaviour
{
    [SerializeField] Button[] _unitButtons;

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
                if (!unit._isDeployed) buttonController.gameObject.SetActive(false);
                else buttonController.gameObject.SetActive(true);
                i++;
            }
        }
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
    }

    public void UnitKilled(UnitController unit)
    {
        ButtonUnitController buttonController;
        foreach (Button myButton in _unitButtons)
        {
            buttonController = myButton.GetComponent<ButtonUnitController>();
            buttonController.UnitKilled(unit);
        }
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
