using UnityEngine;

// A sprite in world space that sits under the UI canvas in the hierarchy. The canvas scale follows the screen size, so
// without this the sprite would shrink and drift away from the board on every screen except 1920x1080.
// It keeps the position and size it was made with, and a background that fills the screen is also centred on the
// camera and enlarged to cover a screen of any shape.
public class WorldBackdrop : MonoBehaviour
{
    [SerializeField] private bool _fillScreen;
    [Tooltip("Position in the world when the screen is 16:9. A background that fills the screen follows the camera instead.")]
    [SerializeField] private Vector3 _worldPosition;
    [Tooltip("Size in the world (scale of the sprite) when the screen is 16:9.")]
    [SerializeField] private Vector3 _worldScale = Vector3.one;

    public bool FillScreen => _fillScreen;

    private void LateUpdate() => Apply(ScreenFit.Aspect);

    public void Apply(float aspect)
    {
        float factor = _fillScreen ? ScreenFit.CoverFactor(aspect) : 1.0f;
        Vector3 position = _worldPosition;
        Camera camera = Camera.main;
        if (_fillScreen && camera != null)
        {
            position.x = camera.transform.position.x;
            position.y = camera.transform.position.y;
        }
        transform.position = position;
        Vector3 parent = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        transform.localScale = new Vector3(_worldScale.x * factor / parent.x, _worldScale.y * factor / parent.y, transform.localScale.z);
    }
}
