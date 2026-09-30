using UnityEngine;

// The game is laid out for 16:9 (1920x1080). On other screens the layout and the board keep their look and size, the whole
// 16:9 frame is fitted into the screen, and only the backgrounds are enlarged so they fill the rest of the screen.
public static class ScreenFit
{
    public const float ReferenceWidth = 1920.0f;
    public const float ReferenceHeight = 1080.0f;
    public const float ReferenceAspect = ReferenceWidth / ReferenceHeight;

    // Tests set it to try other screen shapes; 0 means the real screen.
    public static float AspectOverride { get; set; }

    public static float Aspect
    {
        get
        {
            if (AspectOverride > 0.0f) return AspectOverride;
            return Screen.height > 0 ? (float)Screen.width / Screen.height : ReferenceAspect;
        }
    }

    // How much a background made for the 16:9 frame has to grow to fill a screen of this shape (zooming in, keeping its proportions).
    public static float CoverFactor(float aspect) => aspect >= ReferenceAspect ? aspect / ReferenceAspect : ReferenceAspect / aspect;

    // Size of the orthographic camera that shows the whole 16:9 frame: a narrower screen needs a larger size, a wider one keeps the height.
    public static float CameraSize(float baseSize, float aspect) => aspect >= ReferenceAspect ? baseSize : baseSize * ReferenceAspect / aspect;

    // Size of the canvas, in the units of the layout (1920x1080 on a 16:9 screen), once the 16:9 frame is fitted whole into a screen of this shape.
    public static Vector2 CanvasUnits(float aspect) => aspect >= ReferenceAspect
        ? new Vector2(ReferenceHeight * aspect, ReferenceHeight)
        : new Vector2(ReferenceWidth, ReferenceWidth / aspect);

    // How far the edges of the screen are beyond the 16:9 frame on each side (only one of the two is above zero), in the same units.
    public static Vector2 EdgeMargin(float aspect)
    {
        Vector2 canvas = CanvasUnits(aspect);
        return new Vector2((canvas.x - ReferenceWidth) * 0.5f, (canvas.y - ReferenceHeight) * 0.5f);
    }
}
