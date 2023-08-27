using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycleUnitSpawnerStructureWaypoint : CycleUnitSpawnerBase {
    [Tooltip("Set to empty to grab any waypoint")]
    public string waypoint;
    [Tooltip("Set to empty to grab any structure")]
    public StructureData[] structureTypes;

    private M8.CacheList<Structure> mStructureAvailable;

    protected override bool CanSpawn() {
        RefreshStructureAvailable();

        return mStructureAvailable.Count > 0;
    }

    protected override void ApplySpawnParams(M8.GenericParams parms) {
        if(mStructureAvailable.Count > 0) { //fail-safe, should never get here if there's nothing available
            var structure = mStructureAvailable[Random.Range(0, mStructureAvailable.Count)];

            Waypoint wp;

            if(string.IsNullOrEmpty(waypoint))
                wp = structure.GetWaypointAny();
            else
                wp = structure.GetWaypointRandom(waypoint, false);

            parms[UnitSpawnParams.spawnPoint] = wp != null ? wp.groundPoint.position : structure.position;
            parms[UnitSpawnParams.structureTarget] = structure;
        }
    }

    protected override void Init() {
        base.Init();

        mStructureAvailable = new M8.CacheList<Structure>(ColonyController.instance.structurePaletteController.structureActives.Capacity);
    }

    private void RefreshStructureAvailable() {
        mStructureAvailable.Clear();

        var structures = ColonyController.instance.structurePaletteController.structureActives;
        for(int i = 0; i < structures.Count; i++) {
            var structure = structures[i];

            bool isValid = false;
            if(structureTypes.Length > 0) {
                for(int j = 0; j < structureTypes.Length; j++) {
                    if(structure.data == structureTypes[j]) {
                        isValid = true;
                        break;
                    }
                }
            }
            else if(!(structure is StructureColonyShip)) //exclude colony ship
                isValid = true;

            if(isValid)
                mStructureAvailable.Add(structure);
        }
    }
}