using UnityEngine;

// A full-screen UI layer (dimming, shade) made for the 16:9 frame: zoomed in about its centre so it fills a screen of any shape.
public class ScreenCover : MonoBehaviour
{
    private Vector3 _baseScale;

    private void Awake() => _baseScale = transform.localScale;

    private void LateUpdate()
    {
        float factor = ScreenFit.CoverFactor(ScreenFit.Aspect);
        transform.localScale = new Vector3(_baseScale.x * factor, _baseScale.y * factor, _baseScale.z);
    }
}
