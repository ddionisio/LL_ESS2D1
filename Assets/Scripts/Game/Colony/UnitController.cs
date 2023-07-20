using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour {
    public const string poolGroup = "unit";

    [Header("Spawn Info")]
    public Transform spawnRoot;

    [Header("Signal Invoke")]
    public SignalUnit signalInvokeUnitSpawned;
    public SignalUnit signalInvokeUnitDespawned;

    /// <summary>
    /// All spawned units, treat this as a read-only.
    /// </summary>
    public M8.CacheList<Unit> unitActives { get { return mUnitActives; } }

    private Dictionary<UnitData, M8.CacheList<Unit>> mUnitTypeActives; //use this to go through specific active unit type
    private M8.CacheList<Unit> mUnitActives; //use this to go through all active units

    private M8.PoolController mPoolCtrl;

    private bool mIsInit;

    private M8.GenericParams mUnitSpawnParms = new M8.GenericParams();

    /// <summary>
    /// Spawned units by UnitData, treat this as a read-only.
    /// </summary>
    public M8.CacheList<Unit> GetUnitActivesByData(UnitData unitData) {
        M8.CacheList<Unit> ret;
        mUnitTypeActives.TryGetValue(unitData, out ret);
        return ret;
    }

    public Unit Spawn(UnitData unitData, Structure structureOwner, Vector2 position) {
        if(!mUnitTypeActives.ContainsKey(unitData)) { //fail-safe
            Debug.LogWarning("Trying to spawn unregistered unit data: " + unitData.name);
            return null;
        }

        mUnitSpawnParms[UnitSpawnParams.data] = unitData;
        mUnitSpawnParms[UnitSpawnParams.structureOwner] = structureOwner;
        mUnitSpawnParms[UnitSpawnParams.spawnPoint] = position;

        var spawnTypeName = unitData.spawnPrefab.name;

        var spawnedUnit = mPoolCtrl.Spawn<Unit>(spawnTypeName, spawnTypeName, spawnRoot, mUnitSpawnParms);
        if(spawnedUnit) {
            mUnitTypeActives[unitData].Add(spawnedUnit);
            mUnitActives.Add(spawnedUnit);

            signalInvokeUnitSpawned?.Invoke(spawnedUnit);
        }

        return spawnedUnit;
    }

    public void AddUnitData(UnitData unitData, int capacity) {
        if(capacity <= 0) return;

        if(!mIsInit) Init();

        //setup active list, expand if already exists
        M8.CacheList<Unit> cacheList;
        if(!mUnitTypeActives.TryGetValue(unitData, out cacheList)) {
            cacheList = new M8.CacheList<Unit>(capacity);
            mUnitTypeActives.Add(unitData, cacheList);
        }
        else
            cacheList.Expand(capacity);

        mUnitActives.Expand(capacity);

        //setup pool cache, expand if already exists
        if(!mPoolCtrl.AddType(unitData.spawnPrefab.gameObject, capacity, capacity))
            mPoolCtrl.Expand(unitData.spawnPrefab.name, capacity);
    }

    void OnUnitDespawn(M8.PoolDataController pdc) {
        Unit unit = null;

        for(int i = 0; i < mUnitActives.Count; i++) {
            if(mUnitActives[i].poolCtrl == pdc) {
                unit = mUnitActives[i];
                mUnitActives.RemoveAt(i);
                break;
            }
        }

        if(unit) {
            M8.CacheList<Unit> typeActives;
            if(mUnitTypeActives.TryGetValue(unit.data, out typeActives))
                typeActives.Remove(unit);

            signalInvokeUnitDespawned?.Invoke(unit);
        }
    }

    private void Init() {
        mPoolCtrl = M8.PoolController.CreatePool(poolGroup);

        mPoolCtrl.despawnCallback += OnUnitDespawn;

        mUnitTypeActives = new Dictionary<UnitData, M8.CacheList<Unit>>();

        mUnitActives = new M8.CacheList<Unit>(0);

        mIsInit = true;
    }
}