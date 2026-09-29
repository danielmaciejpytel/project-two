using System;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    public event Action<UnitController> OnUnitClicked;
    public event Action<UnitController> OnUnitHovered;
    public event Action<UnitController> OnUnitUnhovered;
    public event Action<TileController> OnTileClicked;
    public event Action<TileController> OnTileHovered;
    public event Action<UnitController> OnExecutionEnd;
    public event Action<UnitController> OnUnitKilled;

    // Battle log feed: these only report what happened, game rules don't listen to them.
    public event Action<int> OnTurnStarted;
    public event Action<UnitController> OnUnitDeployed;
    public event Action<UnitController, TileController> OnUnitMoved;
    public event Action<UnitController, UnitController, int, int> OnUnitAttacked;
    public event Action<UnitController, int, string> OnUnitDamaged;
    public event Action<UnitController, int, string> OnUnitHealed;
    public event Action<UnitController, string, UnitController> OnEffectApplied;
    public event Action<UnitController, string, UnitController> OnAbilityUsed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void UnitClicked(UnitController clickedUnit) => OnUnitClicked?.Invoke(clickedUnit);

    public void UnitHovered(UnitController hoveredUnit) => OnUnitHovered?.Invoke(hoveredUnit);

    public void UnitUnhovered(UnitController unhoveredUnit) => OnUnitUnhovered?.Invoke(unhoveredUnit);

    public void TileClicked(TileController clickedTile) => OnTileClicked?.Invoke(clickedTile);

    public void TileHovered(TileController hoveredTile) => OnTileHovered?.Invoke(hoveredTile);

    public void ExecutionEnded(UnitController unit) => OnExecutionEnd?.Invoke(unit);

    public void UnitKilled(UnitController killedUnit) => OnUnitKilled?.Invoke(killedUnit);

    public void TurnStarted(int playerId) => OnTurnStarted?.Invoke(playerId);

    public void UnitDeployed(UnitController unit) => OnUnitDeployed?.Invoke(unit);

    public void UnitMoved(UnitController unit, TileController tile) => OnUnitMoved?.Invoke(unit, tile);

    public void UnitAttacked(UnitController attacker, UnitController target, int attackPower, int damageTaken)
        => OnUnitAttacked?.Invoke(attacker, target, attackPower, damageTaken);

    public void UnitDamaged(UnitController unit, int damageTaken, string source) => OnUnitDamaged?.Invoke(unit, damageTaken, source);

    public void UnitHealed(UnitController unit, int amount, string source) => OnUnitHealed?.Invoke(unit, amount, source);

    public void EffectApplied(UnitController unit, string effectName, UnitController source) => OnEffectApplied?.Invoke(unit, effectName, source);

    public void AbilityUsed(UnitController user, string abilityName, UnitController target) => OnAbilityUsed?.Invoke(user, abilityName, target);
}
