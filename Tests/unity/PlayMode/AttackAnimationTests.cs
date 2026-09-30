using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif
using static GameTestUtil;

// A unit that is killed by an attack dies (death animation, end of the game) only when the attacker has finished its attack animation,
// and its death begins right after it.
public class AttackAnimationTests
{
    private class Outcome
    {
        public bool SawTheAttack, DyingDuringTheAttack, DeadForTheGameDuringTheAttack;
        public float AttackEnded = -1.0f, DeathStarted = -1.0f;
    }

    [SetUp]
    public void SetUp() => Reset();

    [TearDown]
    public void TearDown() => Reset();

    // The attacker hits a victim that has 1 health left, and everything is watched frame by frame until the game is over.
    private static IEnumerator AttackToKill(UnitController attacker, UnitController victim, Outcome outcome)
    {
        Animator victimAnimator = GetPrivate<Animator>(victim, "_myAnimator");
        victim.DamageUnit(victim.GetHP() - 1, "test");
        yield return new WaitForSecondsRealtime(0.8f);

        attacker.AttackUnit(victim);
        float end = Time.realtimeSinceStartup + 10.0f;
        while (!Game.IsGameOver && Time.realtimeSinceStartup < end)
        {
            bool attacking = attacker.IsPlayingAttack;
            if (outcome.SawTheAttack && !attacking && outcome.AttackEnded < 0.0f) outcome.AttackEnded = Time.realtimeSinceStartup;
            outcome.SawTheAttack |= attacking;
            if (attacking && victim.IsKilled) outcome.DeadForTheGameDuringTheAttack = true;
            // The animator keeps a trigger set until the death animation is entered, so it shows when the order to die was given.
            if (attacking && (victim.IsPlayingDeath || victimAnimator.GetBool("Die"))) outcome.DyingDuringTheAttack = true;
            if (victim.IsPlayingDeath && outcome.DeathStarted < 0.0f) outcome.DeathStarted = Time.realtimeSinceStartup;
            yield return null;
        }
    }

    private static void AssertTheDeathFollowsTheAttack(Outcome outcome, UnitController attacker)
    {
        Assert.IsTrue(outcome.SawTheAttack, "The attack animation played");
        Assert.IsTrue(outcome.DeadForTheGameDuringTheAttack, "The hit already counted (the test would prove nothing otherwise)");
        Assert.IsFalse(outcome.DyingDuringTheAttack, "The victim was not told to die before the attack animation ended");
        Assert.IsTrue(Game.IsGameOver, "The victim died after the attack");
        Assert.IsFalse(attacker.IsPlayingAttack, "The end of the game comes after the attack animation");
        Assert.Greater(outcome.AttackEnded, 0.0f);
        Assert.Greater(outcome.DeathStarted, 0.0f, "The death animation played");
        Assert.LessOrEqual(outcome.DeathStarted - outcome.AttackEnded, 0.8f, "The death begins right after the attack animation, not after the idle animation");
    }

    [UnityTest]
    public IEnumerator TheDeathAnimationStartsRightAfterTheAttackAnimationHasEnded()
    {
        yield return StartGame();
        UnitController attacker = Game.GetCommander(1);
        Outcome outcome = new Outcome();

        yield return AttackToKill(attacker, Game.GetCommander(2), outcome);

        AssertTheDeathFollowsTheAttack(outcome, attacker);

        // A game that ends with an attack: the end screen is the same as after any other end.
        yield return new WaitForSecondsRealtime(1.0f);
        RectTransform banner = (RectTransform)EndScreen().transform.Find("WinnerBackgroundImage");
        RectTransform summary = (RectTransform)EndScreen().transform.Find("Buttons/SummaryPanel");
        Assert.AreEqual(summary.rect.width, banner.rect.width, 0.5f, "The winner banner is as wide as the panels below it");
    }

    [UnityTest]
    public IEnumerator TheSameHoldsForAUnitThatWasCalled()
    {
        yield return StartGame();
        int player = Game.ActivePlayer;
        UnitController attacker = UnitToCall(player);
        Game.DeployAction();
        EventManager.Instance.UnitClicked(attacker);
        EventManager.Instance.TileClicked(FreeTileNextToCommander(player));
        yield return new WaitForSecondsRealtime(1.0f);
        Assert.IsTrue(attacker.IsDeployed);
        Outcome outcome = new Outcome();

        // The enemy commander is the victim: its death ends the game.
        yield return AttackToKill(attacker, Game.GetCommander(GameController.GetOpponent(player)), outcome);

        AssertTheDeathFollowsTheAttack(outcome, attacker);
    }

    [UnityTest]
    public IEnumerator ADamageThatIsNotAnAttackKillsAtOnce()
    {
        yield return StartGame();
        UnitController victim = Game.GetCommander(2);

        victim.DamageUnit(999, "test");
        yield return WaitUntil(() => victim.IsPlayingDeath, 2.0f);

        Assert.IsTrue(victim.IsPlayingDeath, "Burning, tiles and the like are not held back");
    }

#if UNITY_EDITOR
    // The sprites a clip shows, in the order of its frames.
    private static List<Sprite> SpritesOf(Animator animator, string clipNameEnd)
    {
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (!clip.name.EndsWith(clipNameEnd, System.StringComparison.OrdinalIgnoreCase)) continue;
            List<Sprite> sprites = new List<Sprite>();
            foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                foreach (ObjectReferenceKeyframe key in AnimationUtility.GetObjectReferenceCurve(clip, binding)) sprites.Add(key.value as Sprite);
            }
            return sprites;
        }
        return new List<Sprite>();
    }

    [UnityTest]
    public IEnumerator TheVictimGoesFromTheIdleFramesStraightIntoTheDeathFramesWithoutAJumpBack()
    {
        yield return StartGame();
        UnitController attacker = Game.GetCommander(1);
        UnitController victim = Game.GetCommander(2);
        victim.DamageUnit(victim.GetHP() - 1, "test");
        // Long enough for the damage animation to end, so the victim is idling when it is hit for the last time.
        yield return new WaitForSecondsRealtime(3.5f);
        Animator animator = GetPrivate<Animator>(victim, "_myAnimator");
        SpriteRenderer renderer = victim.GetComponent<SpriteRenderer>();
        List<Sprite> idle = SpritesOf(animator, "Idle");
        List<Sprite> death = SpritesOf(animator, "Death");
        Assert.IsNotEmpty(idle);
        Assert.IsNotEmpty(death);

        attacker.AttackUnit(victim);
        List<Sprite> shown = new List<Sprite>();
        bool orderGiven = false;
        float end = Time.realtimeSinceStartup + 10.0f;
        while (!Game.IsGameOver && Time.realtimeSinceStartup < end)
        {
            orderGiven |= victim.IsPlayingDeath;
            if (orderGiven && (shown.Count == 0 || shown[shown.Count - 1] != renderer.sprite)) shown.Add(renderer.sprite);
            yield return null;
        }

        int firstDeathFrame = shown.FindIndex(death.Contains);
        Assert.GreaterOrEqual(firstDeathFrame, 0, "The death frames were shown");
        for (int i = 0; i < firstDeathFrame; i++) CollectionAssert.Contains(idle, shown[i], "Before the death only idle frames, no foreign frame in between (frame " + i + ")");
        int previous = -1;
        for (int i = firstDeathFrame; i < shown.Count; i++)
        {
            int position = death.IndexOf(shown[i]);
            Assert.GreaterOrEqual(position, 0, "After the first death frame only death frames (frame " + i + ")");
            Assert.GreaterOrEqual(position, previous, "The death frames come in order, no jump back");
            previous = position;
        }
        Assert.LessOrEqual(firstDeathFrame, 12, "Only a few idle frames are left before the death (the blend is short)");
    }
#endif
}
