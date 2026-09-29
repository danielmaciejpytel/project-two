using UnityEngine;

/// <summary>
/// Calm, minimal motion for the artwork in the main menu. The artwork is split into its separate
/// (unconnected) parts, and every part drifts and tilts a little on its own slow sine waves.
/// Motion is in the local units of the artwork (half a canvas pixel), use Intensity to scale it all.
/// </summary>
public class ForegroundAnimator : MonoBehaviour
{
    [System.Serializable]
    public class Piece
    {
        public RectTransform rect;
        [Tooltip("Largest offset from the resting position, horizontally and vertically.")]
        public Vector2 amplitude;
        [Tooltip("Largest tilt in degrees.")]
        public float rotation;
        [Tooltip("Only sinks from the resting position and comes back, never rises above it. For parts that sit on the edge of the screen, so no gap appears under them.")]
        public bool downOnly;
        [Tooltip("Seconds for one slow cycle.")]
        public float period = 8.0f;
        [Range(0.0f, 1.0f)] public float phase;
    }

    [SerializeField] private Piece[] _pieces;
    [Tooltip("Scales all movement, 0 keeps the artwork still.")]
    [SerializeField, Range(0.0f, 2.0f)] private float _intensity = 1.0f;

    private void OnDisable()
    {
        // Back to the resting positions, e.g. when the menu is hidden.
        if (_pieces == null) return;
        foreach (Piece piece in _pieces)
        {
            if (piece.rect == null) continue;
            piece.rect.anchoredPosition = Vector2.zero;
            piece.rect.localRotation = Quaternion.identity;
        }
    }

    private void Update()
    {
        // Unscaled time, so the menu keeps moving whatever the time scale is.
        float time = Time.unscaledTime;
        const float turn = 2.0f * Mathf.PI;
        foreach (Piece piece in _pieces)
        {
            if (piece.rect == null || piece.period <= 0.0f) continue;
            float cycle = time / piece.period + piece.phase;
            // Different speeds for x, y and tilt, so the path is a slow loop and not a straight line.
            float x = Mathf.Sin(turn * cycle * 0.77f + piece.phase * 5.0f) * piece.amplitude.x;
            float wave = Mathf.Sin(turn * cycle);
            float y = (piece.downOnly ? -(0.5f + 0.5f * wave) : wave) * piece.amplitude.y;
            float tilt = Mathf.Sin(turn * cycle * 0.59f + piece.phase * 3.0f) * piece.rotation;
            piece.rect.anchoredPosition = new Vector2(x, y) * _intensity;
            piece.rect.localRotation = Quaternion.Euler(0.0f, 0.0f, tilt * _intensity);
        }
    }
}
