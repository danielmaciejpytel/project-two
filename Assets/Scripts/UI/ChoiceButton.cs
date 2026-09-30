using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Text button of the unit choice screen (BACK, NEXT, DONE and the slot arrows): turns pink on hover,
// can pulse and can be dimmed while it does not react.
[RequireComponent(typeof(CanvasGroup))]
public class ChoiceButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private const float PulseHalfPeriod = 0.8f;
    private const float PulseMinAlpha = 0.4f;
    private const float PulseMaxScale = 1.18f;
    private const float DisabledAlpha = 0.35f;

    [SerializeField] private TMP_Text _label;
    [SerializeField] private Color _normalColor = new Color(0.957f, 0.957f, 0.957f);
    [SerializeField] private Color _hoverColor = new Color(0.843f, 0.180f, 0.400f);
    [SerializeField] private bool _pulseWhileHovered;

    private CanvasGroup _group;
    private Tween _pulse;
    private bool _pulsing;
    private bool _hovered;
    private bool _interactable = true;

    public event Action Clicked;

    public bool Interactable => _interactable;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
    }

    private void OnEnable() => Refresh();

    private void OnDisable()
    {
        _pulse?.Kill();
        _pulse = null;
        _hovered = false;
    }

    public void SetText(string text) => _label.text = text;

    public void SetInteractable(bool interactable)
    {
        _interactable = interactable;
        if (!interactable) _hovered = false;
        Refresh();
    }

    public void SetPulsing(bool pulsing)
    {
        _pulsing = pulsing;
        Refresh();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_interactable) return;
        _hovered = true;
        SoundController.Instance?.PlayHover();
        Refresh();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        Refresh();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_interactable) Clicked?.Invoke();
    }

    // Colour, dimming and the pulse always follow from the flags.
    private void Refresh()
    {
        if (_group == null) _group = GetComponent<CanvasGroup>();
        _label.color = _hovered ? _hoverColor : _normalColor;
        bool pulse = _pulsing && _interactable && (!_hovered || _pulseWhileHovered);
        if (pulse)
        {
            if (_pulse == null || !_pulse.IsActive())
            {
                float phase = 0.0f;
                _pulse = DOTween.To(() => phase, value => { phase = value; ApplyPulse(value); }, 1.0f, PulseHalfPeriod)
                    .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(gameObject);
            }
            return;
        }
        _pulse?.Kill();
        _pulse = null;
        transform.localScale = Vector3.one;
        _group.alpha = _interactable ? 1.0f : DisabledAlpha;
    }

    private void ApplyPulse(float t)
    {
        _group.alpha = Mathf.Lerp(PulseMinAlpha, 1.0f, t);
        transform.localScale = Vector3.one * Mathf.Lerp(1.0f, PulseMaxScale, t);
    }
}
