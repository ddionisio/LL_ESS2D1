using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "structureColonyShip", menuName = "Game/Structure/Colony Ship")]
public class StructureColonyShipData : StructureData {
    [Header("Medic Info")]
    public UnitData medicData;
    public int medicCapacity;

    public override void Setup(ColonyController colonyCtrl, int structureCount) {
        if(medicData && medicCapacity > 0) {
            colonyCtrl.unitController.AddUnitData(medicData, medicCapacity * structureCount, false);
            medicData.Setup(colonyCtrl);
        }
    }
}
