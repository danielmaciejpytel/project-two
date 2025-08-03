using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CommanderBarController : MonoBehaviour
{
    [SerializeField] private GameObject _healthPointPrefab;
    [SerializeField] private GameObject _healthPointPoint;
    [SerializeField] private ScriptableUnit _kingScriptableUnit;
    [SerializeField] private int playerId;
    private List<Image> _healthPoints;

    private void CreateBar(int newHealth)
    {
        Image newPoint;
        Vector3 pointPosition;

        _healthPoints = new List<Image>();
        for (int i = 0; i < newHealth; i++)
        {
            newPoint = Instantiate(_healthPointPrefab, Vector3.zero, Quaternion.identity).GetComponent<Image>();
            newPoint.transform.SetParent(transform);
            pointPosition = _healthPointPoint.transform.position;
            if (playerId == 1) pointPosition.x += i * 27.0f;
            else pointPosition.x -= i * 27.0f;
            newPoint.transform.position = pointPosition;
            _healthPoints.Add(newPoint);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        CreateBar(_kingScriptableUnit.unitHealth);
    }

    public void SetNewValue(int newHealth)
    {
        foreach(Image i in _healthPoints)
        {
            Destroy(i);
        }
        CreateBar(newHealth);
    }
}
