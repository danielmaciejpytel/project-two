using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The damage an attack would do to the enemy under the mouse: next to him, his name and health, the damage, and a note when his cover
// matters (a piercing attack ignores it). Shown while the chosen unit can attack that enemy.
public class DamageCalloutController : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _healthText;
    [SerializeField] private TMP_Text _damageText;
    [SerializeField] private GameObject _note;
    [SerializeField] private TMP_Text _noteText;
    [SerializeField] private Image _noteIcon;
    [SerializeField] private Sprite _piercingIcon;
    [SerializeField] private Sprite _coverIcon;
    [SerializeField] private Color _hotColor = new Color32(0xFF, 0x1B, 0x47, 0xFF);
    [SerializeField] private Color _coldColor = new Color32(0x14, 0xC8, 0xD8, 0xFF);
    [SerializeField] private Color _accentColor = new Color32(0xD7, 0x2E, 0x66, 0xFF);
    [Tooltip("Where the callout sits relative to the feet of the enemy (layout units, the callout's top left corner).")]
    [SerializeField] private Vector2 _offset = new Vector2(64.0f, -186.0f);
    [SerializeField] private float _fadeTime = 0.12f;

    private const float Width = 340.0f;
    private const float Height = 126.0f;
    private CanvasGroup _group;
    private RectTransform _rect;

    private void Awake()
    {
        _rect = (RectTransform)transform;
        _group = GetComponent<CanvasGroup>();
        _group.alpha = 0.0f;
        gameObject.SetActive(false);
    }

    public void Show(UnitController attacker, UnitController target, int attackPower)
    {
        int damage = target.CalculateDamage(attackPower);
        int armor = target.GetArmor();
        bool piercing = attacker.GetComponent<SkillPiercing>() != null;
        _nameText.text = target.GetUnitName();
        _nameText.color = target.GetPlayerId() == 1 ? _hotColor : _coldColor;
        _healthText.text = target.GetHP() + "/" + target.GetMaxHP();
        _damageText.text = "-" + damage + " <color=#" + ColorUtility.ToHtmlStringRGB(_accentColor) + ">HP</color>";
        bool note = armor > 0;
        _note.SetActive(note);
        if (note)
        {
            _noteText.text = piercing ? Loc.T("PIERCING: NO COVER") : Loc.F("COVER: -{0}", armor);
            _noteIcon.sprite = piercing ? _piercingIcon : _coverIcon;
        }
        Place(target);
        gameObject.SetActive(true);
        _group.DOKill();
        _group.DOFade(1.0f, _fadeTime).SetUpdate(true).SetLink(gameObject);
    }

    public void Hide()
    {
        if (!gameObject.activeSelf) return;
        _group.DOKill();
        gameObject.SetActive(false);
        _group.alpha = 0.0f;
    }

    // Next to the feet of the enemy, on the side with more room, inside the layout.
    private void Place(UnitController target)
    {
        Canvas canvas = _rect.GetComponentInParent<Canvas>().rootCanvas;
        Vector2 screen = Camera.main.WorldToScreenPoint(target.transform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, screen, null, out Vector2 local);
        float x = local.x + ScreenFit.ReferenceWidth * 0.5f;
        float y = ScreenFit.ReferenceHeight * 0.5f - local.y;
        float left = x + _offset.x;
        if (left + Width > ScreenFit.ReferenceWidth - HudLayout.Margin) left = x - _offset.x - Width;
        float top = Mathf.Clamp(y + _offset.y, HudLayout.Margin, ScreenFit.ReferenceHeight - HudLayout.Margin - Height);
        left = Mathf.Clamp(left, HudLayout.Margin, ScreenFit.ReferenceWidth - HudLayout.Margin - Width);
        HudLayout.Place(_rect, left, top, Width, Height);
    }
}
