using UnityEngine;

// Hides other parts of the screen while this object is active, without switching them off: what lies under the unit choice screen.
// Needed since the choice screen no longer has an opaque picture that covers everything behind it, so the animated world background
// can show through. The UI target is a CanvasGroup (hides only UI graphics, not the background sprites); the renderers are world
// sprites of the board (its shadow and the dividing line), which are only disabled, so their bounds stay valid.
public class HideWhileActive : MonoBehaviour
{
    [SerializeField] private CanvasGroup _target;
    [SerializeField] private Renderer[] _renderers = new Renderer[0];

    private void OnEnable() => SetHidden(true);

    private void OnDisable() => SetHidden(false);

    private void SetHidden(bool hidden)
    {
        if (_target != null)
        {
            _target.alpha = hidden ? 0.0f : 1.0f;
            _target.blocksRaycasts = !hidden;
        }
        foreach (Renderer renderer in _renderers)
            if (renderer != null) renderer.enabled = !hidden;
    }
}
