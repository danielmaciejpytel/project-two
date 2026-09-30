using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// The text of a row of the menu turns pink while the mouse is over it (the row is this object: a button, a label or a slider), and
// the dark bar of the menu slides in behind the row.
public class HoverTint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text _label;
    [SerializeField] private Color _hoverColor = new Color32(0xD7, 0x2E, 0x66, 0xFF);
    [Tooltip("The menu whose bar follows the row; without it only the color changes.")]
    [SerializeField] private MainMenuController _menu;

    private Color _normalColor;
    private bool _captured;

    public Color HoverColor => _hoverColor;

    private void Capture()
    {
        if (_captured) return;
        _normalColor = _label.color;
        _captured = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Capture();
        _label.color = _hoverColor;
        if (_menu != null) _menu.ShowBarAt(_label.transform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Capture();
        _label.color = _normalColor;
        if (_menu != null) _menu.HideBarAgain();
    }

    // A panel closed while the mouse was over a row must not open with a pink row.
    private void OnDisable()
    {
        if (_captured) _label.color = _normalColor;
    }
}
