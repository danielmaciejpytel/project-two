using TMPro;
using UnityEngine;

// Keeps a one-line text inside the width of its rect: when the (translated) text is too wide, it takes the next smaller size.
// The sizes go down in steps of 8, so the pixel font stays sharp (on a 4K screen every font pixel is a whole number of screen pixels).
[RequireComponent(typeof(TMP_Text))]
public class FitTextSize : MonoBehaviour
{
    [Tooltip("The size the text has when it fits.")]
    [SerializeField] private float _maxSize = 32.0f;
    [SerializeField] private float _minSize = 16.0f;
    [SerializeField] private float _step = 8.0f;

    private TMP_Text _text;
    private string _fitted;

    private void Awake() => _text = GetComponent<TMP_Text>();

    // The text is set by other scripts (the language, the game), so it is checked after them.
    private void LateUpdate()
    {
        if (_text.text != _fitted) Fit();
    }

    public void Fit()
    {
        _fitted = _text.text;
        float width = _text.rectTransform.rect.width;
        float size = _maxSize;
        _text.fontSize = size;
        while (size - _step >= _minSize && _text.GetPreferredValues(_text.text).x > width)
        {
            size -= _step;
            _text.fontSize = size;
        }
    }
}
