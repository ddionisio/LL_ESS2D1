using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitPaletteController : MonoBehaviour {
    public struct UnitInfo {
        public UnitData data;
        public bool isHidden;
        public Coroutine rout;
    }

    [Header("Signal Invoke")]
    public M8.Signal signalInvokeRefresh;

    [Header("Signal Listen")]
    public SignalUnit signalListenUnitSpawned;
    public SignalUnit signalListenUnitDespawned;

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

    public bool isFull {
        get {
            return activeCount > mCapacity;
        }
    }

    private UnitController mUnitCtrl;

    private UnitInfo[] mUnitInfos;

    private UnitDespawnComparer mUnitDespawnComparer = new UnitDespawnComparer();

    private int mCapacity;

    public int GetUnitIndex(UnitData unitData) {
        return unitPalette.GetIndex(unitData);
    }
        
    public int GetActiveCountByType(UnitData unitData) {
        var activeUnits = mUnitCtrl.GetUnitActivesByData(unitData);
        return activeUnits != null ? activeUnits.Count : 0;
    }

    public int GetActiveCountByType(int unitIndex) {
        if(unitIndex < 0 || unitIndex >= mUnitInfos.Length) return 0;

        var activeUnits = mUnitCtrl.GetUnitActivesByData(mUnitInfos[unitIndex].data);
        return activeUnits != null ? activeUnits.Count : 0;
    }

    public bool IsHidden(UnitData unitData) {
        return IsHidden(GetUnitIndex(unitData));
    }

    public bool IsHidden(int unitIndex) {
        return unitIndex >= 0 && unitIndex < mUnitInfos.Length ? mUnitInfos[unitIndex].isHidden : true;
    }

    /// <summary>
    /// Check if this unit type is waiting for spawn/despawn
    /// </summary>
    public bool IsBusy(UnitData unitData) {
        return IsBusy(unitPalette.GetIndex(unitData));
    }

    /// <summary>
    /// Check if this unit type is waiting for spawn/despawn
    /// </summary>
    public bool IsBusy(int unitIndex) {
        return unitIndex >= 0 && unitIndex < mUnitInfos.Length ? mUnitInfos[unitIndex].rout != null : false;
    }

    public void Spawn(UnitData unitData) {
        int ind = unitPalette.GetIndex(unitData);
        if(ind == -1) {
            Debug.LogWarning("Unit not found in palette: " + unitData.name);
            return;
        }

        var structureOwner = ColonyController.instance.colonyShip;
        if(!structureOwner) {
            Debug.LogWarning("Colony ship not found!");
            return;
        }

        Vector2 spawnPt;

        var wp = structureOwner.GetWaypointRandom(GameData.structureWaypointSpawn);
        if(wp != null)
            spawnPt = wp.groundPoint.position;
        else //fail-safe
            spawnPt = structureOwner.position;

        var unit = mUnitCtrl.Spawn(unitData, structureOwner, spawnPt);

        mUnitInfos[ind].rout = StartCoroutine(WaitSpawn(ind, unit));
    }

    public void Despawn(UnitData unitData) {
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

            mUnitInfos[ind].rout = StartCoroutine(WaitDespawn(ind, unit));
        }
    }

    public void Setup(UnitController unitCtrl, UnitPaletteData aUnitPalette) {
        mUnitCtrl = unitCtrl;

        unitPalette = aUnitPalette;

        int unitCapacity = unitPalette.capacity;

        //setup info, and add unit types
        int unitTypeCount = unitPalette.units.Length;

        mUnitInfos = new UnitInfo[unitTypeCount];

        for(int i = 0; i < unitTypeCount; i++) {
            var paletteItm = unitPalette.units[i];

            unitCtrl.AddUnitData(paletteItm.data, unitCapacity);

            mUnitInfos[i] = new UnitInfo { data = paletteItm.data, isHidden = paletteItm.isHidden, rout = null };
        }

        mCapacity = unitPalette.capacityStart;

        activeCount = 0;
    }

    void OnDisable() {
        if(signalListenUnitSpawned) signalListenUnitSpawned.callback -= OnSignalUnitSpawned;
        if(signalListenUnitDespawned) signalListenUnitDespawned.callback -= OnSignalUnitDespawned;

        for(int i = 0; i < mUnitInfos.Length; i++) {
            if(mUnitInfos[i].rout != null) {
                StopCoroutine(mUnitInfos[i].rout);
                mUnitInfos[i].rout = null;
            }
        }
    }

    void OnEnable() {
        if(signalListenUnitSpawned) signalListenUnitSpawned.callback += OnSignalUnitSpawned;
        if(signalListenUnitDespawned) signalListenUnitDespawned.callback += OnSignalUnitDespawned;
    }

    void OnSignalUnitSpawned(Unit unit) {
        int ind = unitPalette.GetIndex(unit.data);
        if(ind != -1) {
            activeCount++;
            signalInvokeRefresh?.Invoke();
        }
    }

    void OnSignalUnitDespawned(Unit unit) {
        int ind = unitPalette.GetIndex(unit.data);
        if(ind != -1) {
            activeCount--;
            signalInvokeRefresh?.Invoke();
        }
    }

    IEnumerator WaitSpawn(int ind, Unit unit) {
        while(unit.state == UnitState.Spawning)
            yield return null;

        mUnitInfos[ind].rout = null;

        signalInvokeRefresh?.Invoke();
    }

    IEnumerator WaitDespawn(int ind, Unit unit) {
        while(unit.state != UnitState.None)
            yield return null;

        mUnitInfos[ind].rout = null;

        signalInvokeRefresh?.Invoke();
    }

    private class UnitDespawnComparer : IComparer<Unit> {
        private int UnitDespawnStateGetPriority(Unit unit) {
            switch(unit.state) {
                case UnitState.Despawning:
                    return 100;
                case UnitState.Dying:
                    return 99;
                case UnitState.Retreat:
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
