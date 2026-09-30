using UnityEngine;

// Keeps the whole 16:9 frame of the board in view on a narrower screen (the height is kept on a wider one).
[RequireComponent(typeof(Camera))]
public class CameraAspectFit : MonoBehaviour
{
    private Camera _camera;
    private float _baseSize;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _baseSize = _camera.orthographicSize;
    }

    private void LateUpdate() => _camera.orthographicSize = ScreenFit.CameraSize(_baseSize, ScreenFit.Aspect);
}
