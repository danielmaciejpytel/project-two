using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ButtonUnitController : MonoBehaviour
{
    [SerializeField] private Image _killedImage;
    [SerializeField] private Text _unitText;
    private UnitController _myUnit;
    private Image _myImage;

    void Awake()
    {
        _myImage = GetComponent<Image>();
        _myUnit = null;
        _killedImage.enabled = false;

    }

    public void ClickedMe()
    {
        if(!_myUnit._isKilled) EventManager._instance.UnitClicked(_myUnit);
    }

    public void SetUnit(UnitController unit)
    {
        _myUnit = unit;
        _myImage.sprite = unit.GetUnitCard();
        _unitText.text = unit.GetUnitName();
        if (unit._isAvailable) _myImage.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        else _myImage.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
        if (unit._isKilled) _killedImage.enabled = true;
        else _killedImage.enabled = false;
    }

    public void EnlargeUnit(UnitController unit)
    {
        if(unit == _myUnit)
        {
            transform.DOScale(1.0f, 0.3f).SetEase(Ease.InOutBack);
        }
        else
        {
            transform.DOScale(0.8f, 0.3f).SetEase(Ease.InOutBack);
        }
    }

    public void UnitKilled(UnitController unit)
    {
        if (unit == _myUnit)
        {
            _killedImage.enabled = true;
            transform.DOScale(0.6f, 0.3f).SetEase(Ease.InOutBack);
        }
    }

    public void MarkForDeployment()
    {
        if (!_myUnit._isDeployed) gameObject.SetActive(true); 
        else gameObject.SetActive(false);
    }

    public void MarkForAction()
    {
        if (!_myUnit._isDeployed) gameObject.SetActive(false);
        else gameObject.SetActive(true);  
    }

    public void DisableUnit(UnitController unit)
    {
        if(unit == _myUnit) _myImage.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
    }

    public bool IsUnitDeployed()
    {
        return _myUnit._isDeployed;
    }
}
