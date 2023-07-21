using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "structureColonyShip", menuName = "Game/Structure/Colony Ship")]
public class StructureColonyShipData : StructureData {
    [Header("Medic Info")]
    public UnitData medicData;
    public int medicCapacity;

    public override void SetupUnitSpawns(UnitController unitCtrl, int structureCount) {
        unitCtrl.AddUnitData(medicData, medicCapacity * structureCount);
    }
}
