using System;
using UnityEngine;

// Moves the layers of the gameplay background (Assets/Sprites/Background/Layers) with gentle sine motion. Every layer has its own
// sway, rotation, scale pulse, drift and alpha pulse, and two global knobs scale all of it, so the whole background can be tuned in
// the Inspector (also while the game runs). Amounts are in pixels of the 1920x1080 artwork and angles in degrees.
public class BackgroundMotion : MonoBehaviour
{
    // What a layer is, so the switches below can hide whole kinds of layers.
    public enum LayerGroup { Always, Dust, Shadows }

    [Serializable]
    public class Layer
    {
        public string Name;
        [Tooltip("Dust = the floating dots, Shadows = the soft shadows under the shards. They can be switched off above.")]
        public LayerGroup Group;
        public Transform Target;
        [Tooltip("Second copy of a layer that scrolls (dust), kept one WrapHeight below the Target so the scroll never shows a gap.")]
        public Transform Copy;
        [Tooltip("How far the layer travels from its home position, in pixels (x and y).")]
        public Vector2 Sway;
        [Tooltip("Sway speed in cycles per second (x and y).")]
        public Vector2 SwayFrequency = new Vector2(0.1f, 0.13f);
        [Range(0.0f, 1.0f)] [Tooltip("Where in its cycle the layer starts, so the layers do not move in step.")]
        public float Phase;
        [Tooltip("Rotation around the centre of the layer, in degrees.")]
        public float Rotation;
        public float RotationFrequency = 0.07f;
        [Tooltip("Breathing: 0.01 grows and shrinks the layer by 1 percent.")]
        public float ScalePulse;
        public float ScaleFrequency = 0.09f;
        [Tooltip("Constant scrolling in pixels per second. Needs a WrapHeight and a Copy to loop.")]
        public Vector2 Drift;
        [Tooltip("Height in pixels after which a scrolling layer starts over (the height of the artwork), 0 = no wrap.")]
        public float WrapHeight;
        [Range(0.0f, 1.0f)] [Tooltip("How much the opacity breathes (0.3 = between 70 and 130 percent of the original).")]
        public float AlphaPulse;
        public float AlphaFrequency = 0.08f;
    }

    [Tooltip("Speed of all the motion. 1 = as designed, 0 = still.")]
    [SerializeField] [Min(0.0f)] private float _speed = 1.0f;
    [Tooltip("Size of all the motion (sway, rotation, scale and alpha). 1 = as designed, 0 = still.")]
    [SerializeField] [Min(0.0f)] private float _amplitude = 1.0f;
    [Tooltip("The floating dots. Off hides them (their motion keeps running, so switching back on is seamless).")]
    [SerializeField] private bool _showDust = true;
    [Tooltip("How visible the floating dots are. 1 = as drawn, 0 = invisible (the pulsing of their opacity works around this value).")]
    [SerializeField] [Range(0.0f, 1.0f)] private float _dustOpacity = 1.0f;
    [Tooltip("The soft shadows under the shards. Off hides them.")]
    [SerializeField] private bool _showShadows = true;
    [Tooltip("Pixels of the artwork per world unit of the layers (the import setting of the sprites).")]
    [SerializeField] private float _pixelsPerUnit = 100.0f;
    [SerializeField] private Layer[] _layers = Array.Empty<Layer>();

    private Vector3[] _homePosition;
    private Vector3[] _homeScale;
    private float[] _homeRotation;
    private SpriteRenderer[] _renderers;
    private float _clock;

    public float Speed { get => _speed; set => _speed = Mathf.Max(0.0f, value); }
    public float Amplitude { get => _amplitude; set => _amplitude = Mathf.Max(0.0f, value); }
    public Layer[] Layers => _layers;
    public bool ShowDust { get => _showDust; set { _showDust = value; ApplyVisibility(); } }
    public float DustOpacity { get => _dustOpacity; set { _dustOpacity = Mathf.Clamp01(value); ApplyDustOpacity(); } }
    public bool ShowShadows { get => _showShadows; set { _showShadows = value; ApplyVisibility(); } }

    private void Awake()
    {
        int count = _layers.Length;
        _homePosition = new Vector3[count];
        _homeScale = new Vector3[count];
        _homeRotation = new float[count];
        _renderers = new SpriteRenderer[count];
        for (int i = 0; i < count; i++)
        {
            Transform target = _layers[i].Target;
            if (target == null) continue;
            _homePosition[i] = target.localPosition;
            _homeScale[i] = target.localScale;
            _homeRotation[i] = target.localEulerAngles.z;
            _renderers[i] = target.GetComponent<SpriteRenderer>();
        }
        ApplyDustOpacity();
    }

    // Switching a renderer on or off sends messages (OnBecameVisible/Invisible), which Unity does not allow in Awake or OnValidate.
    private void Start() => ApplyVisibility();

    private void OnValidate()
    {
        ApplyDustOpacity();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null) ApplyVisibility();
        };
#endif
    }

    // The opacity of the dust without its pulsing, so the slider also shows in the editor. The layers are drawn at full opacity,
    // so the opacity is the whole alpha of the renderer.
    private void ApplyDustOpacity()
    {
        if (_layers == null) return;
        foreach (Layer layer in _layers)
        {
            if (layer.Group != LayerGroup.Dust) continue;
            SetAlpha(layer.Target, _dustOpacity);
            SetAlpha(layer.Copy, _dustOpacity);
        }
    }

    private static void SetAlpha(Transform target, float alpha)
    {
        if (target == null || !target.TryGetComponent(out SpriteRenderer renderer)) return;
        Color color = renderer.color;
        if (Mathf.Approximately(color.a, alpha)) return;
        color.a = alpha;
        renderer.color = color;
    }

    // Hides a layer by disabling its renderer (not the object), so it keeps moving while hidden.
    private void ApplyVisibility()
    {
        if (_layers == null) return;
        foreach (Layer layer in _layers)
        {
            bool visible = layer.Group == LayerGroup.Dust ? _showDust : layer.Group != LayerGroup.Shadows || _showShadows;
            SetVisible(layer.Target, visible);
            SetVisible(layer.Copy, visible);
        }
    }

    private static void SetVisible(Transform target, bool visible)
    {
        if (target != null && target.TryGetComponent(out SpriteRenderer renderer) && renderer.enabled != visible) renderer.enabled = visible;
    }

    private void Update()
    {
        _clock += Time.deltaTime * _speed;
        for (int i = 0; i < _layers.Length; i++) Apply(i, _layers[i]);
    }

    private void Apply(int index, Layer layer)
    {
        if (layer.Target == null) return;
        float t = _clock;
        float unit = 1.0f / _pixelsPerUnit;
        Vector3 position = _homePosition[index];
        position.x += (Wave(t, layer.SwayFrequency.x, layer.Phase) * layer.Sway.x * _amplitude + layer.Drift.x * t) * unit;
        float drift = layer.Drift.y * t;
        if (layer.WrapHeight > 0.0f) drift = Mathf.Repeat(drift, layer.WrapHeight);
        position.y += (Wave(t, layer.SwayFrequency.y, layer.Phase + 0.25f) * layer.Sway.y * _amplitude + drift) * unit;
        layer.Target.localPosition = position;
        if (layer.Copy != null)
        {
            Vector3 copy = position;
            copy.y -= layer.WrapHeight * unit * Mathf.Sign(layer.Drift.y == 0.0f ? 1.0f : layer.Drift.y);
            layer.Copy.localPosition = copy;
        }

        float angle = _homeRotation[index] + Wave(t, layer.RotationFrequency, layer.Phase + 0.5f) * layer.Rotation * _amplitude;
        layer.Target.localRotation = Quaternion.Euler(0.0f, 0.0f, angle);
        float scale = 1.0f + Wave(t, layer.ScaleFrequency, layer.Phase + 0.75f) * layer.ScalePulse * _amplitude;
        layer.Target.localScale = new Vector3(_homeScale[index].x * scale, _homeScale[index].y * scale, _homeScale[index].z);

        if (layer.AlphaPulse > 0.0f && _renderers[index] != null)
        {
            float opacity = layer.Group == LayerGroup.Dust ? _dustOpacity : 1.0f;
            float alpha = Mathf.Clamp01(opacity * (1.0f + Wave(t, layer.AlphaFrequency, layer.Phase) * layer.AlphaPulse * _amplitude));
            SetAlpha(layer.Target, alpha);
            SetAlpha(layer.Copy, alpha);
        }
    }

    private static float Wave(float time, float frequency, float phase) => Mathf.Sin((time * frequency + phase) * Mathf.PI * 2.0f);
}
