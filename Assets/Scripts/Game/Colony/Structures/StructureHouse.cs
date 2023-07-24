using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureHouse : Structure {

    [Header("House Signal Invoke")]
    public M8.Signal signalInvokePopulationChanged;

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

                UpdatePopulation();
            }
        }
    }

    public int waterMax { get { return houseData.GetPopulationLevelInfo(populationLevelIndex).waterMax; } }

    /// <summary>
    /// [0, 1] gets filled gradually via (populationPowerConsumeDelay)
    /// </summary>
    public float power { get { return Mathf.Clamp01(mCurPowerConsumeTime / houseData.populationPowerConsumeDelay); } }

    public float powerConsumptionRate { get { return houseData.GetPopulationLevelInfo(populationLevelIndex).powerConsumptionRate; } }

    private M8.CacheList<Unit> mCitizensActive;

    private int mFoodCount;
    private int mWaterCount;

    private float mCurPowerConsumeTime;

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
                populationLevelIndex = 0;
                population = 0;
                mFoodCount = 0;
                mWaterCount = 0;
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
    }

    protected override void Spawned() {
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
        }
        else { //fail-safe
            if(mCitizensActive == null) mCitizensActive = new M8.CacheList<Unit>(0);

            population = 0;
        }

        populationLevelIndex = 0;
    }

    void OnUnitStateChanged(Unit unit) {
        switch(unit.state) {
            case UnitState.Death:
                //decrease citizen count

                //remove from active
                break;
        }
    }

    void OnSignalUnitDespawned(Unit unit) {

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

                    curSpawnTime = 0f;
                }
            }
            else if(hitpointsCurrent == hitpointsMax) {
                if(population < populationMax) {
                    //update power consumption
                    if(powerConsumptionRate > 0f && power < 1f) {
                        var dt = Time.deltaTime;

                        var consumeAmt = powerConsumptionRate * dt;

                        if(colonyCtrl.power > consumeAmt) {
                            colonyCtrl.power -= consumeAmt;
                            
                            mCurPowerConsumeTime += dt;

                            SetStatusStateAndProgress(StructureStatus.Power, StructureStatusState.Progress, power);
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
        bool isWaterComplete = false, isFoodComplete = false, isPowerComplete = false;

        if(isWaterComplete && isFoodComplete && isPowerComplete) {
            if(population < populationMax) {
                population++;

                if(populationLevelIndex < houseData.populationLevelCount - 1)
                    populationLevelIndex++;

                mFoodCount = 0;
                mWaterCount = 0;
                mCurPowerConsumeTime = 0f;

                signalInvokePopulationChanged?.Invoke();
            }
        }

        //refresh status display
        if(population < populationMax && hitpointsCurrent == hitpointsMax) {
            var colonyCtrl = ColonyController.instance;
            var structureCtrl = colonyCtrl.structurePaletteController;

            if(foodMax > 0) {
                int foodSourceCount = structureCtrl.GetStructureActiveCount(houseData.foodStructureSources);
                SetStatusStateAndProgress(StructureStatus.Food, foodSourceCount > 0 ? StructureStatusState.Progress : StructureStatusState.Require, Mathf.Clamp01((float)foodCount/foodMax));
            }
            else
                SetStatusState(StructureStatus.Food, StructureStatusState.None);

            if(waterMax > 0) {
                int waterSourceCount = structureCtrl.GetStructureActiveCount(houseData.waterStructureSources);
                SetStatusStateAndProgress(StructureStatus.Water, waterSourceCount > 0 ? StructureStatusState.Progress : StructureStatusState.Require, Mathf.Clamp01((float)waterCount / waterMax));
            }
            else
                SetStatusState(StructureStatus.Water, StructureStatusState.None);

            if(powerConsumptionRate > 0f) {
                SetStatusStateAndProgress(StructureStatus.Power, colonyCtrl.power > 0f ? StructureStatusState.Progress : StructureStatusState.Require, power);
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
