using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "unitSpawner", menuName = "Game/Unit/Spawner")]
public class UnitSpawnerData : UnitData {
    [Header("Spawn Info")]
    public UnitData spawnUnitData;
    public int spawnUnitCount;
    public float spawnDelay;

    public override void Setup(ColonyController colonyCtrl) {
        if(spawnUnitData && spawnUnitCount > 0)
            colonyCtrl.unitController.AddUnitData(spawnUnitData, spawnUnitCount, true);
    }
}
