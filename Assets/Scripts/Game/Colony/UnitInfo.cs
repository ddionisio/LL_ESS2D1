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

    Retreat, //during hazzard events, retreat inside their respective base

    Dying //for frogs, how long before despawn. Can be revived by medic at this point
}

public struct UnitSpawnParams {
    public const string data = "unitData"; //UnitData
    public const string structureOwner = "owner"; //Structure (e.g. colony ship, house)
    public const string spawnPoint = "unitSpawnP"; //Vector2
}