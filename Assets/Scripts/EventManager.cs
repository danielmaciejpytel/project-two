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
}
