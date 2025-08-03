using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitPanelController : MonoBehaviour
{
    [SerializeField] private Image _myUnitImage;
    [SerializeField] private Text _myUnitText;
    [SerializeField] private Button _myLeftButton;
    [SerializeField] private Button _myRightButton;
    [SerializeField] private UnitTilePanelController _myInfoPanel;

    private GameObject _myUnitPrefab;
    private UnitController _previousUnit;
    private bool _isButtonActive;

    public void ShowUnitInfo()
    {
        SoundController._instance.PlayHover();
        _previousUnit = _myInfoPanel.GetDisplayedUnit();
        _myInfoPanel.DisplayUnit(_myUnitPrefab.GetComponent<UnitController>());
        _myUnitImage.sprite = _myUnitPrefab.GetComponent<UnitController>().GetUnitPortrait();
    }

    public void ShowPreviousUnit()
    {
        if (_previousUnit != null && !_isButtonActive)
        {
            _myInfoPanel.DisplayUnit(_previousUnit);
            _myUnitImage.sprite = _previousUnit.GetUnitPortrait();
        }
    }

    public void SetUnit(GameObject myUnit)
    {
        UnitController myUnitController;
        myUnitController = myUnit.GetComponent<UnitController>();
        _myUnitPrefab = myUnit;
        _myUnitImage.sprite = myUnitController.GetUnitPortrait();
        _myUnitText.text = myUnitController.GetUnitName();
    }

    public void DisableButtons()
    {
        _myLeftButton.gameObject.SetActive(false);
        _myRightButton.gameObject.SetActive(false);
        _isButtonActive = false;
    }

    public GameObject GetUnitPrefab()
    {
        return _myUnitPrefab;
    }

    public void DisableMe()
    {
        gameObject.SetActive(false);
    }

    public void EnableMe()
    {
        gameObject.SetActive(true);
        _isButtonActive = true;
    }
}
