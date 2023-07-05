using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureColonyShip : Structure {

    public void Spawn() {
        var spawnComplete = this as M8.IPoolSpawnComplete;
        if(spawnComplete != null)
            spawnComplete.OnSpawnComplete();
    }

    void Awake() {
        //special case since we are not spawned from pool
        var init = this as M8.IPoolInit;
        if(init != null)
            init.OnInit();
    }
}
