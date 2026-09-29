using TMPro;
using UnityEngine;

/// <summary>
/// Keeps text drawn with the pixel font crisp: the font size is chosen so that one pixel of the font
/// covers a whole number of screen pixels at the current resolution (no uneven, half-pixel strokes).
/// Sizes are given in font pixels at the 1920x1080 reference; at other resolutions they are rounded
/// down to a whole number of screen pixels. When even one screen pixel per font pixel would be too big
/// (small text at low resolutions), the smooth fallback font is drawn at the designed size instead,
/// so the layout never breaks.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class PixelText : MonoBehaviour
{
    // The pixel font atlas is sampled at 80 pt with 5 atlas pixels per font pixel, so 1 font pixel = 16 pt.
    public const float PointsPerFontPixel = 16.0f;

    [Tooltip("Text size in font pixels at 1920x1080 (1 = 16 pt, 2 = 32 pt, 3 = 48 pt ...).")]
    [SerializeField, Min(1)] private int _fontPixels = 2;
    [Tooltip("Use a smaller whole-pixel size when the text doesn't fit its rectangle.")]
    [SerializeField] private bool _shrinkToFit;
    [Tooltip("Smooth (SDF) version of the same font, used when the pixel font can't be drawn small enough.")]
    [SerializeField] private TMP_FontAsset _fallbackFont;

    private TMP_Text _text;
    private RectTransform _rect;
    private Canvas _canvas;
    private float _appliedScale = -1.0f;
    private string _appliedText;
    private Vector2 _appliedRectSize;
    private TMP_FontAsset _pixelFont;

    public int FontPixels
    {
        get => _fontPixels;
        set
        {
            _fontPixels = Mathf.Max(1, value);
            Apply();
        }
    }

    public bool ShrinkToFit
    {
        get => _shrinkToFit;
        set
        {
            _shrinkToFit = value;
            Apply();
        }
    }

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
        _rect = (RectTransform)transform;
        _text.enableAutoSizing = false;
        if (_pixelFont == null) _pixelFont = _text.font;
    }

    private void OnEnable()
    {
        Apply();
    }

    private void LateUpdate()
    {
        // Re-snap after a resolution change, new text content or a layout change.
        if (!Mathf.Approximately(GetScale(), _appliedScale) || _text.text != _appliedText || _rect.rect.size != _appliedRectSize) Apply();
    }

    private float GetScale()
    {
        if (_canvas == null)
        {
            Canvas parent = GetComponentInParent<Canvas>();
            if (parent != null) _canvas = parent.rootCanvas;
        }
        return _canvas != null ? _canvas.scaleFactor : 1.0f;
    }

    private void Apply()
    {
        if (_text == null) Awake();
        float scale = Mathf.Max(0.01f, GetScale());

        // Round down: the text is never bigger than designed at 1080p, so it can't outgrow its box.
        int screenPixels = Mathf.FloorToInt(_fontPixels * scale + 0.001f);
        SetFont(_pixelFont);
        if (_shrinkToFit)
        {
            while (screenPixels > 1 && !Fits(SizeFor(screenPixels, scale))) screenPixels--;
        }
        bool pixelFits = screenPixels >= 1 && (!_shrinkToFit || Fits(SizeFor(screenPixels, scale)));
        if (pixelFits || _fallbackFont == null)
        {
            _text.fontSize = SizeFor(Mathf.Max(1, screenPixels), scale);
        }
        else
        {
            SetFont(_fallbackFont);
            float size = _fontPixels * PointsPerFontPixel;
            if (_shrinkToFit)
            {
                float minSize = size * 0.5f;
                while (size > minSize && !Fits(size)) size -= 1.0f;
            }
            _text.fontSize = size;
        }

        _appliedScale = scale;
        _appliedText = _text.text;
        _appliedRectSize = _rect.rect.size;
    }

    private void SetFont(TMP_FontAsset font)
    {
        if (font == null || _text.font == font) return;
        _text.font = font;
        _text.fontSharedMaterial = font.material;
    }

    private static float SizeFor(int screenPixels, float scale) => screenPixels / scale * PointsPerFontPixel;

    private bool Fits(float fontSize)
    {
        _text.fontSize = fontSize;
        Rect area = _rect.rect;
        bool wraps = _text.textWrappingMode != TextWrappingModes.NoWrap;
        // Single-line text only has to fit the width; wrapped text has to fit the height.
        Vector2 preferred = _text.GetPreferredValues(_text.text, wraps ? area.width : float.PositiveInfinity, float.PositiveInfinity);
        return wraps ? preferred.y <= area.height + 0.5f : preferred.x <= area.width + 0.5f;
    }
}
