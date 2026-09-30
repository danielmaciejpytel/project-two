using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GameTestUtil;

// On a screen that is not 16:9 the layout and the board keep their look, the camera still shows the whole 16:9 frame,
// and the backgrounds grow to fill the screen (the screen shape is simulated, the real screen stays as it is).
public class ScreenFitPlayTests
{
    private const float Wide = 2560.0f / 1080.0f;
    private const float Tall = 1920.0f / 1200.0f;
    private const float Square = 4.0f / 3.0f;

    [SetUp]
    public void SetUp() => Reset();

    [TearDown]
    public void TearDown()
    {
        ScreenFit.AspectOverride = 0.0f;
        Reset();
    }

    private static IEnumerator ShowAs(float aspect)
    {
        ScreenFit.AspectOverride = aspect;
        yield return null;
        yield return null;
    }

    private static Rect View(Camera camera)
    {
        float halfHeight = camera.orthographicSize;
        float halfWidth = halfHeight * ScreenFit.Aspect;
        Vector3 centre = camera.transform.position;
        return new Rect(centre.x - halfWidth, centre.y - halfHeight, halfWidth * 2.0f, halfHeight * 2.0f);
    }

    private static void AssertCovers(Bounds bounds, Rect view, string what)
    {
        const float tolerance = 0.01f;
        Assert.LessOrEqual(bounds.min.x, view.xMin + tolerance, what + " covers the left edge");
        Assert.GreaterOrEqual(bounds.max.x, view.xMax - tolerance, what + " covers the right edge");
        Assert.LessOrEqual(bounds.min.y, view.yMin + tolerance, what + " covers the bottom edge");
        Assert.GreaterOrEqual(bounds.max.y, view.yMax - tolerance, what + " covers the top edge");
    }

    [UnityTest]
    public IEnumerator TheCameraKeepsTheWholeSixteenByNineFrameInView()
    {
        yield return StartGame();
        Camera camera = Camera.main;
        float baseSize = camera.orthographicSize;

        foreach (float aspect in new[] { ScreenFit.ReferenceAspect, Wide, Tall, Square })
        {
            yield return ShowAs(aspect);
            // The frame is 16:9 with the height of the base size; it has to fit whole into what the camera sees.
            Rect view = View(camera);
            Assert.GreaterOrEqual(view.height + 0.001f, baseSize * 2.0f, "Aspect " + aspect + ": the height of the frame fits");
            Assert.GreaterOrEqual(view.width + 0.001f, baseSize * 2.0f * ScreenFit.ReferenceAspect, "Aspect " + aspect + ": the width of the frame fits");
        }
    }

    [UnityTest]
    public IEnumerator TheBackgroundsFillTheScreenOfAnyShape()
    {
        yield return StartGame();
        Camera camera = Camera.main;

        foreach (float aspect in new[] { ScreenFit.ReferenceAspect, Wide, Tall, Square })
        {
            yield return ShowAs(aspect);
            Rect view = View(camera);
            int covering = 0;
            foreach (WorldBackdrop backdrop in Resources.FindObjectsOfTypeAll<WorldBackdrop>())
            {
                if (!backdrop.FillScreen || !backdrop.gameObject.activeInHierarchy) continue;
                AssertCovers(backdrop.GetComponent<SpriteRenderer>().bounds, view, backdrop.name + " at aspect " + aspect);
                covering++;
            }
            Assert.GreaterOrEqual(covering, 2, "The background art and the dimming");
        }
    }

    [UnityTest]
    public IEnumerator TheShadowOfTheBoardStaysWhereItIsWhateverTheScreen()
    {
        yield return StartGame();
        Bounds before = default;
        foreach (WorldBackdrop backdrop in Resources.FindObjectsOfTypeAll<WorldBackdrop>())
        {
            if (backdrop.FillScreen || backdrop.name != "MapShadow") continue;
            yield return ShowAs(ScreenFit.ReferenceAspect);
            before = backdrop.GetComponent<SpriteRenderer>().bounds;
            foreach (float aspect in new[] { Wide, Tall, Square })
            {
                yield return ShowAs(aspect);
                Bounds now = backdrop.GetComponent<SpriteRenderer>().bounds;
                Assert.AreEqual(before.center.x, now.center.x, 0.01f, "Aspect " + aspect);
                Assert.AreEqual(before.center.y, now.center.y, 0.01f, "Aspect " + aspect);
                Assert.AreEqual(before.size.x, now.size.x, 0.01f, "Aspect " + aspect);
            }
            yield break;
        }
        Assert.Fail("The shadow of the board is not a WorldBackdrop");
    }

    private static void AssertOffset(string corner, float x, float y, float aspect)
    {
        Vector3 position = Find<ScreenCorner>(corner).transform.localPosition;
        Assert.AreEqual(x, position.x, 0.01f, corner + " horizontally, aspect " + aspect);
        Assert.AreEqual(y, position.y, 0.01f, corner + " vertically, aspect " + aspect);
    }

    [UnityTest]
    public IEnumerator TheHudSticksToTheCornersAndEdgesOfTheScreen()
    {
        yield return StartGame();

        foreach (float aspect in new[] { ScreenFit.ReferenceAspect, Wide, Tall, Square })
        {
            yield return ShowAs(aspect);
            Vector2 margin = ScreenFit.EdgeMargin(aspect);
            AssertOffset("CornerTopLeft", -margin.x, margin.y, aspect);
            AssertOffset("CornerTopRight", margin.x, margin.y, aspect);
            AssertOffset("EdgeTop", 0.0f, margin.y, aspect);
            AssertOffset("CornerBottomLeft", -margin.x, -margin.y, aspect);
            AssertOffset("CornerBottomRight", margin.x, -margin.y, aspect);
        }
    }

    [UnityTest]
    public IEnumerator TheControlsOfThePlayerWithTheTurnStickToHisCornerAndFollowTheTurn()
    {
        yield return StartGame();
        ScreenFit.AspectOverride = Square;
        yield return null;

        for (int turn = 0; turn < 2; turn++)
        {
            string corner = Game.ActivePlayer == 1 ? "CornerTopLeft" : "CornerTopRight";
            foreach (string control in new[] { "EndTurnButton", "TimerBackgroundImage", "BattleLogCanvas" })
            {
                Assert.AreEqual(corner, Find<Transform>(control).parent.name, control + " in the turn of player " + Game.ActivePlayer);
            }
            Game.EndTurnAction();
            yield return null;
            yield return null;
        }
    }
}

