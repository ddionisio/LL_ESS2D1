using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "structureHouse", menuName = "Game/Structure (House)")]
public class StructureHouseData : StructureData {
    [Header("Citizen Info")]
    public UnitData citizenData;
    public int citizenCapacity;

    public override void SetupUnitSpawns(UnitController unitCtrl, int structureCount) {
        unitCtrl.AddUnitData(citizenData, citizenCapacity * structureCount);
    }
}
