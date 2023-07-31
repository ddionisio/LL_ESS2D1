using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "unitAttack", menuName = "Game/Unit/Attack")]
public class UnitAttackData : UnitData {
    [Header("Attack Info")]
    [M8.TagSelector]
    public string attackUnitTagFilter;
    public DamageFlags attackFlags;
    public int attackDamage = 1;
    public float attackIdleDelay = 2f;

    public bool canAttackStructure { get { return (attackFlags & DamageFlags.Structure) != DamageFlags.None; } }

    public bool CheckUnitTag(GameObject go) {
        return string.IsNullOrEmpty(attackUnitTagFilter) || go.CompareTag(attackUnitTagFilter);
    }

    public bool CanAttackUnit(Unit unit) {
        return unit.isDamageable && ((attackFlags & ~DamageFlags.Structure) & unit.data.damageImmunity) == DamageFlags.None;
    }
}
