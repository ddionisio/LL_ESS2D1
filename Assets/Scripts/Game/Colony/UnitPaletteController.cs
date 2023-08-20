using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitPaletteController : MonoBehaviour {
    public struct UnitInfo {
        public UnitData data;
        public bool isHidden;
        public bool isForcedShown;

        public int queueCount { get { return mQueueCount; } }

        public float queueTimeElapsed { get { return Time.time - mSpawnQueueLastTime; } }
        public float queueTimeScale { get { return Mathf.Clamp01(queueTimeElapsed / GameData.instance.unitPaletteSpawnDelay); } }
        
        public void AddQueue() {
            mQueueCount++;
            if(mQueueCount == 1)
                mSpawnQueueLastTime = Time.time;
        }

        public void RemoveQueue() {
            if(mQueueCount > 0)
                mQueueCount--;

            mSpawnQueueLastTime = Time.time;
        }

        public void ClearQueue() { mQueueCount = 0; }

        private int mQueueCount;
        private float mSpawnQueueLastTime;

        public UnitInfo(UnitPaletteData.UnitInfo inf) {
            data = inf.data;
            isHidden = inf.IsHidden(0);
            isForcedShown = false;
            mQueueCount = 0;
            mSpawnQueueLastTime = 0f;
        }
    }

    [Header("Signal Invoke")]
    public M8.Signal signalInvokeRefresh;

    public UnitPaletteData unitPalette { get; private set; }

    public int capacity { get { return mCapacity; } }

    public int activeCount { get; private set; }

    public int queueCount { 
        get {
            int count = 0;
            for(int i = 0; i < mUnitInfos.Length; i++)
                count += mUnitInfos[i].queueCount;
            return count;
        } 
    }

    public bool isFull {
        get {
            return activeCount + queueCount >= mCapacity;
        }
    }

    private UnitController mUnitCtrl;

    private UnitInfo[] mUnitInfos;

    private UnitDespawnComparer mUnitDespawnComparer = new UnitDespawnComparer();
        
    private int mCapacity;

    private Coroutine mSpawnQueueRout;

    public int GetUnitIndex(UnitData unitData) {
        return unitPalette.GetIndex(unitData);
    }
        
    public int GetActiveCountByType(UnitData unitData) {
        int count = 0;

        var activeUnits = mUnitCtrl.GetUnitActivesByData(unitData);
        if(activeUnits != null) {
            //don't count ones that are despawning
            for(int i = 0; i < activeUnits.Count; i++) {
                var unit = activeUnits[i];
                if(!(unit.state == UnitState.None || unit.state == UnitState.Despawning))
                    count++;
            }
        }

        return count;
    }

    public int GetSpawnQueueCountByType(UnitData unitData) {
        for(int i = 0; i < mUnitInfos.Length; i++) {
            var inf = mUnitInfos[i];
            if(inf.data == unitData)
                return inf.queueCount;
        }

        return 0;
    }

    public int GetSpawnQueueCount(int unitIndex) {
        return mUnitInfos[unitIndex].queueCount;
    }

    public float GetSpawnQueueTimeScale(int unitIndex) {
        return mUnitInfos[unitIndex].queueTimeScale;
    }

    public bool IsHidden(UnitData unitData) {
        return IsHidden(GetUnitIndex(unitData));
    }

    public bool IsHidden(int unitIndex) {
        return unitIndex >= 0 && unitIndex < mUnitInfos.Length ? mUnitInfos[unitIndex].isHidden : true;
    }

    public void ClearSpawnQueue() {
        if(mSpawnQueueRout != null) {
            StopCoroutine(mSpawnQueueRout);
            mSpawnQueueRout = null;
        }

        if(mUnitInfos != null) {
            for(int i = 0; i < mUnitInfos.Length; i++)
                mUnitInfos[i].ClearQueue();
        }
    }

    public void SpawnQueue(UnitData unitData) {
        int ind = unitPalette.GetIndex(unitData);
        if(ind == -1) {
            Debug.LogWarning("Unit not found in palette: " + unitData.name);
            return;
        }

        mUnitInfos[ind].AddQueue();

        if(mSpawnQueueRout == null)
            mSpawnQueueRout = StartCoroutine(DoSpawnQueue());

        signalInvokeRefresh?.Invoke();
    }

    public void Despawn(UnitData unitData) {        
        int ind = unitPalette.GetIndex(unitData);
        if(ind == -1) {
            Debug.LogWarning("Unit not found in palette: " + unitData.name);
            return;
        }

        //remove from queue first
        if(mUnitInfos[ind].queueCount > 0) {
            mUnitInfos[ind].RemoveQueue();
        }
        else {
            var activeUnits = mUnitCtrl.GetUnitActivesByData(unitData);
            if(activeUnits == null) {
                Debug.LogWarning("Unit not found in palette: " + unitData.name);
                return;
            }

            if(activeUnits.Count > 0) {
                activeUnits.Sort(mUnitDespawnComparer); //grab the unit we most likely would want to despawn

                var unit = activeUnits[0];
                unit.Despawn();

                activeCount--;
            }
        }

        signalInvokeRefresh?.Invoke();
    }

    public void ForceShowUnit(UnitData unitData) {
        var ind = GetUnitIndex(unitData);
        if(ind != -1) {
            var inf = mUnitInfos[ind];
            inf.isForcedShown = true;
            inf.isHidden = false;

            mUnitInfos[ind] = inf;

            signalInvokeRefresh?.Invoke();
        }   
    }

    public void RefreshUnitInfos(int population) {
        var isUpdated = false;

        //update capacity if it's increased
        var newCapacity = unitPalette.GetCurrentCapacity(population);
        if(mCapacity < newCapacity) {
            mCapacity = newCapacity;
            isUpdated = true;
        }

        for(int i = 0; i < mUnitInfos.Length; i++) {
            var inf = mUnitInfos[i];

            //update if unit is now unlocked
            var isHiddenUpdate = !inf.isForcedShown && unitPalette.units[i].IsHidden(population);
            if(inf.isHidden && !isHiddenUpdate) {
                inf.isHidden = isHiddenUpdate;

                mUnitInfos[i] = inf;

                isUpdated = true;
            }
        }

        if(isUpdated)
            signalInvokeRefresh?.Invoke();
    }

    public void Setup(ColonyController colonyCtrl) {
        mUnitCtrl = colonyCtrl.unitController;

        unitPalette = colonyCtrl.unitPalette;

        int unitCapacity = unitPalette.capacity;

        //setup info, and add unit types
        int unitTypeCount = unitPalette.units.Length;

        mUnitInfos = new UnitInfo[unitTypeCount];

        for(int i = 0; i < unitTypeCount; i++) {
            var paletteItm = unitPalette.units[i];

            mUnitCtrl.AddUnitData(paletteItm.data, unitCapacity, true);
            paletteItm.data.Setup(colonyCtrl);

            mUnitInfos[i] = new UnitInfo(paletteItm);
        }

        mCapacity = unitPalette.GetCurrentCapacity(0);

        activeCount = 0;
    }

    void OnDisable() {
        ClearSpawnQueue();
    }

    IEnumerator DoSpawnQueue() {
        var spawnDelay = GameData.instance.unitPaletteSpawnDelay;

        while(true) {
            yield return null;

            int activeUnitQueueCount = 0;
            for(int i = 0; i < mUnitInfos.Length; i++) {
                var inf = mUnitInfos[i];

                if(inf.queueCount > 0) {
                    //ready to spawn?
                    if(inf.queueTimeElapsed >= spawnDelay) {
                        inf.RemoveQueue();

                        //spawn unit
                        var structureOwner = ColonyController.instance.colonyShip;
                        if(!structureOwner) {
                            Debug.LogWarning("Colony ship not found!");
                            continue;
                        }

                        Vector2 spawnPt;

                        var wp = structureOwner.GetWaypointRandom(GameData.structureWaypointSpawn, false);
                        if(wp != null)
                            spawnPt = wp.groundPoint.position;
                        else //fail-safe
                            spawnPt = structureOwner.position;

                        mUnitCtrl.Spawn(inf.data, structureOwner, spawnPt);

                        //refresh
                        mUnitInfos[i] = inf;

                        activeCount++;
                        signalInvokeRefresh?.Invoke();
                    }

                    activeUnitQueueCount++;
                }
            }

            if(activeUnitQueueCount == 0)
                break;
        }

        mSpawnQueueRout = null;
    }

    private class UnitDespawnComparer : IComparer<Unit> {
        private int UnitDespawnStateGetPriority(Unit unit) {
            switch(unit.state) {
                case UnitState.Despawning:
                    return 100;
                case UnitState.Dying:
                    return 99;
                case UnitState.RetreatToBase:
                    return 98;
                case UnitState.Hurt:
                    return 97;

                case UnitState.Idle:
                    return 96;
                case UnitState.Move:
                    return 95;
                case UnitState.Act:
                    return 94;

                case UnitState.Spawning:
                    return 93;

                default:
                    return 0;
            }
        }

        public int Compare(Unit a, Unit b) {
            if(a && b)
                return UnitDespawnStateGetPriority(a) - UnitDespawnStateGetPriority(b);
            else if(a)
                return 1;
            else if(b)
                return -1;

            return 0;
        }
    }
}
