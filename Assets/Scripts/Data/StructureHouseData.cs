using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "structureHouse", menuName = "Game/Structure (House)")]
public class StructureHouseData : StructureData {
    [Header("House Info")]
    public UnitData unitSpawnData;
    public int unitSpawnCapacity; //also determines population capacity
}
