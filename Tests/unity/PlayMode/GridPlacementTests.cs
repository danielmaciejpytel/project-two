using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GameTestUtil;

// The GridPosition object places the whole board: its scale and offset move the tiles, the units, the shadow and the middle line
// together, and clicking still finds the tiles and the units where they are drawn.
public class GridPlacementTests
{
    [SetUp]
    public void SetUp() => Reset();

    [TearDown]
    public void TearDown()
    {
        GridPlacement placement = Object.FindFirstObjectByType<GridPlacement>();
        if (placement != null)
        {
            placement.BoardScale = 1.0f;
            placement.Offset = Vector2.zero;
        }
        Reset();
    }

    private static GridPlacement Placement()
    {
        GridPlacement placement = GridPlacement.Instance;
        Assert.IsNotNull(placement, "MainScene has no GridPosition object with a GridPlacement");
        return placement;
    }

    private static Vector3 Middle(TileController a, TileController b) => (a.transform.position + b.transform.position) * 0.5f;

    private static Vector3 MiddleOfTheBoard() => Middle(Game.GetGrid().GetTile(4, 4), Game.GetGrid().GetTile(5, 5));

    private static Bounds Shadow() => Find<SpriteRenderer>("MapShadow").bounds;

    [UnityTest]
    public IEnumerator TheBoardIsWhereItWasMadeAtScaleOneAndNoOffset()
    {
        yield return StartGame();
        Assert.AreEqual(1.0f, Placement().transform.lossyScale.x, 0.0001f);
        Assert.AreEqual(0.0f, MiddleOfTheBoard().x, 0.01f);
        Assert.AreEqual(0.0f, Shadow().center.x, 0.2f, "The shadow is centred under the board");
        Assert.Less(Shadow().center.y, MiddleOfTheBoard().y, "The shadow falls a little below the board");
    }

    [UnityTest]
    public IEnumerator TheScaleChangesTheSizeOfTheWholeBoardAroundItsMiddle()
    {
        yield return StartGame();
        Vector3 middleBefore = MiddleOfTheBoard();
        float tileStep = Vector3.Distance(Game.GetGrid().GetTile(4, 4).transform.position, Game.GetGrid().GetTile(5, 4).transform.position);
        float shadowWidth = Shadow().size.x;
        UnitController commander = Game.GetCommander(1);
        float unitHeight = commander.GetComponent<SpriteRenderer>().bounds.size.y;

        Placement().BoardScale = 0.6f;
        yield return null;

        Assert.AreEqual(middleBefore.x, MiddleOfTheBoard().x, 0.01f, "The middle of the board stays in place when only the scale changes");
        Assert.AreEqual(middleBefore.y, MiddleOfTheBoard().y, 0.01f);
        Assert.AreEqual(tileStep * 0.6f, Vector3.Distance(Game.GetGrid().GetTile(4, 4).transform.position, Game.GetGrid().GetTile(5, 4).transform.position), 0.01f);
        Assert.AreEqual(shadowWidth * 0.6f, Shadow().size.x, 0.05f, "The shadow of the board is smaller too");
        Assert.AreEqual(unitHeight * 0.6f, commander.GetComponent<SpriteRenderer>().bounds.size.y, 0.05f, "The units are smaller too");
    }

    [UnityTest]
    public IEnumerator TheOffsetMovesTheWholeBoardAndItsShadowTogether()
    {
        yield return StartGame();
        Vector3 middleBefore = MiddleOfTheBoard();
        Vector3 shadowBefore = Shadow().center;
        Vector3 commanderBefore = Game.GetCommander(1).transform.position;

        Placement().Offset = new Vector2(1.5f, -0.5f);
        yield return null;

        Assert.AreEqual(middleBefore.x + 1.5f, MiddleOfTheBoard().x, 0.01f);
        Assert.AreEqual(middleBefore.y - 0.5f, MiddleOfTheBoard().y, 0.01f);
        Assert.AreEqual(shadowBefore.x + 1.5f, Shadow().center.x, 0.01f);
        Assert.AreEqual(shadowBefore.y - 0.5f, Shadow().center.y, 0.01f);
        Assert.AreEqual(commanderBefore.x + 1.5f, Game.GetCommander(1).transform.position.x, 0.01f, "The units move with the board");
    }

    [UnityTest]
    public IEnumerator UnitsStandOnTheirTilesAtAnyScale()
    {
        yield return StartGame();
        UnitController commander = Game.GetCommander(1);
        Vector3 shiftAtScaleOne = commander.transform.position - commander.CurrentTile.transform.position;

        Placement().BoardScale = 0.6f;
        Placement().Offset = new Vector2(-2.0f, 0.4f);
        yield return null;

        Vector3 shift = commander.transform.position - commander.CurrentTile.transform.position;
        Assert.AreEqual(shiftAtScaleOne.x * 0.6f, shift.x, 0.01f, "The unit keeps its place on the tile");
        Assert.AreEqual(shiftAtScaleOne.y * 0.6f, shift.y, 0.01f);
    }

    [UnityTest]
    public IEnumerator ClicksFindTheTilesAndUnitsWhereTheyAreDrawn()
    {
        yield return StartGame();
        Placement().BoardScale = 0.7f;
        Placement().Offset = new Vector2(1.0f, 0.3f);
        yield return null;
        yield return new WaitForFixedUpdate();

        TileController tile = Game.GetGrid().GetTile(3, 5);
        Collider2D hit = Physics2D.OverlapPoint(tile.transform.position);
        Assert.IsNotNull(hit, "Something is under the tile");
        Assert.AreEqual(tile, hit.GetComponentInParent<TileController>(), "The tile is found at its new place");

        UnitController commander = Game.GetCommander(1);
        Collider2D unitHit = Physics2D.OverlapPoint(commander.transform.position);
        Assert.IsNotNull(unitHit);
        Assert.AreEqual(commander, unitHit.GetComponentInParent<UnitController>(), "The unit is found at its new place");
    }

    [UnityTest]
    public IEnumerator AUnitCanMoveOnASmallBoard()
    {
        yield return StartGame();
        UnitController commander = Game.GetCommander(Game.ActivePlayer);
        Vector3 shiftAtScaleOne = commander.transform.position - commander.CurrentTile.transform.position;
        Placement().BoardScale = 0.6f;
        Placement().Offset = new Vector2(-1.0f, 0.0f);
        yield return null;
        GridPosition at = commander.GetGridPosition();
        TileController target = null;
        foreach (GridPosition step in new[] { new GridPosition(1, 0), new GridPosition(0, -1), new GridPosition(-1, 0), new GridPosition(0, 1) })
        {
            TileController candidate = Game.GetGrid().GetTile(at.x + step.x, at.y + step.y);
            if (candidate != null && !candidate.IsOccupied && candidate.isWalkable()) { target = candidate; break; }
        }
        Assert.IsNotNull(target, "A free tile next to the commander");

        EventManager.Instance.UnitClicked(commander);
        EventManager.Instance.TileClicked(target);
        yield return WaitUntil(() => commander.CurrentTile == target, 5.0f);

        Assert.AreEqual(target, commander.CurrentTile, "The unit walked to the tile");
        yield return null;
        Vector3 shift = commander.transform.position - target.transform.position;
        Assert.AreEqual(shiftAtScaleOne.x * 0.6f, shift.x, 0.02f, "It stands on the tile the way it did at full size, only smaller");
        Assert.AreEqual(shiftAtScaleOne.y * 0.6f, shift.y, 0.02f);
    }
}
