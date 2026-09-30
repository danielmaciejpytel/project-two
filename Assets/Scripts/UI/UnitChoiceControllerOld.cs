using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public readonly struct ChosenUnit : IEquatable<ChosenUnit>
{
    public readonly int playerId;
    public readonly int unitType;

    public ChosenUnit(int p, int t)
    {
        playerId = p;
        unitType = t;
    }

    public bool Equals(ChosenUnit other) => playerId == other.playerId && unitType == other.unitType;

    public override bool Equals(object obj) => obj is ChosenUnit other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(playerId, unitType);

    public static bool operator ==(ChosenUnit cu1, ChosenUnit cu2) => cu1.Equals(cu2);

    public static bool operator !=(ChosenUnit cu1, ChosenUnit cu2) => !cu1.Equals(cu2);
}

// The previous unit choice screen, kept in the scene as "UnitChoicePanelOld" (inactive) for reference.
public class UnitChoiceControllerOld : MonoBehaviour
{
    [SerializeField] private UnitPanelController[] _player1Panels;
    [SerializeField] private GameObject[] _player1UnitPrefabs;
    [SerializeField] private UnitTilePanelController _player1InfoPanel;
    [SerializeField] private UnitPanelController[] _player2Panels;
    [SerializeField] private GameObject[] _player2UnitPrefabs;
    [SerializeField] private UnitTilePanelController _player2InfoPanel;
    [SerializeField] private Button _nextButton;
    [SerializeField] private GameController _myGameController;
    [SerializeField] private TMP_Text _myDescription;

    private UnitPanelController _currentUnitPanel;
    private UnitPanelController _currentOpponentUnitPanel;
    private List<ChosenUnit> _chosenUnits;
    private int _currentPanelIndex;
    private int _currentPlayer;
    private int _currentUnitIndex;
    private bool _aiPicking;

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
        StartAiPickIfNeeded();
    }

    // Both lines stay visible during the whole draft.
    private static string ChoicePrompt(string firstLine) => Loc.T(firstLine) + " \n" + Loc.T("By choosing one, you also make a choice for your enemy.");

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

    // Ignore the human pressing buttons while the computer is choosing.
    private bool IsBlockedByAi() => GameSession.IsAiPlayer(_currentPlayer) && !_aiPicking;

    private void StartAiPickIfNeeded()
    {
        if (GameSession.IsAiPlayer(_currentPlayer)) StartCoroutine(AiPick());
    }

    private IEnumerator AiPick()
    {
        _nextButton.interactable = false;
        yield return new WaitForSeconds(0.6f);
        _aiPicking = true;
        int unitCount = _currentPlayer == 1 ? _player1UnitPrefabs.Length : _player2UnitPrefabs.Length;
        int steps = UnityEngine.Random.Range(0, Mathf.Max(1, unitCount - 1));
        _aiPicking = false;
        for (int i = 0; i < steps; i++)
        {
            _aiPicking = true;
            NextUnit("right");
            _aiPicking = false;
            yield return new WaitForSeconds(0.25f);
        }
        yield return new WaitForSeconds(0.5f);
        _nextButton.interactable = true;
        _aiPicking = true;
        NextUnitPanel();
        _aiPicking = false;
    }

    // Back: return to the game mode choice in the menu (works for the computer's picks too).
    public void BackToModeSelection()
    {
        StopAllCoroutines();
        GameSession.OpenPlayMenu = true;
        _myGameController.QuitPressed();
    }

    public void NextUnitPanel()
    {
        TMP_Text buttonText;
        UnitController currentUnitController;
        GameObject opposingUnit;
        bool unitValid;

        if (IsBlockedByAi()) return;

        SoundController.Instance?.PlayClick();
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
            _myDescription.text = Loc.T("Player 2: Choose your commander");
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
                _myDescription.text = ChoicePrompt("Super Cold: Choose doppelganger.");
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
            _myDescription.text = ChoicePrompt("Super Hot: Choose doppelganger.");
        }
        if (_currentPlayer == 2 && _currentPanelIndex + 1 == _player2Panels.Length)
        {
            buttonText = _nextButton.GetComponentInChildren<TMP_Text>();
            buttonText.text = Loc.T("Done");
        }
        _currentUnitPanel.EnableMe();
        if (GameSession.IsAiPlayer(_currentPlayer)) _currentUnitPanel.DisableButtons();
        StartAiPickIfNeeded();
    }

    public void NextUnit(string direction)
    {
        GameObject opposingUnit;
        UnitController currentUnitController;
        bool unitValid;

        if (IsBlockedByAi()) return;
        SoundController.Instance?.PlayClick();
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
