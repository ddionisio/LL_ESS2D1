using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StructureState {
    None,
    Active,
    Spawning,
    Construction,
    Repair, //repairing
    Damage, //when hitpoint is decreased (used as immunity window)
    MoveReady, //during placement when move action is selected
    Moving, //for moveable structures, start moving
    Destroyed, //when hitpoint reaches 0
    Demolish, //when deconstruct via UI
}

public enum StructureStatus {
    Construct, //build/repair
    Demolish,

    Food,
    Water,
    Power,

    Growth, //for plants
}

public enum StructureStatusState {
    None,
    Progress,
    Require
}

[System.Flags]
public enum StructureAction {
    None = 0x0,

    Cancel = 0x1,

    Move = 0x2,
    Demolish = 0x4,

    //Upgrade?
}

public struct StructureSpawnParams {
    public const string data = "structureData"; //StructureData
    public const string spawnPoint = "structureSpawnP"; //Vector2
    public const string spawnNormal = "structureSpawnN"; //Vector2
}

public struct StructureStatusInfo {
    public StructureStatus type;
    public StructureStatusState state;
    public float progress; //[0, 1]

    public StructureStatusInfo(StructureStatus aType) {
        type = aType;
        state = StructureStatusState.None;
        progress = 0f;
    }
}