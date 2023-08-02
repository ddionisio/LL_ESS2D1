using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UnitState {
    None,

    Idle, //determine decisions here
    Move, //move to destination
    Act, //perform action

    Hurt, //when damaged, also used as invul. delay

    Spawning,
    Despawning,

    Retreat, //run away from danger
    RetreatToBase, //during hazzard events, retreat inside their respective base
    BounceToBase, //bounce back to their structure owner

    Dying, //for frogs, how long before death. Can be revived by medic at this point
    Death, //animate and despawn
}

[System.Flags]
public enum DamageFlags {
    None = 0x0,
    Physical = 0x1,
    Poison = 0x2,
    Structure = 0x4,
}

public enum DirType {
    None,
    Up,
    Down,
    Left,
    Right
}

public struct UnitSpawnParams {
    public const string data = "unitData"; //UnitData
    public const string structureOwner = "owner"; //Structure (e.g. colony ship, house)
    public const string spawnPoint = "unitSpawnP"; //Vector2

    public const string structureTarget = "structTgt"; //target structure for specific units

    public const string moveDirType = "dirtype"; //DirType, for specific entities with one-off move
}