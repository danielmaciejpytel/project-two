using UnityEngine;

// Sticks everything under it to a side or a corner of the screen. The layout is 16:9; on a screen of another shape the frame is
// fitted whole and the screen reaches beyond it, so this moves the group out by that space, to the edge of the screen.
public class ScreenCorner : MonoBehaviour
{
    [Tooltip("-1 sticks to the left edge, 1 to the right edge, 0 stays in the middle.")]
    [SerializeField, Range(-1, 1)] private int _horizontal;
    [Tooltip("1 sticks to the top edge, -1 to the bottom edge, 0 stays in the middle.")]
    [SerializeField, Range(-1, 1)] private int _vertical;

    private void LateUpdate() => Apply(ScreenFit.Aspect);

    public Vector2 OffsetFor(float aspect)
    {
        Vector2 margin = ScreenFit.EdgeMargin(aspect);
        return new Vector2(_horizontal * margin.x, _vertical * margin.y);
    }

    public void Apply(float aspect)
    {
        Vector2 offset = OffsetFor(aspect);
        Vector3 position = transform.localPosition;
        position.x = offset.x;
        position.y = offset.y;
        transform.localPosition = position;
    }
}
