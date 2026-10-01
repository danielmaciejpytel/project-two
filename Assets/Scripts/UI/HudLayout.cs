using UnityEngine;

// Positions of the gameplay HUD in the 1920x1080 layout of the approved mockup. Coordinates have their origin in the top left corner
// and y grows downwards; the HUD groups (ScreenCorner) have their origin in the middle of the layout, so Place converts between the two.
public static class HudLayout
{
    public const float Margin = 24.0f;

    // The command column of the player who has the turn: the main button and Call, the ability button, the battle log.
    public const float ColumnWidth = 384.0f;
    public const float LeftColumnX = Margin;
    public const float RightColumnX = ScreenFit.ReferenceWidth - Margin - ColumnWidth;
    public const float ButtonsY = 136.0f;
    public const float ButtonHeight = 64.0f;
    public const float ButtonGap = 12.0f;
    public const float PrimaryButtonWidth = 244.0f;
    public const float CallButtonWidth = ColumnWidth - PrimaryButtonWidth - ButtonGap;
    public const float RowStep = ButtonHeight + ButtonGap;
    public const float LogY = ButtonsY + RowStep;
    public const float LogHeight = 216.0f;

    // The details panel (bottom left corner) has a slanted right edge, from x 517 at its top to x 872 at its bottom (872 x 208).
    // Where that edge is, inside the panel, at this height from its top: text must end before it.
    public const float InfoPanelWidth = 872.0f;
    public const float InfoPanelHeight = 208.0f;
    public const float InfoPanelSlantTop = 517.0f;
    public static float InfoPanelSlantX(float y) => InfoPanelSlantTop + y * (InfoPanelWidth - InfoPanelSlantTop) / InfoPanelHeight;

    public static float ColumnX(int playerId) => playerId == 1 ? LeftColumnX : RightColumnX;

    // Puts the rect at layout coordinates: its top left corner and its size. The parent's origin is the middle of the layout.
    public static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(x + width * 0.5f - ScreenFit.ReferenceWidth * 0.5f, ScreenFit.ReferenceHeight * 0.5f - y - height * 0.5f);
    }

    // Puts a child at coordinates inside its parent (top left corner of the parent, y downwards).
    public static void PlaceIn(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.0f, 1.0f);
        rect.pivot = new Vector2(0.0f, 1.0f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(x, -y);
    }
}
