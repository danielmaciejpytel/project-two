using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One of the five places of a team on the unit choice screen: the Superior and four doppelgangers.
public class ChoiceSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private const float PortraitWidth = 117.5f;
    private const float MeleePortraitWidth = 129.6f;
    private const float PortraitHeight = 193.5f;
    private const float MeleePortraitHeight = 207.3f;
    private const float SuperiorPortraitHeight = 212.5f;
    private const float GhostPortraitHeight = 198.7f;
    private const float PortraitBottom = 34.5f;
    private const float MeleePortraitBottom = 55.2f;
    private const float SpriteSize = 2000.0f;

    private static readonly Color FilledFill = new Color(1.0f, 1.0f, 1.0f, 0.16f);
    private static readonly Color EmptyLine = new Color(1.0f, 1.0f, 1.0f, 0.14f);
    private static readonly Color GhostColor = new Color(0.0f, 0.0f, 0.0f, 0.22f);
    private static readonly Color EmptyNameColor = new Color(1.0f, 1.0f, 1.0f, 0.22f);
    private static readonly Color NameColor = new Color(0.957f, 0.957f, 0.957f);
    private static readonly Color SuperiorBase = new Color(0.165f, 0.165f, 0.165f);

    // Where the figure is inside each 2000 x 2000 portrait: left, top, right, bottom (pixels).
    private static readonly (string prefix, int left, int top, int right, int bottom)[] FigureBounds =
    {
        ("Ability", 734, 373, 1334, 1654),
        ("Assasin", 578, 366, 1239, 1654),
        ("Commander", 713, 319, 1282, 1665),
        ("Low", 713, 436, 1137, 1667),
        ("Melee", 581, 260, 1211, 1660),
        ("Tank", 710, 397, 1337, 1654),
    };

    [SerializeField] private Image _fill;
    [SerializeField] private Image _frame;
    [SerializeField] private Image _portrait;
    [SerializeField] private TMP_Text _name;
    [SerializeField] private GameObject _superiorBar;
    [SerializeField] private ChoiceButton _leftArrow;
    [SerializeField] private ChoiceButton _rightArrow;
    [SerializeField] private bool _isLast;
    [SerializeField] private Sprite _fillLast;
    [SerializeField] private Sprite _frameSolid;
    [SerializeField] private Sprite _frameDashed;
    [SerializeField] private Sprite _frameLastSolid;
    [SerializeField] private Sprite _frameLastDashed;

    private bool _hasUnit;

    public int Index { get; set; }

    public event Action<ChoiceSlotView, bool> HoverChanged;
    public event Action<int> ArrowPressed;

    private void Awake()
    {
        _leftArrow.Clicked += () => ArrowPressed?.Invoke(-1);
        _rightArrow.Clicked += () => ArrowPressed?.Invoke(1);
        _leftArrow.SetPulsing(true);
        _rightArrow.SetPulsing(true);
        if (_isLast) _fill.sprite = _fillLast;
    }

    // unit == null shows the empty place with the dark shape of ghost. framed: outline in the team colour
    // (the place being chosen). arrows: the < > buttons of the picking team.
    public void Show(UnitController unit, Sprite ghost, bool superior, bool framed, bool arrows, Color team)
    {
        _hasUnit = unit != null;
        _superiorBar.SetActive(superior);
        _leftArrow.gameObject.SetActive(arrows);
        _rightArrow.gameObject.SetActive(arrows);
        if (unit != null)
        {
            _name.text = unit.GetUnitName();
            _name.color = NameColor;
            _fill.enabled = true;
            _fill.color = superior ? Color.Lerp(SuperiorBase, team, 0.18f) : FilledFill;
            _frame.sprite = _isLast ? _frameLastSolid : _frameSolid;
            _frame.color = superior || framed ? team : Color.clear;
            SetPortrait(unit.GetUnitPortrait(), Color.white, superior ? SuperiorPortraitHeight : 0.0f);
        }
        else
        {
            _name.text = "?";
            _name.color = EmptyNameColor;
            _fill.enabled = false;
            _frame.sprite = _isLast ? _frameLastDashed : _frameDashed;
            _frame.color = EmptyLine;
            SetPortrait(ghost, GhostColor, GhostPortraitHeight);
        }
    }

    // Puts the figure of the portrait into a box of the given height (0 = the height of its kind), standing above the name.
    private void SetPortrait(Sprite sprite, Color color, float boxHeight)
    {
        _portrait.sprite = sprite;
        _portrait.color = color;
        _portrait.enabled = sprite != null;
        if (sprite == null) return;
        var bounds = FigureBounds[0];
        foreach (var candidate in FigureBounds)
        {
            if (sprite.name.StartsWith(candidate.prefix, StringComparison.Ordinal)) { bounds = candidate; break; }
        }
        bool melee = sprite.name.StartsWith("Melee", StringComparison.Ordinal);
        float boxWidth = melee ? MeleePortraitWidth : PortraitWidth;
        if (boxHeight <= 0.0f) boxHeight = melee ? MeleePortraitHeight : PortraitHeight;
        float bottom = melee && boxHeight == MeleePortraitHeight ? MeleePortraitBottom : PortraitBottom;
        float figureWidth = bounds.right - bounds.left;
        float figureHeight = bounds.bottom - bounds.top;
        float scale = Mathf.Min(boxWidth / figureWidth, boxHeight / figureHeight);
        RectTransform rect = _portrait.rectTransform;
        rect.sizeDelta = new Vector2(SpriteSize * scale, SpriteSize * scale);
        float centerX = (bounds.left + bounds.right) * 0.5f;
        rect.anchoredPosition = new Vector2(-(centerX - SpriteSize * 0.5f) * scale, bottom - (SpriteSize - bounds.bottom) * scale);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_hasUnit) HoverChanged?.Invoke(this, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HoverChanged?.Invoke(this, false);
    }
}
