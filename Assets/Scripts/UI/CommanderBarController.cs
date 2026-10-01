using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The health of a team's commander: one square per health point under the team name. Lost points stay as dark squares.
public class CommanderBarController : MonoBehaviour
{
    [SerializeField] private GameObject _healthPointPrefab;
    [SerializeField] private GameObject _healthPointPoint;
    [SerializeField] private ScriptableUnit _kingScriptableUnit;
    [SerializeField] private int playerId;
    [Tooltip("A full health point.")]
    [SerializeField] private Sprite _fullSprite;
    [Tooltip("A lost health point.")]
    [SerializeField] private Sprite _lostSprite;
    [Tooltip("Distance between two health points (the square and the gap).")]
    [SerializeField] private float _step = 20.0f;
    [Tooltip("The stripes of the bar, from the one nearest to the team name to the farthest: one for every living member of the team. " +
             "When a member of the team dies, the farthest stripe goes.")]
    [SerializeField] private GameObject[] _stripes;
    private List<Image> _healthPoints;
    private int _health;

    private void CreateBar(int maxHealth)
    {
        // Work in the bar's local (canvas) space, so spacing and scale follow the Canvas Scaler.
        Vector3 firstPoint = transform.InverseTransformPoint(_healthPointPoint.transform.position);
        // Super Cold's bar is mirrored: its points start at the right edge and grow to the left.
        float step = playerId == 1 ? _step : -_step;

        _healthPoints = new List<Image>();
        for (int i = 0; i < maxHealth; i++)
        {
            Image newPoint = Instantiate(_healthPointPrefab, transform, false).GetComponent<Image>();
            newPoint.transform.localPosition = firstPoint + new Vector3(i * step, 0.0f, 0.0f);
            _healthPoints.Add(newPoint);
        }
        Show(_health);
    }

    // Start is called before the first frame update
    void Start()
    {
        _health = _kingScriptableUnit.unitHealth;
        CreateBar(_health);
        EventManager events = EventManager.Instance;
        if (events == null) return;
        events.OnUnitKilled += OnUnitKilled;
        events.OnTurnStarted += OnTurnStarted;
    }

    private void OnDestroy()
    {
        EventManager events = EventManager.Instance;
        if (events == null) return;
        events.OnUnitKilled -= OnUnitKilled;
        events.OnTurnStarted -= OnTurnStarted;
    }

    private void OnUnitKilled(UnitController killed) => ShowLivingMembers(killed);

    private void OnTurnStarted(int player) => ShowLivingMembers(null);

    // One stripe for every member of the team that is still alive (the reserve counts); the farthest stripes are hidden first.
    private void ShowLivingMembers(UnitController justKilled)
    {
        GameController game = GameController.Instance;
        if (game == null || _stripes == null || game.Units.Count == 0) return;
        int alive = 0;
        foreach (UnitController unit in game.Units)
        {
            if (unit.GetPlayerId() == playerId && !unit.IsKilled && unit != justKilled) alive++;
        }
        for (int i = 0; i < _stripes.Length; i++) _stripes[i].SetActive(i < alive);
    }

    public void SetNewValue(int newHealth)
    {
        _health = newHealth;
        Show(newHealth);
    }

    private void Show(int health)
    {
        for (int i = 0; i < _healthPoints.Count; i++)
        {
            bool full = i < health;
            Image point = _healthPoints[i];
            if (_fullSprite != null && _lostSprite != null) point.sprite = full ? _fullSprite : _lostSprite;
            else point.enabled = full;
        }
    }
}
