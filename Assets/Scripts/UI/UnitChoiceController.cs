using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public struct ChosenUnit
{
    public int playerId;
    public int unitType;

    public ChosenUnit(int p, int t)
    {
        playerId = p;
        unitType = t;
    }

    public static bool operator ==(ChosenUnit cu1, ChosenUnit cu2)
    {
        if (cu1.playerId == cu2.playerId && cu1.unitType == cu2.unitType) return true;
        else return false;
    }

    public static bool operator !=(ChosenUnit cu1, ChosenUnit cu2)
    {
        if (cu1.playerId != cu2.playerId || cu1.unitType != cu2.unitType) return true;
        else return false;
    }

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

public class UnitChoiceController : MonoBehaviour
{
    [SerializeField] private UnitPanelController[] _player1Panels;
    [SerializeField] private GameObject[] _player1UnitPrefabs;
    [SerializeField] private UnitTilePanelController _player1InfoPanel;
    [SerializeField] private UnitPanelController[] _player2Panels;
    [SerializeField] private GameObject[] _player2UnitPrefabs;
    [SerializeField] private UnitTilePanelController _player2InfoPanel;
    [SerializeField] private Button _nextButton;
    [SerializeField] private GameController _myGameController;
    [SerializeField] private Text _myDescription;

    private UnitPanelController _currentUnitPanel;
    private UnitPanelController _currentOpponentUnitPanel;
    private List<ChosenUnit> _chosenUnits;
    private int _currentPanelIndex;
    private int _currentPlayer;
    private int _currentUnitIndex;

    private void Start()
    {
        int i = 0;
        foreach(UnitPanelController unitPanel in _player1Panels)
        {
            if (i <= 1)
            {
                if (i == 0)
                {
                    unitPanel.SetUnit(_player1UnitPrefabs[0]);
                    unitPanel.DisableButtons();
                    _myGameController.AddUnitPrefab(unitPanel.GetUnitPrefab(), 1);
                }
                else _currentUnitPanel = unitPanel;
            }
            else unitPanel.DisableMe();
            i++;
        }
        i = 0;
        foreach (UnitPanelController unitPanel in _player2Panels)
        {
            if (i <= 1)
            {
                if (i == 0)
                {
                    unitPanel.SetUnit(_player2UnitPrefabs[0]);
                    _myGameController.AddUnitPrefab(unitPanel.GetUnitPrefab(), 2);
                }
                else _currentOpponentUnitPanel = unitPanel;
                unitPanel.DisableButtons();
            }
            else unitPanel.DisableMe();
            i++;
        }
        _currentPanelIndex = 1;
        _currentPlayer = 1;
        _currentUnitIndex = 1;
        _currentUnitPanel.SetUnit(_player1UnitPrefabs[1]);
        _currentOpponentUnitPanel.SetUnit(_player2UnitPrefabs[1]);
        _player1InfoPanel.DisplayUnit(_player1UnitPrefabs[1].GetComponent<UnitController>());
        _player2InfoPanel.DisplayUnit(_player2UnitPrefabs[1].GetComponent<UnitController>());
        _chosenUnits = new List<ChosenUnit>();
    }

    private GameObject GetOpposingUnit(int lookForType, string unitName)
    {
        UnitController myUnitController;    
        if(_currentPlayer == 1)
        {
            foreach(GameObject myGO in _player2UnitPrefabs)
            {
                myUnitController = myGO.GetComponent<UnitController>();
                if (myUnitController.GetUnitType() == lookForType && myUnitController.GetUnitName() != unitName) return myGO;
            }
        }
        else
        {
            foreach (GameObject myGO in _player1UnitPrefabs)
            {
                myUnitController = myGO.GetComponent<UnitController>();
                if (myUnitController.GetUnitType() == lookForType && myUnitController.GetUnitName() != unitName) return myGO;
            }
        }
        return null;
    }

    public void NextUnitPanel()
    {
        Text buttonText;
        UnitController currentUnitController;
        GameObject opposingUnit;
        bool unitValid;

        SoundController._instance.PlayClick();
        currentUnitController = _currentUnitPanel.GetUnitPrefab().GetComponent<UnitController>();
        _chosenUnits.Add(new ChosenUnit(currentUnitController.GetPlayerId(), currentUnitController.GetUnitType()));
        _myGameController.AddUnitPrefab(_currentUnitPanel.GetUnitPrefab(), _currentPlayer);
        _currentUnitPanel.DisableButtons();
        if (_currentPanelIndex != 0)    // if minion was chosen
        {
            currentUnitController = _currentOpponentUnitPanel.GetUnitPrefab().GetComponent<UnitController>();
            _chosenUnits.Add(new ChosenUnit(currentUnitController.GetPlayerId(), currentUnitController.GetUnitType()));
            _myGameController.AddUnitPrefab(_currentOpponentUnitPanel.GetUnitPrefab(), _currentPlayer == 1 ? 2 : 1);
            _currentOpponentUnitPanel.DisableButtons();
        }
        /*else
        {
            // if commander was chosen
        }
        {
            _player2Panels[0].gameObject.SetActive(true);
            _player2InfoPanel.gameObject.SetActive(true);
            _player2InfoPanel.DisplayUnit(_player2UnitPrefabs[0].GetComponent<UnitController>());
            _myDescription.text = "Player 2: Choose your commander";
        }*/
        if (_currentPlayer == 2 && _currentPanelIndex >= _player1Panels.Length-1)
        {
            gameObject.SetActive(false);
            _myGameController.StartGame();
            return;
        }
        if (_currentPlayer == 1)
        {
            _currentPlayer = 2;
            _currentUnitIndex = 1;
            if (_currentPanelIndex != 0)    // if minion is being chosen
            {
                unitValid = false;
                while (!unitValid)
                {
                    currentUnitController = _player2UnitPrefabs[_currentUnitIndex].GetComponent<UnitController>();
                    if (!_chosenUnits.Contains(new ChosenUnit(currentUnitController.GetPlayerId(), currentUnitController.GetUnitType()))) unitValid = true;
                    else _currentUnitIndex++;
                }
                _currentPanelIndex++;
                _currentOpponentUnitPanel = _player1Panels[_currentPanelIndex];
                _currentOpponentUnitPanel.EnableMe();
                _currentOpponentUnitPanel.DisableButtons();
                opposingUnit = GetOpposingUnit(currentUnitController.GetUnitType(), currentUnitController.GetUnitName());
                _currentOpponentUnitPanel.SetUnit(opposingUnit);
                _player1InfoPanel.DisplayUnit(opposingUnit.GetComponent<UnitController>());
                _myDescription.text = "Super Cold: Choose doppelganger.";
                _currentUnitPanel = _player2Panels[_currentPanelIndex];
                _currentUnitPanel.SetUnit(_player2UnitPrefabs[_currentUnitIndex]);
                _player2InfoPanel.DisplayUnit(currentUnitController);
            }
            else
            {
                currentUnitController = _player2UnitPrefabs[0].GetComponent<UnitController>();
                _currentUnitPanel = _player2Panels[_currentPanelIndex];
                _currentUnitPanel.SetUnit(_player2UnitPrefabs[0]);
                _player2InfoPanel.DisplayUnit(currentUnitController);
            }
        }
        else
        {
            _currentPlayer = 1;
            _currentUnitIndex = 1;
            unitValid = false;
            while (!unitValid)
            {
                currentUnitController = _player1UnitPrefabs[_currentUnitIndex].GetComponent<UnitController>();
                if (!_chosenUnits.Contains(new ChosenUnit(currentUnitController.GetPlayerId(), currentUnitController.GetUnitType()))) unitValid = true;
                else _currentUnitIndex++;
            }
            _currentPanelIndex++;
            _currentOpponentUnitPanel = _player2Panels[_currentPanelIndex];
            _currentOpponentUnitPanel.EnableMe();
            _currentOpponentUnitPanel.DisableButtons();
            opposingUnit = GetOpposingUnit(currentUnitController.GetUnitType(), currentUnitController.GetUnitName());
            _currentOpponentUnitPanel.SetUnit(opposingUnit);
            _currentUnitPanel = _player1Panels[_currentPanelIndex];
            _currentUnitPanel.SetUnit(_player1UnitPrefabs[_currentUnitIndex]);
            _player2InfoPanel.DisplayUnit(opposingUnit.GetComponent<UnitController>());
            _player1InfoPanel.DisplayUnit(currentUnitController);
            _myDescription.text = "Super Hot: Choose doppelganger.";
        }
        if (_currentPlayer == 2 && _currentPanelIndex + 1 == _player2Panels.Length)
        {
            buttonText = _nextButton.GetComponentInChildren<Text>();
            buttonText.text = "Done";
        }
        _currentUnitPanel.EnableMe();
    }

    public void NextUnit(string direction)
    {
        GameObject opposingUnit;
        UnitController currentUnitController;
        bool unitValid;

        SoundController._instance.PlayClick();
        unitValid = false;
        while (!unitValid)
        {
            if (direction == "right")
            {
                if (_currentPlayer == 1)
                {
                    if (_currentUnitIndex == _player1UnitPrefabs.Length - 1) _currentUnitIndex = 1;
                    else _currentUnitIndex++;
                }
                else
                {
                    if (_currentUnitIndex == _player2UnitPrefabs.Length - 1) _currentUnitIndex = 1;
                    else _currentUnitIndex++;
                }
            }
            else
            {
                if (_currentPlayer == 1)
                {
                    if (_currentUnitIndex == 1) _currentUnitIndex = _player1UnitPrefabs.Length - 1;
                    else _currentUnitIndex--;
                }
                else
                {
                    if (_currentUnitIndex == 1) _currentUnitIndex = _player2UnitPrefabs.Length - 1;
                    else _currentUnitIndex--;
                }
            }
            if (_currentPlayer == 1)
            {
                currentUnitController = _player1UnitPrefabs[_currentUnitIndex].GetComponent<UnitController>();
            }
            else
            {
                currentUnitController = _player2UnitPrefabs[_currentUnitIndex].GetComponent<UnitController>();
            }
            if (!_chosenUnits.Contains(new ChosenUnit(currentUnitController.GetPlayerId(), currentUnitController.GetUnitType()))) unitValid = true;
        }
        if(_currentPlayer == 1)
        {
            _currentUnitPanel.SetUnit(_player1UnitPrefabs[_currentUnitIndex]);
            currentUnitController = _player1UnitPrefabs[_currentUnitIndex].GetComponent<UnitController>();
            _player1InfoPanel.DisplayUnit(currentUnitController);
            opposingUnit = GetOpposingUnit(currentUnitController.GetUnitType(), currentUnitController.GetUnitName());
            _currentOpponentUnitPanel.SetUnit(opposingUnit);
            _player2InfoPanel.DisplayUnit(opposingUnit.GetComponent<UnitController>()); 
        }
        else
        {
            _currentUnitPanel.SetUnit(_player2UnitPrefabs[_currentUnitIndex]);
            currentUnitController = _player2UnitPrefabs[_currentUnitIndex].GetComponent<UnitController>();
            _player2InfoPanel.DisplayUnit(currentUnitController);
            opposingUnit = GetOpposingUnit(currentUnitController.GetUnitType(), currentUnitController.GetUnitName());
            _currentOpponentUnitPanel.SetUnit(opposingUnit);
            _player1InfoPanel.DisplayUnit(opposingUnit.GetComponent<UnitController>());
        }
    }
}
