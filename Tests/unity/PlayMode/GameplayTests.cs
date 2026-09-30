using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static GameTestUtil;

// The turn: the state machine behind the HUD buttons, Call, the timer and the range hints.
public class GameplayTests
{
    [SetUp]
    public void SetUp() => Reset();

    [TearDown]
    public void TearDown() => Reset();

    [UnityTest]
    public IEnumerator TheGameStartsInTheFirstTurnWithBothCommandersOnTheBoard()
    {
        yield return StartGame();

        Assert.IsInstanceOf<BeginTurnState>(Game.CurrentState);
        Assert.IsFalse(Game.IsGameOver);
        Assert.IsTrue(Game.ActivePlayer == 1 || Game.ActivePlayer == 2);
        Assert.IsTrue(Game.GetCommander(1).IsDeployed);
        Assert.IsTrue(Game.GetCommander(2).IsDeployed);
        Assert.IsTrue(Button("DeployMinionButton").gameObject.activeSelf, "Call");
        Assert.IsTrue(Button("EndTurnButton").gameObject.activeSelf, "End Turn");
        Assert.IsFalse(Button("AbilityButton").gameObject.activeSelf, "Ability needs a unit with an ability");
    }

    [UnityTest]
    public IEnumerator TheHudButtonsAreWired()
    {
        yield return StartGame();

        foreach (string name in new[] { "EndTurnButton", "DeployMinionButton", "AbilityButton", "UnitButton1", "UnitButton5" })
        {
            Assert.Greater(Button(name).onClick.GetPersistentEventCount(), 0, name + " does nothing when pressed");
        }
        Assert.IsNull(GameObject.Find("QuitButton"), "The HUD has no Quit button, the pause menu (Escape) has it");
        Assert.IsNull(GameObject.Find("ChangeModeButton"), "The HUD has no Mode button, F1 switches the designer view");
    }

    [UnityTest]
    public IEnumerator CallPutsAUnitNextToTheCommanderAndUsesUpTheCall()
    {
        yield return StartGame();
        int player = Game.ActivePlayer;
        UnitController unit = UnitToCall(player);
        TileController tile = FreeTileNextToCommander(player);

        Game.DeployAction();
        Assert.IsInstanceOf<DeploymentState>(Game.CurrentState);
        EventManager.Instance.UnitClicked(unit);
        EventManager.Instance.TileClicked(tile);
        yield return null;

        Assert.IsTrue(unit.IsDeployed);
        Assert.AreSame(tile, unit.CurrentTile);
        Assert.IsInstanceOf<BeginTurnState>(Game.CurrentState);
        Assert.IsTrue(Game.DeployedThisTurn());
        Assert.IsFalse(Button("DeployMinionButton").gameObject.activeSelf, "One call per turn");
    }

    [UnityTest]
    public IEnumerator CancellingCallKeepsItAvailable()
    {
        yield return StartGame();

        // A click on a tile before any card was picked.
        Game.DeployAction();
        EventManager.Instance.TileClicked(FarTile());
        Assert.IsInstanceOf<BeginTurnState>(Game.CurrentState);
        Assert.IsFalse(Game.DeployedThisTurn());
        Assert.IsTrue(Button("DeployMinionButton").gameObject.activeSelf);

        // Pressing Call a second time.
        Game.DeployAction();
        Assert.IsInstanceOf<DeploymentState>(Game.CurrentState);
        Game.DeployAction();
        Assert.IsInstanceOf<BeginTurnState>(Game.CurrentState);
        Assert.IsFalse(Game.DeployedThisTurn());
        Assert.IsTrue(Button("DeployMinionButton").gameObject.activeSelf);
        yield return null;
    }

    [UnityTest]
    public IEnumerator PickingACardToCallDoesNotOfferAnAbility()
    {
        yield return StartGame();

        Game.DeployAction();
        EventManager.Instance.UnitClicked(UnitToCall(Game.ActivePlayer));

        Assert.IsFalse(Button("AbilityButton").gameObject.activeSelf);
        yield return null;
    }

    [UnityTest]
    public IEnumerator EndingTheTurnPassesItAndMovesTheControlsToTheOtherSide()
    {
        yield return StartGame();
        RectTransform endTurn = (RectTransform)Button("EndTurnButton").transform;
        int first = Game.ActivePlayer;
        Assert.AreEqual(first == 1, IsOnLeftHalf(endTurn), "Super Hot's controls are on the left, Super Cold's on the right");

        Game.EndTurnAction();
        yield return null;

        Assert.AreEqual(GameController.GetOpponent(first), Game.ActivePlayer);
        Assert.AreEqual(first != 1, IsOnLeftHalf(endTurn));
        Assert.IsTrue(Button("DeployMinionButton").gameObject.activeSelf, "The new player can call");
    }

    [UnityTest]
    public IEnumerator TheTurnEndsWhenTheTimerRunsOut()
    {
        yield return StartGame();
        UIController ui = Game.GetUI();
        int first = Game.ActivePlayer;

        SetPrivate(ui, "_myTimer", 60);
        yield return WaitUntil(() => Game.ActivePlayer != first, 4.0f);

        Assert.AreNotEqual(first, Game.ActivePlayer, "The timer did not end the turn");
        Assert.Less(GetPrivate<int>(ui, "_myTimer"), 10, "The next turn starts with a fresh timer");
    }

    [UnityTest]
    public IEnumerator TheInfoPanelIsClearedWhenTheTurnChanges()
    {
        yield return StartGame();
        UnitController commander = Game.GetCommander(Game.ActivePlayer);
        Game.GetUI().DisplayUnit(commander);
        UnitTilePanelController panel = GetPrivate<UnitTilePanelController>(Game.GetUI(), "_myInfoPanel");
        Assert.AreEqual(commander.GetUnitName(), GetPrivate<TMPro.TMP_Text>(panel, "_name").text);

        Game.EndTurnAction();
        yield return null;

        Assert.AreEqual("", GetPrivate<TMPro.TMP_Text>(panel, "_name").text);
    }

    [UnityTest]
    public IEnumerator RangeHintsAreShownOnlyForTheTeamThatIsPlaying()
    {
        yield return StartGame();
        int active = Game.ActivePlayer;
        Color[,] clean = SnapshotOverlays();

        Game.GetCommander(GameController.GetOpponent(active)).PointerEnter();
        int changedByEnemy = ChangedTiles(clean);
        Game.GetCommander(GameController.GetOpponent(active)).PointerExit();
        yield return null;

        Game.GetCommander(active).PointerEnter();
        int changedByOwn = ChangedTiles(clean);

        Assert.LessOrEqual(changedByEnemy, 1, "An enemy shows only its own tile, no move or attack range");
        Assert.Greater(changedByOwn, 1, "A unit of the active team shows its move range");
    }

    [UnityTest]
    public IEnumerator KilledUnitsKeepTheirSmallCardsAfterTheTurnChanges()
    {
        yield return StartGame();
        int player = Game.ActivePlayer;
        UnitController unit = UnitToCall(player);
        Game.DeployAction();
        EventManager.Instance.UnitClicked(unit);
        EventManager.Instance.TileClicked(FreeTileNextToCommander(player));
        yield return null;

        unit.DamageUnit(999, "test");
        Assert.IsTrue(unit.IsKilled);
        Game.EndTurnAction();
        Game.EndTurnAction();
        yield return new WaitForSecondsRealtime(0.6f);

        bool found = false;
        foreach (ButtonUnitController card in Object.FindObjectsByType<ButtonUnitController>(FindObjectsSortMode.None))
        {
            if (GetPrivate<UnitController>(card, "_myUnit") != unit) continue;
            found = true;
            Assert.AreEqual(0.6f, card.transform.localScale.x, 0.01f, "The card of a killed unit is shown small");
        }
        Assert.IsTrue(found, "The killed unit has a card");
    }

    [UnityTest]
    public IEnumerator AUnitThatHasMovedShowsNoMoveRangeFromItsNewPlace()
    {
        yield return StartGame();
        UnitController commander = Game.GetCommander(Game.ActivePlayer);
        Color[,] clean = SnapshotOverlays();
        commander.PointerEnter();
        int changedBeforeMoving = ChangedTiles(clean);
        int crosshairsBeforeMoving = CrosshairTiles();
        commander.PointerExit();
        yield return null;

        commander.HasMoved = true;
        commander.PointerEnter();

        Assert.Greater(changedBeforeMoving, 1, "A unit that has not moved shows where it can go");
        Assert.LessOrEqual(ChangedTiles(clean), 1, "After the move only the unit's own tile is marked");
        Assert.Greater(crosshairsBeforeMoving, 0);
        Assert.AreEqual(crosshairsBeforeMoving > 0, CrosshairTiles() > 0, "It can still attack, so the attack range stays");
    }

    [UnityTest]
    public IEnumerator AUnitThatHasPlayedShowsNoRanges()
    {
        yield return StartGame();
        UnitController commander = Game.GetCommander(Game.ActivePlayer);
        Color[,] clean = SnapshotOverlays();

        commander.IsAvailable = false;
        commander.PointerEnter();

        Assert.LessOrEqual(ChangedTiles(clean), 1, "No move range");
        Assert.AreEqual(0, CrosshairTiles(), "No attack range either");
        yield return null;
    }
}
