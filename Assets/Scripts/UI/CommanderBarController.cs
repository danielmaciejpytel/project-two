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
        // Work in the bar's local (canvas) space, so spacing and scale follow the Canvas Scaler.
        Vector3 firstPoint = transform.InverseTransformPoint(_healthPointPoint.transform.position);
        float step = playerId == 1 ? 27.0f : -27.0f;

        _healthPoints = new List<Image>();
        for (int i = 0; i < newHealth; i++)
        {
            Image newPoint = Instantiate(_healthPointPrefab, transform, false).GetComponent<Image>();
            newPoint.transform.localPosition = firstPoint + new Vector3(i * step, 0.0f, 0.0f);
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
        // Destroy the whole point objects, destroying only the Image component leaked an empty object per point.
        foreach (Image point in _healthPoints)
        {
            Destroy(point.gameObject);
        }
        CreateBar(newHealth);
    }
}
