using UnityEngine;

// Places the whole board in the scene: the tiles with their effects, the units, the shadow and the middle line of the board.
// Put it on the "GridPosition" object. Scale makes everything bigger or smaller at once (around the middle of the board),
// Offset moves it (X to the left or right, Y up or down). The background, the dimming and the HUD stay where they are.
// It works in the editor and while playing, so the values can be tried out in the Inspector with the game running.
[ExecuteAlways, DisallowMultipleComponent]
public class GridPlacement : MonoBehaviour
{
    // The middle of the board art as it was made, in world units; Scale grows and shrinks the board around this point.
    private static readonly Vector2 DefaultPivot = new Vector2(0.0f, -3.365f);

    [Tooltip("Size of the whole board: 1 is the original size, 0.8 is smaller, 1.2 is larger.")]
    [SerializeField, Min(0.05f)] private float _scale = 1.0f;
    [Tooltip("Moves the whole board in world units. X: negative to the left, positive to the right. Y: negative down, positive up.")]
    [SerializeField] private Vector2 _offset;
    [Tooltip("The point of the board that stays in place when the scale changes. It is set to the middle of the board when a game starts.")]
    [SerializeField] private Vector2 _pivot = DefaultPivot;

    private Vector3 _appliedPosition = new Vector3(float.NaN, 0.0f, 0.0f);
    private float _appliedScale = float.NaN;

    public static GridPlacement Instance { get; private set; }

    // The parent of everything that belongs to the board; null when the scene has no GridPosition object.
    public static Transform BoardParent => Instance != null ? Instance.transform : null;

    // The current size of the board; lengths given in board units (like the sprite shift of a unit) are multiplied by it.
    public static float Scale => Instance != null ? Instance.transform.lossyScale.x : 1.0f;

    public float BoardScale
    {
        get => _scale;
        set { _scale = Mathf.Max(0.05f, value); Apply(); }
    }

    public Vector2 Offset
    {
        get => _offset;
        set { _offset = value; Apply(); }
    }

    public void SetPivot(Vector2 pivot)
    {
        _pivot = pivot;
        Apply();
    }

    private void OnEnable()
    {
        Instance = this;
        Apply();
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void OnValidate()
    {
        _scale = Mathf.Max(0.05f, _scale);
        Apply();
    }

    private void Update() => Apply();

    // Scaling about the pivot and then moving by the offset is the same as placing the object at pivot * (1 - scale) + offset.
    public void Apply()
    {
        Vector3 position = new Vector3(_offset.x + _pivot.x * (1.0f - _scale), _offset.y + _pivot.y * (1.0f - _scale), 0.0f);
        if (position == _appliedPosition && Mathf.Approximately(_scale, _appliedScale)) return;
        _appliedPosition = position;
        _appliedScale = _scale;
        transform.SetPositionAndRotation(position, Quaternion.identity);
        transform.localScale = new Vector3(_scale, _scale, 1.0f);
        // Clicks are found with physics, which must know the colliders have moved.
        if (Application.isPlaying) Physics2D.SyncTransforms();
    }
}
