using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAttackModifier
{
    int GetAttackModifier(UnitController target);
}
