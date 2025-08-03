using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthController : MonoBehaviour
{
    [SerializeField] private GameObject _healthPointDesignerPrefab;
    [SerializeField] private GameObject _healthPointPrefab;
    [SerializeField] private Sprite _healthDesignerSprite;
    [SerializeField] private Sprite _lostHealthDesignerSprite;
    [SerializeField] private Sprite _healthPlayer1Point;
    [SerializeField] private Sprite _healthPlayer2Point;
    private CommanderBarController _myBar;


    private List<SpriteRenderer> _designerHealthPointsList;
    private List<SpriteRenderer> _healthPointsList;
    private int _healthPoints, _initialHealthPoints;
    private bool _showPotentialDamage;
    private bool _isDesignerMode;
    private int _playerId;

    private void CreateHealthBar()
    {
        GameObject newPoint;
        SpriteRenderer pointRenderer;
        for (int i = 0; i < _initialHealthPoints; i++)
        {
            newPoint = Instantiate(_healthPointDesignerPrefab, Vector3.zero, Quaternion.identity);
            newPoint.transform.SetParent(this.transform);
            if (i < 5) newPoint.transform.localPosition = new Vector3(-0.3f + 0.2f * i, 0.8f, 0.0f);
            else if (i < 10) newPoint.transform.localPosition = new Vector3(-0.3f + 0.2f * (i - 5), 0.6f, 0.0f);
            else newPoint.transform.localPosition = new Vector3(-0.3f + 0.2f * (i - 10), 0.4f, 0.0f);
            _designerHealthPointsList.Add(newPoint.GetComponent<SpriteRenderer>());
            newPoint = Instantiate(_healthPointPrefab, Vector3.zero, Quaternion.identity);
            newPoint.transform.SetParent(this.transform);
            pointRenderer = newPoint.GetComponent<SpriteRenderer>();
            if (_playerId == 1)
            {
                newPoint.transform.localPosition = new Vector3(-0.8f, -0.7f + i * 0.1f, 0.0f);
                pointRenderer.sprite = _healthPlayer1Point;
            }
            else
            {
                newPoint.transform.localPosition = new Vector3(0.8f, -0.7f + i * 0.1f, 0.0f);
                pointRenderer.sprite = _healthPlayer2Point;
            }
            _healthPointsList.Add(pointRenderer);
        }
    }

    private void UpdateHealthBar()
    {
        int i = 0;
        foreach (SpriteRenderer healthPointRenderer in _designerHealthPointsList)
        {
            if (i < _healthPoints) healthPointRenderer.sprite = _healthDesignerSprite;
            else healthPointRenderer.sprite = _lostHealthDesignerSprite;
            i++;
        }
        i = 0;
        foreach (SpriteRenderer healthPointRenderer in _healthPointsList)
        {
            if (i < _healthPoints) healthPointRenderer.enabled = true;
            else healthPointRenderer.enabled = false;
            i++;
        }
    }

    public void InitializeHealth(int health, int myPlayerId, bool isKing)
    {
        _healthPoints = health;
        _initialHealthPoints = health;
        _designerHealthPointsList = new List<SpriteRenderer>();
        _healthPointsList = new List<SpriteRenderer>();
        _showPotentialDamage = false;
        _isDesignerMode = false;
        _playerId = myPlayerId;
        CreateHealthBar();
        SetMode("player");
        if (isKing)
        {
            if (_playerId == 1) _myBar = GameObject.Find("RedBarImage").GetComponent<CommanderBarController>();
            else _myBar = GameObject.Find("BlueBarImage").GetComponent<CommanderBarController>();
        }
        else _myBar = null;
    }

    // returns true if unit dead
    public bool ChangeHealth(int healthChange)
    {
        _healthPoints += healthChange;
        if (_healthPoints < 0) _healthPoints = 0;
        if (_healthPoints > _initialHealthPoints) _healthPoints = _initialHealthPoints;
        UpdateHealthBar();
        if (_myBar != null) _myBar.SetNewValue(_healthPoints);
        if (_healthPoints == 0) return true;
        else return false;   
    }

    // returns true if unit dead
    public bool ChangeHPNumber(int change)
    {
        for (int i = _designerHealthPointsList.Count - 1; i >= 0; i--)
        {
            Destroy(_designerHealthPointsList[i]);
        }
        _designerHealthPointsList.Clear();
        for (int i = _healthPointsList.Count - 1; i >= 0; i--)
        {
            Destroy(_healthPointsList[i]);
        }
        _healthPointsList.Clear();
        _initialHealthPoints += change;
        if (_initialHealthPoints <= 0) return true;
        _healthPoints += change;
        CreateHealthBar();
        UpdateHealthBar();
        if(_isDesignerMode) SetMode("designer");
        else SetMode("player");
        if (_myBar != null) _myBar.SetNewValue(_healthPoints);
        if (_healthPoints <= 0) return true;
        else return false;
    }

    public int GetCurrentHealth()
    {
        return _healthPoints;
    }

    public void StopShowingPotentialDamage()
    {
        _showPotentialDamage = false;
    }

    public IEnumerator ShowPotentialDamage(int damage)
    {
        int currentHealth, minHealth;
        if (_showPotentialDamage) yield break;
        else
        {
            _showPotentialDamage = true;
            currentHealth = _healthPoints;
            minHealth = _healthPoints - damage;
            if (minHealth < 0) minHealth = 0;
            while (_showPotentialDamage)
            {
                for (int i = _healthPoints - 1; i >= minHealth; i--)
                {
                    if (_isDesignerMode) _designerHealthPointsList[i].sprite = _lostHealthDesignerSprite;
                    else _healthPointsList[i].enabled = false;
                }
                yield return new WaitForSeconds(0.25f);
                for (int i = _healthPoints - 1; i >= minHealth; i--)
                {
                    if(_isDesignerMode) _designerHealthPointsList[i].sprite = _healthDesignerSprite;
                    else _healthPointsList[i].enabled = true;
                }
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    public void SetMode(string newMode)
    {
        if(newMode == "designer")
        {
            _isDesignerMode = true;
            foreach(SpriteRenderer healthPointRenderer in _designerHealthPointsList)
            {
                healthPointRenderer.enabled = true;
            }
            foreach (SpriteRenderer healthPointRenderer in _healthPointsList)
            {
                healthPointRenderer.enabled = false;
            }
        }
        else
        {
            _isDesignerMode = false;
            foreach (SpriteRenderer healthPointRenderer in _designerHealthPointsList)
            {
                healthPointRenderer.enabled = false;
            }
            foreach (SpriteRenderer healthPointRenderer in _healthPointsList)
            {
                healthPointRenderer.enabled = true;
            }
        }
    }
}
