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
    public bool hitpointStartApply; //if true, apply hitpointStart at spawn
    public int hitpointStart; //initial hitpoint value
    public int hitpoints; //for damageable, set to 0 for invulnerable

    [SerializeField]
    float _moveSpeed = 1f;
    [SerializeField]
    float _moveSpeedDeviation = 0f;
    [SerializeField]
    float _runSpeed = 1.5f;

    public bool canRevive; //can be revived by some means (e.g. medic for frogs)

    public DamageFlags damageImmunity;

    [Header("Spawn")]
    public Unit spawnPrefab;

    public float moveSpeed { 
        get {
            if(_moveSpeedDeviation != 0f)
                return _moveSpeed + Random.Range(-_moveSpeedDeviation, _moveSpeedDeviation);
            else
                return _moveSpeed;
        }
    }

    public float runSpeed { get { return _runSpeed; } }

    /// <summary>
    /// For units that need specific setup during initialization (cache projectiles, spawning other units)
    /// </summary>
    public virtual void Setup(ColonyController colonyCtrl) { }
}
