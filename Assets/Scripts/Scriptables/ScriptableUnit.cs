using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Unit", menuName = "Designers/Unit")]
public class ScriptableUnit : ScriptableObject
{
    public string unitName;
    public int unitHealth;
    public int moveRange;
    public int attackDamage;
    public int attackRange;
    public int armor;
    public float moveSpeed;
    public bool isKing;
    public int attacksCount;
    public bool summoningSickness;
    public int unitTypeId;
}
