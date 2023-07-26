using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureHouse : Structure {
    [Header("House Signal Listen")]
    public SignalUnit signalListenUnitDespawned;

    public StructureHouseData houseData { get; private set; }

    /// <summary>
    /// Current PopulationLevelInfo index
    /// </summary>
    public int populationLevelIndex { get; private set; }

    public int population { get; private set; }

    public int populationMax { get { return houseData.citizenCapacity; } }

    public int foodCount { 
        get { return mFoodCount; }
        set {
            var _val = Mathf.Clamp(value, 0, foodMax);
            if(mFoodCount != _val) {
                mFoodCount = _val;

                if(state == StructureState.Active)
                    UpdatePopulation();
            }
        }
    }

    public int foodMax { get { return houseData.GetPopulationLevelInfo(populationLevelIndex).foodMax; } }

    public int waterCount { 
        get { return mWaterCount; }
        set {
            var _val = Mathf.Clamp(value, 0, waterMax);
            if(mWaterCount != _val) {
                mWaterCount = _val;

                if(state == StructureState.Active)
                    UpdatePopulation();
            }
        }
    }

    public int waterMax { get { return houseData.GetPopulationLevelInfo(populationLevelIndex).waterMax; } }

    /// <summary>
    /// [0, 1] gets filled gradually via (populationPowerConsumeDelay)
    /// </summary>
    public float power { get { return houseData.populationPowerConsumeDelay > 0f ? Mathf.Clamp01(mCurPowerConsumeTime / houseData.populationPowerConsumeDelay) : 1f; } }

    public float powerConsumptionRate { get { return houseData.GetPopulationLevelInfo(populationLevelIndex).powerConsumptionRate; } }

    //AI purpose for citizens to gather resources
    public bool isFoodGatherAvailable { get { return foodCount < foodMax && mFoodGatherCount < (foodMax - mFoodCount); } }
    public bool isWaterGatherAvailable { get { return waterCount < waterMax && mWaterGatherCount < (waterMax - waterCount); } }

    private M8.CacheList<Unit> mCitizensActive;

    private int mFoodCount;
    private int mWaterCount;

    private int mFoodGatherCount;
    private int mWaterGatherCount;

    private float mCurPowerConsumeTime;

    public void AddFoodGather() {
        if(foodCount < foodMax)
            mFoodGatherCount = Mathf.Clamp(mFoodGatherCount + 1, 0, foodMax - foodCount);
    }

    public void RemoveFoodGather() {
        if(mFoodGatherCount > 0)
            mFoodGatherCount--;
    }

    public void AddWaterGather() {
        if(waterCount < waterMax)
            mWaterGatherCount = Mathf.Clamp(mWaterGatherCount + 1, 0, waterMax - waterCount);
    }

    public void RemoveWaterGather() {
        if(mWaterGatherCount > 0)
            mWaterGatherCount--;
    }

    protected override void ClearCurrentState() {
        base.ClearCurrentState();

        switch(state) {
            case StructureState.Active:
                break;
        }
    }

    protected override void ApplyCurrentState() {
        base.ApplyCurrentState();

        switch(state) {
            case StructureState.Active:
                mRout = StartCoroutine(DoActive());
                break;

            case StructureState.Destroyed:
                mCurPowerConsumeTime = 0f;
                break;

            case StructureState.None:
                if(ColonyController.isInstantiated) {
                    var colonyCtrl = ColonyController.instance;

                    colonyCtrl.population -= population;
                    colonyCtrl.populationCapacity -= populationMax;
                }

                populationLevelIndex = 0;
                population = 0;
                mFoodCount = 0;
                mWaterCount = 0;
                mFoodGatherCount = 0;
                mWaterGatherCount = 0;
                mCurPowerConsumeTime = 0f;
                break;
        }
    }

    protected override void Init() {
        
    }

    protected override void Despawned() {
        //in the event we despawn houses during play
        for(int i = 0; i < mCitizensActive.Count; i++) {
            var unit = mCitizensActive[i];
            if(unit)
                unit.Despawn();
        }

        mCitizensActive.Clear();
        //

        houseData = null;

        if(signalListenUnitDespawned) signalListenUnitDespawned.callback -= OnSignalUnitDespawned;
    }

    protected override void Spawned() {
        var colonyCtrl = ColonyController.instance;

        houseData = data as StructureHouseData;

        if(houseData) {
            //initialize citizens data
            if(mCitizensActive != null) {
                //changed capacity?
                if(houseData.citizenCapacity > mCitizensActive.Capacity)
                    mCitizensActive.Resize(houseData.citizenCapacity);
            }
            else
                mCitizensActive = new M8.CacheList<Unit>(houseData.citizenCapacity);

            population = houseData.citizenStartCount;

            colonyCtrl.populationCapacity += populationMax;
            colonyCtrl.population += population;
        }
        else { //fail-safe
            if(mCitizensActive == null) mCitizensActive = new M8.CacheList<Unit>(0);

            population = 0;
        }

        populationLevelIndex = 0;

        if(signalListenUnitDespawned) signalListenUnitDespawned.callback += OnSignalUnitDespawned;
    }

    void OnUnitStateChanged(Unit unit) {
        switch(unit.state) {
            case UnitState.Death:
                //decrease citizen count (min: 1)
                if(population > 1) {
                    population--;

                    ColonyController.instance.population--;

                    //also go back one level for requirements
                    if(populationLevelIndex > population - 1) {
                        populationLevelIndex = population - 1;

                        //clamp values
                        if(mFoodCount > foodMax) mFoodCount = foodMax;
                        if(mWaterCount > waterMax) mWaterCount = waterMax;
                    }

                    if(state == StructureState.Active)
                        UpdatePopulation();
                }
                break;
        }
    }

    void OnSignalUnitDespawned(Unit unit) {
        int ind = mCitizensActive.IndexOf(unit);
        if(ind != -1) {
            unit.stateChangedCallback -= OnUnitStateChanged;
            mCitizensActive.RemoveAt(ind);
        }
    }

    IEnumerator DoActive() {
        UpdatePopulation();

        var colonyCtrl = ColonyController.instance;

        var curSpawnTime = 0f;

        while(true) {
            //can't do anything during hazzard weather
            while(colonyCtrl.cycleController.isHazzard)
                yield return null;

            //check if we need to spawn
            if(population > mCitizensActive.Count) {
                if(curSpawnTime < GameData.instance.structureUnitSpawnDelay) {
                    curSpawnTime += Time.deltaTime;
                }
                else {
                    //spawn
                    if(houseData.citizenData) {
                        var wp = GetWaypointRandom(GameData.structureWaypointSpawn, false);

                        var citizen = colonyCtrl.unitController.Spawn(houseData.citizenData, this, wp != null ? wp.groundPoint.position : position);

                        citizen.stateChangedCallback += OnUnitStateChanged;

                        mCitizensActive.Add(citizen);
                    }

                    curSpawnTime = 0f;
                }
            }
            else if(hitpointsCurrent == hitpointsMax) {
                if(population < populationMax) {
                    //update power consumption
                    if(powerConsumptionRate > 0f && power < 1f) {
                        var dt = Time.deltaTime;

                        var consumeAmt = powerConsumptionRate * dt;

                        var colonyPower = colonyCtrl.GetResourceAmount(StructureResourceData.ResourceType.Power);

                        if(colonyPower > consumeAmt) {
                            colonyPower -= consumeAmt;
                            colonyCtrl.SetResourceAmount(StructureResourceData.ResourceType.Power, colonyPower);

                            mCurPowerConsumeTime += dt;

                            SetStatusStateAndProgress(StructureStatus.Power, StructureStatusState.Progress, colonyPower);
                        }
                        else
                            SetStatusState(StructureStatus.Power, StructureStatusState.Require);
                    }
                    else
                        UpdatePopulation();
                }
            }

            yield return null;
        }
    }

    private void UpdatePopulation() {
        bool isWaterComplete = waterMax == 0 || waterCount == waterMax, 
             isFoodComplete  = foodMax == 0 || foodCount == foodMax, 
             isPowerComplete = powerConsumptionRate == 0 || power == 1f;

        if(isWaterComplete && isFoodComplete && isPowerComplete) {
            if(population < populationMax) {
                population++;

                if(populationLevelIndex < houseData.populationLevelCount - 1)
                    populationLevelIndex++;

                mFoodCount = 0;
                mWaterCount = 0;
                mCurPowerConsumeTime = 0f;

                ColonyController.instance.population++;
            }
        }

        //refresh status display
        if(population < populationMax && hitpointsCurrent == hitpointsMax) {
            var colonyCtrl = ColonyController.instance;
            var structureCtrl = colonyCtrl.structurePaletteController;

            //check if there are any food structures on the map, and update progress based on foodCount/foodMax
            if(foodCount < foodMax) {
                int foodSourceCount = structureCtrl.GetStructureActiveCount(houseData.foodStructureSources);
                SetStatusStateAndProgress(StructureStatus.Food, foodSourceCount > 0 ? StructureStatusState.Progress : StructureStatusState.Require, Mathf.Clamp01((float)foodCount/foodMax));
            }
            else
                SetStatusState(StructureStatus.Food, StructureStatusState.None);

            //check if there are water reserves for the colony, and update progress based on waterCount/waterMax
            if(waterCount < waterMax) {
                int waterSourceCount = structureCtrl.GetStructureActiveCount(houseData.waterStructureSources);
                SetStatusStateAndProgress(StructureStatus.Water, waterSourceCount > 0 ? StructureStatusState.Progress : StructureStatusState.Require, Mathf.Clamp01((float)waterCount / waterMax));
            }
            else
                SetStatusState(StructureStatus.Water, StructureStatusState.None);

            //check if there's any power left for the colony
            if(powerConsumptionRate > 0f && power < 1f) {
                var colonyPower = colonyCtrl.GetResourceAmount(StructureResourceData.ResourceType.Power);

                SetStatusStateAndProgress(StructureStatus.Power, colonyPower > 0f ? StructureStatusState.Progress : StructureStatusState.Require, power);
            }
            else
                SetStatusState(StructureStatus.Power, StructureStatusState.None);
        }
        else {
            SetStatusState(StructureStatus.Food, StructureStatusState.None);
            SetStatusState(StructureStatus.Water, StructureStatusState.None);
            SetStatusState(StructureStatus.Power, StructureStatusState.None);
        }
    }
}
