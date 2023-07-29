using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour {
    public const string poolGroup = "unit";

    [Header("Spawn Info")]
    public Transform spawnRoot;

    /// <summary>
    /// All spawned units, treat this as a read-only.
    /// </summary>
    public M8.CacheList<Unit> unitActives { get { return mUnitActives; } }
    public M8.CacheList<Unit> unitAllyActives { get { return mUnitAllyActives; } }
    public M8.CacheList<Unit> unitEnemyActives { get { return mUnitEnemyActives; } }

    public delegate bool CheckUnitValid<T>(T structure) where T : Unit;

    private Dictionary<UnitData, M8.CacheList<Unit>> mUnitTypeActives; //use this to go through specific active unit type
    private M8.CacheList<Unit> mUnitActives; //use this to go through all active units
    private M8.CacheList<Unit> mUnitAllyActives; //use this to go through ally active units
    private M8.CacheList<Unit> mUnitEnemyActives; //use this to go through enemy active units

    private M8.PoolController mPoolCtrl;

    private bool mIsInit;

    private M8.GenericParams mUnitSpawnParms = new M8.GenericParams();

    public T GetUnitNearestActiveByData<T>(float positionX, UnitData unitData, CheckUnitValid<T> checkValid) where T : Unit {
        T ret = null;
        float dist = 0f;

        var activeList = GetUnitActivesByData(unitData);
        if(activeList != null) {
            for(int i = 0; i < activeList.Count; i++) {
                var unit = activeList[i] as T;
                if(unit && checkValid(unit)) {
                    //see if it's closer to our current available structure, or it's the first one
                    var structureDist = Mathf.Abs(unit.position.x - positionX);
                    if(!ret || structureDist < dist) {
                        ret = unit;
                        dist = structureDist;
                    }
                }
            }
        }

        return ret;
    }

    /// <summary>
    /// Spawned units by UnitData, treat this as a read-only.
    /// </summary>
    public M8.CacheList<Unit> GetUnitActivesByData(UnitData unitData) {
        M8.CacheList<Unit> ret;
        mUnitTypeActives.TryGetValue(unitData, out ret);
        return ret;
    }

    public Unit Spawn(UnitData unitData, Structure structureOwner, Vector2 position) {
        mUnitSpawnParms[UnitSpawnParams.structureOwner] = structureOwner;
        mUnitSpawnParms[UnitSpawnParams.spawnPoint] = position;

        return Spawn(unitData, mUnitSpawnParms);
    }

    public Unit Spawn(UnitData unitData, M8.GenericParams parms) {
        if(!mUnitTypeActives.ContainsKey(unitData)) { //fail-safe
            Debug.LogWarning("Trying to spawn unregistered unit data: " + unitData.name);
            return null;
        }

        parms[UnitSpawnParams.data] = unitData;

        var spawnTypeName = unitData.spawnPrefab.name;

        var spawnedUnit = mPoolCtrl.Spawn<Unit>(spawnTypeName, spawnTypeName, spawnRoot, parms);
        if(spawnedUnit) {
            mUnitTypeActives[unitData].Add(spawnedUnit);
            mUnitActives.Add(spawnedUnit);

            if(spawnedUnit.CompareTag(GameData.instance.unitAllyTag))
                mUnitAllyActives.Add(spawnedUnit);
            else if(spawnedUnit.CompareTag(GameData.instance.unitEnemyTag))
                mUnitEnemyActives.Add(spawnedUnit);

            GameData.instance.signalUnitSpawned?.Invoke(spawnedUnit);
        }

        return spawnedUnit;
    }

    /// <summary>
    /// Register and cache unit pool for spawning. If capacityExpand=true, expand existing cache by capacity, otherwise only expand if given capacity is larger
    /// </summary>
    public void AddUnitData(UnitData unitData, int capacity, bool capacityExpand) {
        if(capacity <= 0) return;

        if(!mIsInit) Init();

        //setup active list, expand if already exists
        M8.CacheList<Unit> cacheList;
        if(!mUnitTypeActives.TryGetValue(unitData, out cacheList)) {
            cacheList = new M8.CacheList<Unit>(capacity);
            mUnitTypeActives.Add(unitData, cacheList);
        }
        else {
            if(capacityExpand)
                cacheList.Expand(capacity);
            else if(cacheList.Capacity < capacity)
                cacheList.Resize(capacity);
        }

        if(capacityExpand) {
            mUnitActives.Expand(capacity);
            mUnitAllyActives.Expand(capacity);
            mUnitEnemyActives.Expand(capacity);
        }
        else if(mUnitActives.Capacity < capacity) {
            mUnitActives.Resize(capacity);
            mUnitAllyActives.Resize(capacity);
            mUnitEnemyActives.Resize(capacity);
        }

        //setup pool cache, expand if already exists
        if(!mPoolCtrl.AddType(unitData.spawnPrefab.gameObject, capacity, capacity)) {
            if(capacityExpand)
                mPoolCtrl.Expand(unitData.spawnPrefab.name, capacity);
            else {
                var curPoolCapacity = mPoolCtrl.CapacityCount(unitData.spawnPrefab.name);
                if(curPoolCapacity < capacity)
                    mPoolCtrl.Expand(unitData.spawnPrefab.name, capacity - curPoolCapacity);
            }
        }
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

            if(unit.CompareTag(GameData.instance.unitAllyTag))
                mUnitAllyActives.Remove(unit);
            else if(unit.CompareTag(GameData.instance.unitEnemyTag))
                mUnitEnemyActives.Remove(unit);

            GameData.instance.signalUnitDespawned?.Invoke(unit);
        }
    }

    private void Init() {
        mPoolCtrl = M8.PoolController.CreatePool(poolGroup);

        mPoolCtrl.despawnCallback += OnUnitDespawn;

        mUnitTypeActives = new Dictionary<UnitData, M8.CacheList<Unit>>();

        mUnitActives = new M8.CacheList<Unit>(0);
        mUnitAllyActives = new M8.CacheList<Unit>(0);
        mUnitEnemyActives = new M8.CacheList<Unit>(0);

        mIsInit = true;
    }
}