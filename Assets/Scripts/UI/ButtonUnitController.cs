using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

// One card of the team panel: the frame with the card of the unit, its name and health points below. The look follows the state of the
// unit: selected (raised, in the team color), reserve (dashed, dim), called this turn (NEW), already moved (MOVED), killed (greyed out like the reserve, with a dashed strike and the word DEAD below).
public class ButtonUnitController : MonoBehaviour
{
    [SerializeField] private Image _killedImage;
    [SerializeField] private TMP_Text _unitText;
    [Header("Card")]
    [Tooltip("The frame with everything that is raised when the card is selected.")]
    [SerializeField] private RectTransform _frame;
    [SerializeField] private Image _frameImage;
    [SerializeField] private Image _cardImage;
    [SerializeField] private Image _badgeImage;
    [SerializeField] private TMP_Text _badgeText;
    [Tooltip("Row of the health points under the name.")]
    [SerializeField] private RectTransform _pipRow;
    [Header("Sprites")]
    [SerializeField] private Sprite _frameSprite;
    [SerializeField] private Sprite _reserveSprite;
    [SerializeField] private Sprite _selectedRedSprite;
    [SerializeField] private Sprite _selectedBlueSprite;
    [SerializeField] private Sprite _badgeNewSprite;
    [SerializeField] private Sprite _badgeMovedSprite;
    [SerializeField] private Sprite _pipRedSprite;
    [SerializeField] private Sprite _pipBlueSprite;
    [SerializeField] private Sprite _pipLostSprite;
    [Header("Look")]
    [SerializeField] private Color _hotColor = new Color32(0xFF, 0x1B, 0x47, 0xFF);
    [SerializeField] private Color _coldColor = new Color32(0x14, 0xC8, 0xD8, 0xFF);
    [SerializeField] private Color _mutedColor = new Color32(0xA3, 0xA3, 0xA3, 0xFF);
    [Tooltip("How far a selected card is raised.")]
    [SerializeField] private float _raise = 6.0f;
    [SerializeField] private float _movedAlpha = 0.55f;
    [SerializeField] private float _reserveAlpha = 0.3f;
    [Tooltip("In the Call mode the cards that can be called pulse between dim and bright, one pulse takes this long.")]
    [SerializeField] private float _pulseSeconds = 1.1f;
    [SerializeField] private float _pipSize = 5.0f;
    [SerializeField] private float _pipGap = 2.0f;
    private UnitController _myUnit;
    private bool _selected;
    private bool _callMode;
    private bool _calledThisTurn;
    private readonly List<Image> _pips = new List<Image>();

    void Awake()
    {
        _myUnit = null;
        _killedImage.enabled = false;
        if (_badgeImage != null) _badgeImage.gameObject.SetActive(false);
    }

    private void Update()
    {
        // The cards that can be called, until one is chosen.
        if (!_callMode || _myUnit == null || _myUnit.IsDeployed || _myUnit.IsKilled || _selected) return;
        float wave = 0.5f - 0.5f * Mathf.Cos(Time.time * 2.0f * Mathf.PI / _pulseSeconds);
        float tone = Mathf.Lerp(0.6f, 1.0f, wave);
        _cardImage.color = new Color(tone, tone, tone, Mathf.Lerp(_reserveAlpha, 1.0f, wave));
    }

    private void Start()
    {
        EventManager events = EventManager.Instance;
        if (events == null) return;
        events.OnUnitDeployed += OnUnitChanged;
        events.OnUnitDamaged += OnUnitHealthChanged;
        events.OnUnitHealed += OnUnitHealthChanged;
    }

    private void OnDestroy()
    {
        EventManager events = EventManager.Instance;
        if (events == null) return;
        events.OnUnitDeployed -= OnUnitChanged;
        events.OnUnitDamaged -= OnUnitHealthChanged;
        events.OnUnitHealed -= OnUnitHealthChanged;
    }

    private void OnUnitChanged(UnitController unit)
    {
        if (unit != _myUnit) return;
        _calledThisTurn = true;
        Refresh();
    }

    private void OnUnitHealthChanged(UnitController unit, int amount, string source)
    {
        if (unit == _myUnit) Refresh();
    }

    public void ClickedMe()
    {
        if(!_myUnit.IsKilled) EventManager.Instance.UnitClicked(_myUnit);
    }

    public void SetUnit(UnitController unit)
    {
        _myUnit = unit;
        _calledThisTurn = false;
        _selected = false;
        Refresh();
    }

    // The card of the chosen unit is raised; the others are not (null: nobody is chosen). A killed unit's card keeps its size.
    public void EnlargeUnit(UnitController unit)
    {
        _selected = unit != null && unit == _myUnit;
        Refresh();
        if (_frame != null)
        {
            _frame.DOKill();
            _frame.DOAnchorPosY(_selected && !_myUnit.IsKilled ? _raise : 0.0f, 0.15f).SetUpdate(true).SetLink(_frame.gameObject);
        }
    }

    public void UnitKilled(UnitController unit)
    {
        if (unit == _myUnit) Refresh();
    }

    // The Call mode: the reserve can be chosen (its cards say "Call").
    public void MarkForDeployment()
    {
        _callMode = true;
        Refresh();
    }

    public void MarkForAction()
    {
        _callMode = false;
        Refresh();
    }

    public void DisableUnit(UnitController unit)
    {
        if (unit == _myUnit) Refresh();
    }

    public bool IsUnitDeployed()
    {
        return _myUnit.IsDeployed;
    }

    public bool IsInPlay() => _myUnit != null && _myUnit.IsDeployed && !_myUnit.IsKilled;

    private void Refresh()
    {
        if (_myUnit == null) return;
        UnitController unit = _myUnit;
        bool red = unit.GetPlayerId() == 1;
        Color team = red ? _hotColor : _coldColor;
        bool killed = unit.IsKilled;
        bool reserve = !unit.IsDeployed && !killed;
        bool moved = unit.IsDeployed && !killed && !unit.IsAvailable && !_calledThisTurn;
        bool called = unit.IsDeployed && !killed && _calledThisTurn;
        bool chosen = _selected && !killed;

        _frameImage.sprite = chosen ? (red ? _selectedRedSprite : _selectedBlueSprite) : reserve ? _reserveSprite : _frameSprite;
        _cardImage.sprite = unit.GetUnitCard();
        // A killed unit is greyed out like the reserve (and keeps its size); the strike over its card is the killed image.
        float alpha = killed || reserve && !chosen ? _reserveAlpha : moved ? _movedAlpha : 1.0f;
        _cardImage.color = new Color(1.0f, 1.0f, 1.0f, alpha);
        _killedImage.enabled = killed;

        // Below the card the reserve says RESERVE (or CALL) and a killed unit says DEAD, both in the muted color.
        _unitText.text = reserve ? Loc.T(_callMode ? "CALL" : "RESERVE") : killed ? Loc.T("DEAD") : unit.GetShortUnitName();
        _unitText.color = chosen ? team : reserve || killed ? _mutedColor : Color.white;

        if (_badgeImage != null)
        {
            _badgeImage.gameObject.SetActive(called || moved);
            _badgeImage.sprite = called ? _badgeNewSprite : _badgeMovedSprite;
            _badgeText.text = Loc.T(called ? "NEW" : "MOVED");
        }
        ShowHealth(unit, red, reserve || killed);
    }

    // One small square per health point, in the team color for what is left.
    private void ShowHealth(UnitController unit, bool red, bool hidden)
    {
        if (_pipRow == null) return;
        int max = hidden ? 0 : unit.GetMaxHP();
        int hp = hidden ? 0 : unit.GetHP();
        while (_pips.Count < max)
        {
            GameObject pip = new GameObject("HealthPoint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pip.transform.SetParent(_pipRow, false);
            Image image = pip.GetComponent<Image>();
            image.raycastTarget = false;
            RectTransform rect = (RectTransform)pip.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(_pipSize, _pipSize);
            _pips.Add(image);
        }
        float width = max * _pipSize + Mathf.Max(0, max - 1) * _pipGap;
        for (int i = 0; i < _pips.Count; i++)
        {
            Image pip = _pips[i];
            pip.gameObject.SetActive(i < max);
            if (i >= max) continue;
            pip.sprite = i < hp ? (red ? _pipRedSprite : _pipBlueSprite) : _pipLostSprite;
            ((RectTransform)pip.transform).anchoredPosition = new Vector2(-width * 0.5f + _pipSize * 0.5f + i * (_pipSize + _pipGap), 0.0f);
        }
    }
}
