using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureColonyShip : Structure {
    [SerializeField]
    StructureColonyShipData _data;

    public override StructureAction actionFlags {
        get {
            return StructureAction.None;
        }
    }

    private bool mIsInit;

    public void Init(UnitController unitController) {
        if(mIsInit) return;

        _data.SetupUnitSpawns(unitController, 1);

        //special case since we are not spawned from pool
        var init = this as M8.IPoolInit;
        if(init != null)
            init.OnInit();

        mIsInit = true;
    }

    public void Spawn() {
        var spawn = this as M8.IPoolSpawn;
        if(spawn != null) {
            var parms = new M8.GenericParams();
            parms[StructureSpawnParams.spawnPoint] = position;
            parms[StructureSpawnParams.spawnNormal] = up;
            parms[StructureSpawnParams.data] = _data;

            spawn.OnSpawned(parms);
        }

        var spawnComplete = this as M8.IPoolSpawnComplete;
        if(spawnComplete != null)
            spawnComplete.OnSpawnComplete();
    }
}
