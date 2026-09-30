using NUnit.Framework;
using UnityEngine;

// The shapes of the screen the game is fitted to: the 16:9 layout stays as it is, backgrounds and the camera adapt.
public class ScreenFitTests
{
    private const float Wide = 2560.0f / 1080.0f;
    private const float Tall = 1920.0f / 1200.0f;
    private const float Square = 4.0f / 3.0f;

    [Test]
    public void SixteenByNineNeedsNoChange()
    {
        Assert.AreEqual(1.0f, ScreenFit.CoverFactor(ScreenFit.ReferenceAspect), 0.0001f);
        Assert.AreEqual(5.0f, ScreenFit.CameraSize(5.0f, ScreenFit.ReferenceAspect), 0.0001f);
    }

    [Test]
    public void AWiderScreenEnlargesTheBackgroundByTheWidthAndKeepsTheCameraHeight()
    {
        Assert.AreEqual(Wide / ScreenFit.ReferenceAspect, ScreenFit.CoverFactor(Wide), 0.0001f);
        Assert.AreEqual(5.0f, ScreenFit.CameraSize(5.0f, Wide), 0.0001f);
    }

    [Test]
    public void ANarrowerScreenEnlargesTheBackgroundByTheHeightAndWidensTheCamera()
    {
        Assert.AreEqual(ScreenFit.ReferenceAspect / Tall, ScreenFit.CoverFactor(Tall), 0.0001f);
        Assert.AreEqual(5.0f * ScreenFit.ReferenceAspect / Tall, ScreenFit.CameraSize(5.0f, Tall), 0.0001f);
        Assert.AreEqual(5.0f * ScreenFit.ReferenceAspect / Square, ScreenFit.CameraSize(5.0f, Square), 0.0001f);
    }

    [Test]
    public void TheBackgroundAlwaysCoversTheScreenAndNeverShrinks()
    {
        foreach (float aspect in new[] { 1.0f, Square, Tall, ScreenFit.ReferenceAspect, Wide, 32.0f / 9.0f })
        {
            float factor = ScreenFit.CoverFactor(aspect);
            Assert.GreaterOrEqual(factor, 1.0f, "Aspect " + aspect);
            // A screen of this shape is "aspect" wide and 1 high. The 16:9 frame fitted into it, enlarged by the factor,
            // has to reach both dimensions of the screen.
            float frameWidth = aspect >= ScreenFit.ReferenceAspect ? ScreenFit.ReferenceAspect : aspect;
            float frameHeight = aspect >= ScreenFit.ReferenceAspect ? 1.0f : aspect / ScreenFit.ReferenceAspect;
            Assert.GreaterOrEqual(frameWidth * factor + 0.0001f, aspect, "Width, aspect " + aspect);
            Assert.GreaterOrEqual(frameHeight * factor + 0.0001f, 1.0f, "Height, aspect " + aspect);
        }
    }

    [Test]
    public void TheSpaceBeyondTheFrameIsOnTheSidesOfAWideScreenAndAboveAndBelowATallOne()
    {
        Assert.AreEqual(Vector2.zero, ScreenFit.EdgeMargin(ScreenFit.ReferenceAspect));
        Vector2 wide = ScreenFit.EdgeMargin(Wide);
        Assert.AreEqual((1080.0f * Wide - 1920.0f) * 0.5f, wide.x, 0.001f);
        Assert.AreEqual(0.0f, wide.y, 0.001f);
        Vector2 tall = ScreenFit.EdgeMargin(Tall);
        Assert.AreEqual(0.0f, tall.x, 0.001f);
        Assert.AreEqual(60.0f, tall.y, 0.001f);
        Assert.AreEqual(180.0f, ScreenFit.EdgeMargin(Square).y, 0.001f);
    }
}

