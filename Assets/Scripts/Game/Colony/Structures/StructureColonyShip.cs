using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureColonyShip : Structure {
    [SerializeField]
    StructureColonyShipData _data;

    public StructureColonyShipData colonyShipData { get { return _data; } }

    public override StructureAction actionFlags {
        get {
            return StructureAction.None;
        }
    }

    private M8.CacheList<Unit> mUnitDyingList;
    private M8.CacheList<UnitMedic> mMedicActives;

    private bool mIsInit;

    public void Init(ColonyController colonyController) {
        if(mIsInit) return;

        _data.Setup(colonyController, 1);

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
        
    protected override void Init() {
        mUnitDyingList = new M8.CacheList<Unit>(64);
        mMedicActives = new M8.CacheList<UnitMedic>(_data.medicCapacity);

        //setup signals
        var gameDat = GameData.instance;

        if(gameDat.signalUnitDying) GameData.instance.signalUnitDying.callback += OnUnitDying;
        if(gameDat.signalUnitDespawned) gameDat.signalUnitDespawned.callback += OnUnitDespawned;
    }

    protected override void Spawned() {
        
    }
        
    void OnDestroy() {
        if(GameData.isInstantiated) {
            var gameDat = GameData.instance;

            if(gameDat.signalUnitDying) GameData.instance.signalUnitDying.callback -= OnUnitDying;
            if(gameDat.signalUnitDespawned) gameDat.signalUnitDespawned.callback -= OnUnitDespawned;
        }
    }

    void Update() {
        //check for dying units
        if(mUnitDyingList.Count > 0) {
            for(int i = 0; i < mUnitDyingList.Count; i++) {
                var unitDying = mUnitDyingList[i];
                if(unitDying.state != UnitState.Dying) { //this unit is no longer dying
                    mUnitDyingList.RemoveAt(i);
                    continue;
                }

                UnitMedic medicAssigned = null;

                for(int j = 0; j < mMedicActives.Count; j++) {
                    var medic = mMedicActives[j];

                    if(medic.targetUnit == unitDying) { //already targeted by a medic?
                        medicAssigned = medic;
                        break;
                    }
                    else if(!medic.targetUnit && (medic.state == UnitState.Idle || medic.state == UnitState.Move)) { //reassign medic to this unit
                        medic.targetUnit = unitDying;
                        medicAssigned = medic;
                        break;
                    }
                }

                if(!medicAssigned) {
                    //can spawn medic?
                    if(!mMedicActives.IsFull) {
                        var wp = GetWaypointRandom(GameData.structureWaypointSpawn, false);
                        var medic = (UnitMedic)ColonyController.instance.unitController.Spawn(_data.medicData, this, wp != null ? wp.groundPoint.position : position);

                        medic.targetUnit = unitDying;

                        mMedicActives.Add(medic);
                    }
                    else //wait for available medic
                        break;
                }
            }
        }
    }

    void OnUnitDying(Unit unit) {
        if(unit.CompareTag(GameData.instance.unitAllyTag))
            mUnitDyingList.Add(unit);
    }

    void OnUnitDespawned(Unit unit) {
        //check if it's one of those units we have in dying queue
        mUnitDyingList.Remove(unit);

        //remove from active medics if it's ours
        if(unit.data == _data.medicData)
            mMedicActives.Remove((UnitMedic)unit);
    }
}
