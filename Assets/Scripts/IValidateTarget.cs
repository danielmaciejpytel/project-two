using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IValidateTarget
{
    bool IsTargetValid(UnitController targetUnit);
    GridPosition GetValidPosition();
}
