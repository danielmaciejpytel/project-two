using UnityEngine;

// The game is laid out for 16:9 (1920x1080). On other screens the layout and the board keep their look and size, the whole
// 16:9 frame is fitted into the screen, and only the backgrounds are enlarged so they fill the rest of the screen.
public static class ScreenFit
{
    public const float ReferenceAspect = 1920.0f / 1080.0f;

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
}
