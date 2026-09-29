using TMPro;
using UnityEngine;

/// <summary>
/// Keeps a text of the scene in the chosen language. The key is the English text; the text is
/// refreshed whenever the language changes. With "Fit Button Width" the button around the text
/// follows the text's width (its left edge stays in place), since translations differ in length.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField, TextArea] private string _key;
    [SerializeField] private bool _fitButtonWidth;
    [SerializeField] private float _fitPadding = 6.0f;

    private TMP_Text _text;
    private RectTransform _button;
    private float _leftEdge;

    public string Key => _key;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
        if (_fitButtonWidth)
        {
            _button = (RectTransform)transform.parent;
            _leftEdge = _button.anchoredPosition.x - _button.pivot.x * _button.rect.width;
        }
    }

    private void OnEnable()
    {
        Loc.Changed += Apply;
        Apply();
    }

    private void OnDisable()
    {
        Loc.Changed -= Apply;
    }

    private void Apply()
    {
        _text.text = Loc.T(_key);
        if (!_fitButtonWidth) return;
        _text.ForceMeshUpdate();
        float width = _text.preferredWidth + _fitPadding;
        _button.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        _button.anchoredPosition = new Vector2(_leftEdge + _button.pivot.x * width, _button.anchoredPosition.y);
    }
}
