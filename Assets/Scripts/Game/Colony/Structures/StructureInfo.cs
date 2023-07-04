using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StructureState {
    None,
    Active,
    Spawning,
    Construction,
    Demolish, //when moving/deconstruct via UI
    Damaged, //when hp reaches 0
    Reparing,
}

public struct StructureSpawnParams {
    public const string spawnPoint = "structureSpawnPt";
}