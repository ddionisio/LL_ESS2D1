using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "unit", menuName = "Game/Unit")]
public class UnitData : ScriptableObject {
    [Header("Info")]
    [M8.Localize]
    public string nameRef;
    [M8.Localize]
    public string descRef;

    public Sprite icon;

    [Header("Stats")]
    public int hitpoints; //for damageable, set to 0 for invulnerable

    public float moveSpeed = 1f;
    public float runSpeed = 1.5f;

    public bool canRevive; //can be revived by some means (e.g. medic for frogs)

    [Header("Spawn")]
    public Unit spawnPrefab;
}
