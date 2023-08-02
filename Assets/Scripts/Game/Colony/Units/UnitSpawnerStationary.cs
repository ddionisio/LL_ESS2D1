using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitSpawnerStationary : Unit {
    [Header("Spawn Telemetry Info")]
    [SerializeField]
    Waypoint[] _spawnWaypoints;
    [SerializeField]
    bool _spawnIsGrounded;

    public int spawnedUnitCount { get { return mSpawnedUnits != null ? mSpawnedUnits.Count : 0; } }

    private M8.CacheList<Unit> mSpawnedUnits;

    private DirType mSpawnMoveDir = DirType.None;

    private M8.GenericParams mSpawnParms = new M8.GenericParams();

    protected override void ApplyCurrentState() {
        base.ApplyCurrentState();

        switch(state) {
            case UnitState.Idle:
                mRout = StartCoroutine(DoIdle());
                break;

            case UnitState.Act:
                mRout = StartCoroutine(DoSpawn());
                break;

            case UnitState.None:
                ClearSpawnedUnits();
                break;
        }
    }

    protected override void Spawned(M8.GenericParams parms) {
        mSpawnMoveDir = DirType.None;

        if(parms != null) {
            if(parms.ContainsKey(UnitSpawnParams.moveDirType))
                mSpawnMoveDir = parms.GetValue<DirType>(UnitSpawnParams.moveDirType);
        }

        var spawnDat = data as UnitSpawnerData;
        if(spawnDat && spawnDat.spawnUnitData) {
            if(mSpawnedUnits == null)
                mSpawnedUnits = new M8.CacheList<Unit>(spawnDat.spawnUnitCount);
            else if(mSpawnedUnits.Capacity < spawnDat.spawnUnitCount)
                mSpawnedUnits.Resize(spawnDat.spawnUnitCount);
        }

        for(int i = 0; i < _spawnWaypoints.Length; i++)
            _spawnWaypoints[i].RefreshWorldPoint(position);
    }

    IEnumerator DoIdle() {
        var spawnDat = data as UnitSpawnerData;
        if(!spawnDat) { //fail-safe
            mRout = null;
            yield break;
        }

        yield return null;

        //wait for delay and spawn availability
        while(spawnedUnitCount >= spawnDat.spawnUnitCount)
            yield return null;

        RestartStateTime();

        while(stateTimeElapsed < spawnDat.spawnDelay)
            yield return null;

        mRout = null;
        state = UnitState.Act;
    }

    IEnumerator DoSpawn() {
        var spawnDat = data as UnitSpawnerData;
        if(!(spawnDat && spawnDat.spawnUnitData && mSpawnedUnits != null)) { //fail-safe
            mRout = null;
            yield break;
        }

        yield return null;

        var unitCtrl = ColonyController.instance.unitController;

        //spawn a unit
        if(!mSpawnedUnits.IsFull) { //check just in case
            var wp = _spawnWaypoints[Random.Range(0, _spawnWaypoints.Length)];

            if(_spawnIsGrounded)
                mSpawnParms[UnitSpawnParams.spawnPoint] = wp.groundPoint.position;
            else
                mSpawnParms[UnitSpawnParams.spawnPoint] = wp.point;

            mSpawnParms[UnitSpawnParams.moveDirType] = mSpawnMoveDir;

            var unit = unitCtrl.Spawn(spawnDat.spawnUnitData, mSpawnParms);
            if(unit) {
                unit.poolCtrl.despawnCallback += OnUnitDespawned;
                mSpawnedUnits.Add(unit);
            }
        }

        //wait for animation
        if(mTakeActInd != -1) {
            while(animator.isPlaying)
                yield return null;
        }

        mRout = null;
        state = UnitState.Idle;
    }

    void OnUnitDespawned(M8.PoolDataController pdc) {
        for(int i = 0; i < mSpawnedUnits.Count; i++) {
            var unit = mSpawnedUnits[i];
            if(unit.poolCtrl == pdc) {
                unit.poolCtrl.despawnCallback -= OnUnitDespawned;
                mSpawnedUnits.RemoveAt(i);
                break;
            }
        }
    }

    private void ClearSpawnedUnits() {
        if(mSpawnedUnits != null) {
            for(int i = 0; i < mSpawnedUnits.Count; i++) {
                var unit = mSpawnedUnits[i];
                if(unit)
                    unit.Despawn();
            }

            mSpawnedUnits.Clear();
        }
    }
}
