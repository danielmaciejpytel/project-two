using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Computer opponent. It plays through the same game states as a human (clicks on units and tiles,
/// Call and End Turn), so every rule, effect and animation works exactly as in hot-seat.
/// Strategy per turn: call one Doppelganger, sometimes use the commander's ability (Confuse or
/// Teleport), then each unit moves to the best tile and attacks the most valuable target;
/// the commander stays back and only attacks what is next to it.
/// Difficulty: Easy makes clumsy, partly random choices and never uses abilities; Normal is the
/// strategy above; Hard also avoids tiles the enemy can strike next turn, always calls and uses
/// abilities when useful, focuses wounded and dangerous targets and moves its commander out of danger.
/// </summary>
public class AIController : MonoBehaviour
{
    private const float ThinkDelay = 0.8f;
    private const float StepDelay = 0.45f;
    private const float ExecutionTimeout = 10.0f;
    // Easy plays a random move or target this often.
    private const float EasyMistakeChance = 0.5f;

    private GameController _game;
    private int _playerId;
    private Coroutine _turn;
    private AiDifficulty _difficulty = AiDifficulty.Normal;
    // Hard: tiles every enemy can stand on next turn.
    private Dictionary<UnitController, List<GridPosition>> _enemyReach;

    // Abilities: now and then on Normal, whenever useful on Hard, never on Easy.
    private float AbilityChance => _difficulty == AiDifficulty.Hard ? 1.0f : _difficulty == AiDifficulty.Normal ? 0.35f : 0.0f;

    private bool IsMyTurn => _game != null && !_game.IsGameOver && _game.ActivePlayer == _playerId;

    public void Initialize(GameController game, int playerId, AiDifficulty difficulty = AiDifficulty.Normal)
    {
        _game = game;
        _playerId = playerId;
        _difficulty = difficulty;
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
        // Easy forgets to call reinforcements half of the time.
        if (IsMyTurn && (_difficulty != AiDifficulty.Easy || Random.value < 0.5f)) yield return TryDeploy();
        if (IsMyTurn) yield return TryUseAbilities();

        foreach (UnitController unit in GetActingOrder())
        {
            if (!IsMyTurn) break;
            if (!unit.IsDeployed || unit.IsKilled || !unit.IsAvailable) continue;
            if (_difficulty == AiDifficulty.Hard) UpdateEnemyReach();
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

    private IEnumerator TryUseAbilities()
    {
        foreach (UnitController unit in GetActingOrder())
        {
            if (!IsMyTurn) yield break;
            IAbility ability = unit.GetComponent<IAbility>();
            if (ability == null || !ability.IsAvailableThisTurn() || !unit.IsAvailable || unit.HasMoved) continue;
            if (Random.value > AbilityChance) continue;

            if (ability is AbilityConfuse)
            {
                UnitController target = ChooseConfuseTarget();
                if (target == null) continue;
                yield return Select(unit);
                _game.RunWithoutInputLock(_game.AbilityAction);
                yield return new WaitForSeconds(StepDelay);
                _game.RunWithoutInputLock(() => EventManager.Instance.UnitClicked(target));
                yield return new WaitForSeconds(StepDelay);
            }
            else if (ability is AbilityTeleport)
            {
                if (!ChooseTeleport(unit, out UnitController ally, out TileController tile)) continue;
                yield return Select(unit);
                _game.RunWithoutInputLock(_game.AbilityAction);
                yield return new WaitForSeconds(StepDelay);
                _game.RunWithoutInputLock(() => EventManager.Instance.UnitClicked(ally));
                yield return new WaitForSeconds(StepDelay);
                _game.RunWithoutInputLock(() => EventManager.Instance.TileClicked(tile));
                yield return new WaitForSeconds(StepDelay);
            }
            yield return Deselect(unit);
        }
    }

    // After an ability the caster stays selected; clicking a tile it can't reach returns to the turn start.
    private IEnumerator Deselect(UnitController unit)
    {
        if (!(_game.CurrentState is UnitSelectedState)) yield break;
        BoardGrid grid = _game.GetGrid();
        TileController far = null;
        float farthest = -1.0f;
        for (int y = 0; y < grid.GetBoardHeight(); y++)
        {
            for (int x = 0; x < grid.GetBoardWidth(); x++)
            {
                TileController tile = grid.GetTile(x, y);
                if (tile == null || tile.IsOccupied) continue;
                float distance = Distance(unit.GetGridPosition(), tile.GetGridPosition());
                if (distance > farthest && !grid.IsTileInMoveRange(unit, tile))
                {
                    farthest = distance;
                    far = tile;
                }
            }
        }
        if (far == null) yield break;
        _game.RunWithoutInputLock(() => EventManager.Instance.TileClicked(far));
        yield return new WaitForSeconds(StepDelay);
    }

    private IEnumerator PlayUnit(UnitController unit)
    {
        BoardGrid grid = _game.GetGrid();

        // The commander losing means losing the game, so it holds its position
        // (on Hard it steps out of danger when it can).
        if ((!unit.IsKing() || _difficulty == AiDifficulty.Hard) && !unit.HasMoved)
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

    // Confuse takes one attack from an enemy that is close enough to strike one of our units next turn.
    private UnitController ChooseConfuseTarget()
    {
        List<UnitController> mine = new List<UnitController>();
        foreach (UnitController unit in _game.Units)
        {
            if (unit.GetPlayerId() == _playerId && unit.IsDeployed && !unit.IsKilled && unit.CurrentTile != null) mine.Add(unit);
        }

        UnitController best = null;
        float bestValue = 0.0f;
        foreach (UnitController enemy in Enemies())
        {
            if (enemy.FreeAttacksCount < 1 || enemy.GetComponent<EffectConfused>() != null) continue;
            int reach = enemy.GetMoveRange() + enemy.GetAttackRange() + 1;
            float value = 0.0f;
            foreach (UnitController unit in mine)
            {
                if (Distance(enemy.GetGridPosition(), unit.GetGridPosition()) > reach) continue;
                value = Mathf.Max(value, enemy.GetAttackStrength() * 2.0f + (unit.IsKing() ? 10.0f : 0.0f));
            }
            if (value > bestValue)
            {
                bestValue = value;
                best = enemy;
            }
        }
        return best;
    }

    // Teleport shifts an ally by one tile; used when it puts the ally in a clearly better spot.
    private bool ChooseTeleport(UnitController caster, out UnitController bestAlly, out TileController bestTile)
    {
        bestAlly = null;
        bestTile = null;
        float bestGain = 5.0f;
        BoardGrid grid = _game.GetGrid();
        foreach (UnitController ally in _game.Units)
        {
            if (ally == caster || ally.GetPlayerId() != _playerId || !ally.IsDeployed || ally.IsKilled || ally.CurrentTile == null) continue;
            float current = ScorePosition(ally, ally.CurrentTile);
            GridPosition center = ally.GetGridPosition();
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    TileController tile = grid.GetTile(center.x + dx, center.y + dy);
                    if (tile == null || tile.IsOccupied || !tile.isWalkable()) continue;
                    float gain = ScorePosition(ally, tile) - current;
                    if (gain > bestGain)
                    {
                        bestGain = gain;
                        bestAlly = ally;
                        bestTile = tile;
                    }
                }
            }
        }
        return bestAlly != null;
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
        List<TileController> reachable = _game.GetGrid().GetReachableTiles(unit);
        if (_difficulty == AiDifficulty.Easy && Random.value < EasyMistakeChance)
        {
            // Clumsy: wander to a random tile (or stay).
            int pick = Random.Range(0, reachable.Count + 1);
            return pick < reachable.Count ? reachable[pick] : null;
        }

        TileController best = null;
        float bestScore = ScorePosition(unit, unit.CurrentTile);
        foreach (TileController tile in reachable)
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

        if (_difficulty == AiDifficulty.Hard)
        {
            // Avoid tiles the enemy can strike next turn; never walk into a lethal spot.
            int danger = DamageThreat(unit, position);
            score -= danger * (unit.IsKing() ? 12.0f : 4.0f);
            if (danger >= unit.GetHP()) score -= unit.IsKing() ? 2000.0f : 80.0f;
            // The commander only looks for safety, it doesn't charge.
            if (unit.IsKing()) return score - 0.1f * Distance(position, unit.GetGridPosition());

            // While the commander is in danger, guard it: stay next to it and go for the units threatening it.
            UnitController myCommander = _game.GetCommander(_playerId);
            if (myCommander != null && !myCommander.IsKilled && DamageThreat(myCommander, myCommander.GetGridPosition()) > 0)
            {
                GridPosition kingPosition = myCommander.GetGridPosition();
                if (Mathf.Abs(kingPosition.x - position.x) <= 1 && Mathf.Abs(kingPosition.y - position.y) <= 1) score += 15.0f;
                foreach (UnitController enemy in Enemies())
                {
                    if (ThreatensMyCommander(enemy) && unit.IsTargetValid(enemy) && CanAttackFrom(unit, position, enemy)) { score += 40.0f; break; }
                }
            }
        }

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
            // Easy doesn't weigh targets at all.
            float value = _difficulty == AiDifficulty.Easy ? Random.value : TargetValue(unit, enemy);
            // Hard finishes off wounded units, hits the most dangerous ones and above all those threatening its commander.
            if (_difficulty == AiDifficulty.Hard)
            {
                value += enemy.GetAttackStrength() * 3.0f + (enemy.GetMaxHP() - enemy.GetHP()) * 2.0f;
                if (ThreatensMyCommander(enemy)) value += 40.0f;
            }
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

    // Hard: where every enemy can stand next turn (its reachable tiles plus its own).
    private void UpdateEnemyReach()
    {
        _enemyReach = new Dictionary<UnitController, List<GridPosition>>();
        foreach (UnitController enemy in Enemies())
        {
            List<GridPosition> positions = new List<GridPosition> { enemy.GetGridPosition() };
            foreach (TileController tile in _game.GetGrid().GetReachableTiles(enemy)) positions.Add(tile.GetGridPosition());
            _enemyReach[enemy] = positions;
        }
    }

    // Hard: can this enemy strike our commander next turn?
    private bool ThreatensMyCommander(UnitController enemy)
    {
        UnitController myCommander = _game.GetCommander(_playerId);
        if (myCommander == null || myCommander.IsKilled || myCommander.CurrentTile == null) return false;
        if (_enemyReach == null) UpdateEnemyReach();
        if (!_enemyReach.TryGetValue(enemy, out List<GridPosition> positions)) return false;
        GridPosition kingPosition = myCommander.GetGridPosition();
        foreach (GridPosition from in positions)
        {
            if (from != kingPosition && CanHit(enemy, from, kingPosition, null)) return true;
        }
        return false;
    }

    // Hard: total damage the enemy could deal to this unit if it stood on "position" next turn.
    private int DamageThreat(UnitController unit, GridPosition position)
    {
        if (_enemyReach == null) UpdateEnemyReach();
        int total = 0;
        foreach (KeyValuePair<UnitController, List<GridPosition>> pair in _enemyReach)
        {
            UnitController enemy = pair.Key;
            if (enemy.IsKilled || enemy.CurrentTile == null) continue;
            foreach (GridPosition from in pair.Value)
            {
                if (from == position || !CanHit(enemy, from, position, unit)) continue;
                total += unit.CalculateDamage(enemy.GetCalculatedAttack(unit)) * Mathf.Max(1, enemy.GetBaseAttacksCount());
                break;
            }
        }
        return total;
    }

    // Same range rules as BoardGrid.IsTileInAttackRange, evaluated as if the attacker stood on "from".
    private bool CanAttackFrom(UnitController attacker, GridPosition from, UnitController target)
    {
        return CanHit(attacker, from, target.GetGridPosition(), attacker);
    }

    // "ignored" is a unit that will have moved away, so its tile doesn't block the line of fire.
    private bool CanHit(UnitController attacker, GridPosition from, GridPosition to, UnitController ignored)
    {
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
            // Tiles of units that will have moved are empty by then.
            if (between.IsOccupied && between.Unit != attacker && between.Unit != ignored) return false;
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
