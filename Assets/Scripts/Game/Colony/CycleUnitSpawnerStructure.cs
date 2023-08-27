using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycleUnitSpawnerStructure : CycleUnitSpawnerBase {
    private M8.CacheList<Structure> mStructureAvailable;

    protected override bool CanSpawn() {
        RefreshStructureAvailable();

        return mStructureAvailable.Count > 0;
    }

    protected override void ApplySpawnParams(M8.GenericParams parms) {        
        if(mStructureAvailable.Count > 0) { //fail-safe, should never get here if there's nothing available
            var structure = mStructureAvailable[Random.Range(0, mStructureAvailable.Count)];

            parms[UnitSpawnParams.spawnPoint] = structure.position;
            parms[UnitSpawnParams.structureTarget] = structure;
        }
    }

    protected override void Init() {
        base.Init();

        mStructureAvailable = new M8.CacheList<Structure>(ColonyController.instance.structurePaletteController.structureActives.Capacity);        
    }

    private void RefreshStructureAvailable() {
        mStructureAvailable.Clear();

        var unitTargetData = unitData as UnitTargetStructureData;
        if(unitTargetData) {
            var structures = ColonyController.instance.structurePaletteController.structureActives;
            for(int i = 0; i < structures.Count; i++) {
                var structure = structures[i];
                if(unitTargetData.IsTargetable(structure) && !IsStructureTargeted(structure))
                    mStructureAvailable.Add(structure);
            }
        }
    }

    private bool IsStructureTargeted(Structure structure) {
        for(int i = 0; i < spawnedUnits.Count; i++) {
            var unit = spawnedUnits[i] as UnitTargetStructure;
            if(unit.targetStructure == structure)
                return true;
        }

        return false;
    }
}