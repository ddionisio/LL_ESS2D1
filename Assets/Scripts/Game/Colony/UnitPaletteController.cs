using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitPaletteController : MonoBehaviour {
    public struct UnitInfo {
        public UnitData data;
        public bool isHidden;
    }

    [Header("Signal Invoke")]
    public M8.Signal signalInvokeRefresh;

    public UnitPaletteData unitPalette { get; private set; }

    public int capacity {
        get { return mCapacity; }
        set {
            var newCapacity = Mathf.Clamp(value, 0, unitPalette.capacity);
            if(mCapacity != newCapacity) {                
                mCapacity = newCapacity;

                signalInvokeRefresh?.Invoke();
            }
        }
    }

    public int activeCount { get; private set; }

    public int queueCount { get { return mUnitSpawnQueue.Count; } }

    public bool isFull {
        get {
            return activeCount + mUnitSpawnQueue.Count >= mCapacity;
        }
    }

    private UnitController mUnitCtrl;

    private UnitInfo[] mUnitInfos;

    private UnitDespawnComparer mUnitDespawnComparer = new UnitDespawnComparer();

    private List<UnitData> mUnitSpawnQueue = new List<UnitData>(32);

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
        int count = 0;
        for(int i = 0; i < mUnitSpawnQueue.Count; i++) {
            if(mUnitSpawnQueue[i] == unitData)
                count++;
        }
        return count;
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

        mUnitSpawnQueue.Clear();
    }

    public void SpawnQueue(UnitData unitData) {
        int ind = unitPalette.GetIndex(unitData);
        if(ind == -1) {
            Debug.LogWarning("Unit not found in palette: " + unitData.name);
            return;
        }

        mUnitSpawnQueue.Add(unitData);

        if(mSpawnQueueRout == null)
            mSpawnQueueRout = StartCoroutine(DoSpawnQueue());

        signalInvokeRefresh?.Invoke();
    }

    public void Despawn(UnitData unitData) {
        //remove from queue first
        if(mUnitSpawnQueue.Count > 0) {
            for(int i = mUnitSpawnQueue.Count - 1; i >= 0; i--) {
                var queue = mUnitSpawnQueue[i];
                if(queue == unitData) {
                    mUnitSpawnQueue.RemoveAt(i);
                    break;
                }
            }
        }
        else {
            int ind = unitPalette.GetIndex(unitData);
            if(ind == -1) {
                Debug.LogWarning("Unit not found in palette: " + unitData.name);
                return;
            }

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

            mUnitInfos[i] = new UnitInfo { data = paletteItm.data, isHidden = paletteItm.isHidden };
        }

        mCapacity = unitPalette.capacityStart;

        activeCount = 0;
    }

    void OnDisable() {
        ClearSpawnQueue();
    }

    IEnumerator DoSpawnQueue() {
        var spawnDelay = GameData.instance.unitPaletteSpawnDelay;

        while(true) {
            var curTime = 0f;
            while(curTime < spawnDelay) {                
                yield return null;
                curTime += Time.deltaTime;
            }

            if(mUnitSpawnQueue.Count > 0) {
                var unitData = mUnitSpawnQueue[0];
                mUnitSpawnQueue.RemoveAt(0);

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

                mUnitCtrl.Spawn(unitData, structureOwner, spawnPt);

                activeCount++;
                signalInvokeRefresh?.Invoke();
            }
            else
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
