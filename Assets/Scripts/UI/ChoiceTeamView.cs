using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A team's dark panel on the unit choice screen: heading, five places and the information about a unit.
public class ChoiceTeamView : MonoBehaviour
{
    private const float PanelIdleAlpha = 0.65f;
    private const float ContentIdleAlpha = 0.75f;

    [SerializeField] private Image _panel;
    [SerializeField] private CanvasGroup _content;
    [SerializeField] private TMP_Text _heading;
    [SerializeField] private TMP_Text _player;
    [SerializeField] private ChoiceSlotView[] _slots;
    [SerializeField] private ChoiceInfoView _info;

    public ChoiceSlotView[] Slots => _slots;
    public ChoiceInfoView Info => _info;

    public void SetTexts(string heading, string player)
    {
        _heading.text = heading;
        _player.text = player;
        AlignPlayerToHeading();
    }

    // The "Player n" text stands on the same baseline as the heading, a little to the right of it.
    private void AlignPlayerToHeading()
    {
        _heading.ForceMeshUpdate();
        _player.ForceMeshUpdate();
        if (_heading.textInfo.lineCount == 0 || _player.textInfo.lineCount == 0) return;
        RectTransform headingRect = _heading.rectTransform;
        RectTransform playerRect = _player.rectTransform;
        float headingBaseline = headingRect.anchoredPosition.y + _heading.textInfo.lineInfo[0].baseline;
        float playerBaseline = _player.textInfo.lineInfo[0].baseline;
        playerRect.anchoredPosition = new Vector2(headingRect.anchoredPosition.x + _heading.preferredWidth + 14.0f, headingBaseline - playerBaseline);
    }

    // The team that is not choosing is dimmed.
    public void SetIdle(bool idle)
    {
        Color color = _panel.color;
        color.a = idle ? PanelIdleAlpha : 1.0f;
        _panel.color = color;
        _content.alpha = idle ? ContentIdleAlpha : 1.0f;
    }
}
