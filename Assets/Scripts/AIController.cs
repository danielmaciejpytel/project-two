using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Computer opponent. It plays through the same game states as a human (clicks on units and tiles,
/// Call and End Turn), so every rule, effect and animation works exactly as in hot-seat.
/// Strategy per turn: call one Doppelganger, then each unit moves to the best tile and attacks
/// the most valuable target; the commander stays back and only attacks what is next to it.
/// </summary>
public class AIController : MonoBehaviour
{
    private const float ThinkDelay = 0.8f;
    private const float StepDelay = 0.45f;
    private const float ExecutionTimeout = 10.0f;

    private GameController _game;
    private int _playerId;
    private Coroutine _turn;

    private bool IsMyTurn => _game != null && !_game.IsGameOver && _game.ActivePlayer == _playerId;

    public void Initialize(GameController game, int playerId)
    {
        _game = game;
        _playerId = playerId;
        EventManager.Instance.OnTurnStarted += OnTurnStarted;
    }

    private void OnDestroy()
    {
        if (EventManager.Instance != null) EventManager.Instance.OnTurnStarted -= OnTurnStarted;
    }

    private void OnTurnStarted(int playerId)
    {
        if (playerId != _playerId) return;
        if (_turn != null) StopCoroutine(_turn);
        _turn = StartCoroutine(PlayTurn());
    }

    private IEnumerator PlayTurn()
    {
        yield return new WaitForSeconds(ThinkDelay);
        if (IsMyTurn) yield return TryDeploy();

        foreach (UnitController unit in GetActingOrder())
        {
            if (!IsMyTurn) break;
            if (!unit.IsDeployed || unit.IsKilled || !unit.IsAvailable) continue;
            yield return PlayUnit(unit);
        }

        // Units that had nothing to do are still available, so the turn has to be ended explicitly.
        if (IsMyTurn)
        {
            yield return new WaitForSeconds(StepDelay);
            if (IsMyTurn) _game.RunWithoutInputLock(_game.EndTurnAction);
        }
        _turn = null;
    }

    #region Actions

    private IEnumerator TryDeploy()
    {
        UIController ui = _game.GetUI();
        UnitController commander = _game.GetCommander(_playerId);
        if (ui.DeployedThisTurn() || commander == null || !(_game.CurrentState is BeginTurnState)) yield break;

        List<UnitController> reserve = new List<UnitController>();
        foreach (UnitController unit in _game.Units)
        {
            if (unit.GetPlayerId() == _playerId && !unit.IsDeployed && !unit.IsKilled) reserve.Add(unit);
        }
        if (reserve.Count == 0) yield break;

        TileController tile = ChooseDeploymentTile(commander);
        if (tile == null) yield break;
        UnitController unitToCall = reserve[Random.Range(0, reserve.Count)];

        _game.RunWithoutInputLock(_game.DeployAction);
        yield return new WaitForSeconds(StepDelay);
        _game.RunWithoutInputLock(() => EventManager.Instance.UnitClicked(unitToCall));
        yield return new WaitForSeconds(StepDelay);
        _game.RunWithoutInputLock(() => EventManager.Instance.TileClicked(tile));
        yield return new WaitForSeconds(StepDelay);
    }

    private IEnumerator PlayUnit(UnitController unit)
    {
        BoardGrid grid = _game.GetGrid();

        // The commander losing means losing the game, so it holds its position.
        if (!unit.IsKing() && !unit.HasMoved)
        {
            TileController destination = ChooseDestination(unit);
            if (destination != null)
            {
                yield return Select(unit);
                _game.RunWithoutInputLock(() => EventManager.Instance.TileClicked(destination));
                yield return WaitForExecution();
            }
        }

        while (IsMyTurn && !unit.IsKilled && unit.IsAvailable && unit.FreeAttacksCount > 0 && grid.HasPossibleAttack(unit))
        {
            UnitController target = ChooseTarget(unit);
            if (target == null) break;
            yield return Select(unit);
            int attacksBefore = unit.FreeAttacksCount;
            _game.RunWithoutInputLock(() => EventManager.Instance.UnitClicked(target));
            yield return WaitForExecution();
            // Nothing happened (e.g. target no longer valid): stop instead of retrying forever.
            if (unit.FreeAttacksCount == attacksBefore && unit.IsAvailable) break;
        }
    }

    // Clicking an own unit selects it from any state; clicking the already selected one changes nothing.
    private IEnumerator Select(UnitController unit)
    {
        _game.RunWithoutInputLock(() => EventManager.Instance.UnitClicked(unit));
        yield return new WaitForSeconds(StepDelay);
    }

    private IEnumerator WaitForExecution()
    {
        float elapsed = 0.0f;
        yield return null;
        while (_game.CurrentState is ExecutionState && elapsed < ExecutionTimeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(StepDelay);
    }

    #endregion

    #region Evaluation

    private List<UnitController> GetActingOrder()
    {
        // Units closest to the enemy act first, the commander last.
        List<UnitController> order = new List<UnitController>();
        foreach (UnitController unit in _game.Units)
        {
            if (unit.GetPlayerId() == _playerId && unit.IsDeployed && !unit.IsKilled) order.Add(unit);
        }
        order.Sort((a, b) =>
        {
            if (a.IsKing() != b.IsKing()) return a.IsKing() ? 1 : -1;
            return DistanceToNearestEnemy(a.GetGridPosition()).CompareTo(DistanceToNearestEnemy(b.GetGridPosition()));
        });
        return order;
    }

    private IEnumerable<UnitController> Enemies()
    {
        foreach (UnitController unit in _game.Units)
        {
            if (unit.GetPlayerId() != _playerId && unit.IsDeployed && !unit.IsKilled && unit.CurrentTile != null) yield return unit;
        }
    }

    private TileController ChooseDeploymentTile(UnitController commander)
    {
        GridPosition center = commander.GetGridPosition();
        BoardGrid grid = _game.GetGrid();
        TileController best = null;
        float bestScore = float.MinValue;
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                TileController tile = grid.GetTile(center.x + dx, center.y + dy);
                if (tile == null || tile == commander.CurrentTile || tile.IsOccupied || !tile.isWalkable()) continue;
                float score = TileBonus(tile, false) - DistanceToNearestEnemy(tile.GetGridPosition());
                if (score > bestScore)
                {
                    bestScore = score;
                    best = tile;
                }
            }
        }
        return best;
    }

    /// <summary>Best tile to move to, or null when staying is best.</summary>
    private TileController ChooseDestination(UnitController unit)
    {
        TileController best = null;
        float bestScore = ScorePosition(unit, unit.CurrentTile);
        foreach (TileController tile in _game.GetGrid().GetReachableTiles(unit))
        {
            float score = ScorePosition(unit, tile);
            if (score > bestScore)
            {
                bestScore = score;
                best = tile;
            }
        }
        return best;
    }

    private float ScorePosition(UnitController unit, TileController tile)
    {
        GridPosition position = tile.GetGridPosition();
        float score = TileBonus(tile, unit.GetAttackRange() > 1);

        float bestTarget = 0.0f;
        foreach (UnitController enemy in Enemies())
        {
            if (!unit.IsTargetValid(enemy) || !CanAttackFrom(unit, position, enemy)) continue;
            bestTarget = Mathf.Max(bestTarget, TargetValue(unit, enemy));
        }
        if (bestTarget > 0.0f) score += 100.0f + bestTarget;

        // Close in on the enemy, above all on its commander.
        UnitController enemyCommander = _game.GetCommander(GameController.GetOpponent(_playerId));
        if (enemyCommander != null && !enemyCommander.IsKilled) score -= 1.5f * Distance(position, enemyCommander.GetGridPosition());
        score -= 0.5f * DistanceToNearestEnemy(position);
        return score;
    }

    private UnitController ChooseTarget(UnitController unit)
    {
        BoardGrid grid = _game.GetGrid();
        UnitController best = null;
        float bestValue = float.MinValue;
        foreach (UnitController enemy in Enemies())
        {
            if (!unit.IsTargetValid(enemy) || !grid.IsTileInAttackRange(unit, enemy.CurrentTile)) continue;
            float value = TargetValue(unit, enemy);
            if (value > bestValue)
            {
                bestValue = value;
                best = enemy;
            }
        }
        return best;
    }

    private static float TargetValue(UnitController attacker, UnitController target)
    {
        int damage = target.CalculateDamage(attacker.GetCalculatedAttack(target));
        float value = damage;
        bool kills = damage >= target.GetHP();
        if (kills) value += target.IsKing() ? 1000.0f : 50.0f;
        if (target.IsKing()) value += 30.0f;
        return value;
    }

    // Same range rules as BoardGrid.IsTileInAttackRange, evaluated as if the attacker stood on "from".
    private bool CanAttackFrom(UnitController attacker, GridPosition from, UnitController target)
    {
        GridPosition to = target.GetGridPosition();
        int dx = Mathf.Abs(from.x - to.x);
        int dy = Mathf.Abs(from.y - to.y);
        if (dx <= 1 && dy <= 1) return true;

        int range = attacker.GetAttackRange();
        if (range <= 1) return false;
        if (!((dx == 0 && dy <= range) || (dy == 0 && dx <= range))) return false;

        int stepX = System.Math.Sign(to.x - from.x);
        int stepY = System.Math.Sign(to.y - from.y);
        BoardGrid grid = _game.GetGrid();
        for (int x = from.x + stepX, y = from.y + stepY; x != to.x || y != to.y; x += stepX, y += stepY)
        {
            TileController between = grid.GetTile(x, y);
            // The attacker's current tile will be empty after it moves.
            if (between.IsOccupied && between.Unit != attacker) return false;
        }
        return true;
    }

    private static float TileBonus(TileController tile, bool isRanged)
    {
        switch (tile.GetTileName())
        {
            case "Dead Zone": return -15.0f;
            case "Safe Zone": return 5.0f;
            case "Cover": return 4.0f;
            case "Reinforcement Field": return 4.0f;
            case "High Ground": return isRanged ? 6.0f : 2.0f;
            case "Restriction Area": return -2.0f;
            default: return 0.0f;
        }
    }

    private float DistanceToNearestEnemy(GridPosition position)
    {
        float best = 100.0f;
        foreach (UnitController enemy in Enemies())
        {
            best = Mathf.Min(best, Distance(position, enemy.GetGridPosition()));
        }
        return best;
    }

    private static float Distance(GridPosition a, GridPosition b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    #endregion
}
