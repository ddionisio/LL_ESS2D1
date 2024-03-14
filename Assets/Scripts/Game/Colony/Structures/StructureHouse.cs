using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureHouse : Structure {
    [Header("Landing Info")]
    public GameObject landingActiveGO; //disable upon touchdown
    public float landingDelay = 1.0f;
    public DG.Tweening.Ease landingEase = DG.Tweening.Ease.OutSine;

    [Header("Population FX")]
    public ParticleSystem popIncreaseFX;
    public ParticleSystem popDecreaseFX;

    [Header("Colony Ship Animation")]
    [M8.Animator.TakeSelector]
    public int takeLanding = -1;

    [Header("House SFX")]
    [M8.SoundPlaylist]
    public string sfxShipThruster;
    [M8.SoundPlaylist]
    public string sfxShipLand;
    [M8.SoundPlaylist]
    public string sfxPopIncrease;
    [M8.SoundPlaylist]
    public string sfxPopDecrease;

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

    //AI purpose for citizens to gather resources
    public bool isFoodGatherAvailable { get { return population < populationMax && foodCount < foodMax && mFoodGatherCount < (foodMax - foodCount); } }
    public bool isWaterGatherAvailable { get { return population < populationMax && waterCount < waterMax && mWaterGatherCount < (waterMax - waterCount); } }

    private M8.CacheList<Unit> mCitizensActive;

    private int mFoodCount;
    private int mWaterCount;

    private int mFoodGatherCount;
    private int mWaterGatherCount;

    private float mLastPowerRequiredTime;

    private ColonyController.ResourceQuota mPowerQuota;

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
            case StructureState.Moving:
                if(!string.IsNullOrEmpty(sfxShipLand))
                    M8.SoundPlaylist.instance.Play(sfxShipLand, false);

                if(landingActiveGO) landingActiveGO.SetActive(false);
                break;

            case StructureState.Active:
                break;
        }
    }

    protected override void ApplyCurrentState() {
        switch(state) {
            case StructureState.Spawning:
                if(activeGO) activeGO.SetActive(true);

                if(boxCollider) boxCollider.enabled = false;
                SetPlacementBlocker(true);

                mRout = StartCoroutine(DoSpawn());
                break;

            case StructureState.Moving:
                base.ApplyCurrentState();

                if(!string.IsNullOrEmpty(sfxShipThruster))
                    M8.SoundPlaylist.instance.Play(sfxShipThruster, false);

                if(landingActiveGO) landingActiveGO.SetActive(true);
                break;

            case StructureState.Active:
                base.ApplyCurrentState();

                mRout = StartCoroutine(DoActive());
                break;

            case StructureState.Destroyed:
                base.ApplyCurrentState();

                DecreasePopulation();
                break;

            case StructureState.None:
                base.ApplyCurrentState();

                if(ColonyController.isInstantiated) {
                    var colonyCtrl = ColonyController.instance;

                    colonyCtrl.population -= population;
                    colonyCtrl.populationCapacity -= populationMax;

                    if(mPowerQuota != null) {
                        colonyCtrl.RemoveResourceQuota(StructureResourceData.ResourceType.Power, mPowerQuota);
                        mPowerQuota = null;
                    }
                }
                else {
                    mPowerQuota = null;
                }

                populationLevelIndex = 0;
                population = 0;
                mFoodCount = 0;
                mWaterCount = 0;
                mFoodGatherCount = 0;
                mWaterGatherCount = 0;
                break;

            default:
                base.ApplyCurrentState();
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

        if(GameData.instance.signalUnitDespawned) GameData.instance.signalUnitDespawned.callback -= OnSignalUnitDespawned;
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

            //population = houseData.citizenStartCount;

            //colonyCtrl.populationCapacity += populationMax;
            //colonyCtrl.population += population;
        }
        else { //fail-safe
            if(mCitizensActive == null) mCitizensActive = new M8.CacheList<Unit>(0);

            population = 0;
        }

        populationLevelIndex = 0;

        if(GameData.instance.signalUnitDespawned) GameData.instance.signalUnitDespawned.callback += OnSignalUnitDespawned;
    }

    void OnUnitStateChanged(Unit unit) {
        switch(unit.state) {
            case UnitState.Death:
                //decrease citizen count (min: 1)
                DecreasePopulation();
                break;
        }
    }

    void OnSignalUnitDespawned(Unit unit) {
        if(unit.data == houseData.citizenData) {
            int ind = mCitizensActive.IndexOf(unit);
            if(ind != -1) {
                unit.stateChangedCallback -= OnUnitStateChanged;
                mCitizensActive.RemoveAt(ind);
            }
        }
    }

    IEnumerator DoActive() {
        var colonyCtrl = ColonyController.instance;

        var workerCount = Mathf.Min(houseData.citizenWorkerCapacity, population);

        var curSpawnTime = 0f;

        while(true) {
            //can't do anything during hazzard weather or when cycle is paused
            while(colonyCtrl.cycleController.isHazzard || !colonyCtrl.cycleAllowProgress)
                yield return null;

            //check if we need to spawn
            if(workerCount > mCitizensActive.Count) {
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
                if(population < populationMax)
                    UpdatePopulation();
                else if(GetStatusState(StructureStatus.Power) == StructureStatusState.Require && Time.time - mLastPowerRequiredTime >= houseData.lowPowerDecayDelay) {
                    //reduce population if max and is low power for a long time
                    DecreasePopulation();
                }
            }

            //update power consumption
            if(mPowerQuota != null) {
                var powerLastState = GetStatusState(StructureStatus.Power);

                if(!mPowerQuota.isFulfilled) {
                    SetStatusStateAndProgress(StructureStatus.Power, StructureStatusState.Require, Mathf.Clamp01(mPowerQuota.amountFulfilled / mPowerQuota.amount));

                    if(powerLastState != StructureStatusState.Require)
                        mLastPowerRequiredTime = Time.time;
                }
                else {
                    SetStatusState(StructureStatus.Power, StructureStatusState.None);
                    mLastPowerRequiredTime = Time.time;
                }
            }

            yield return null;
        }
    }

    IEnumerator DoSpawn() {
        var easeFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(landingEase);

        var screenExt = ColonyController.instance.mainCamera2D.screenExtent;

        var startPos = new Vector2(position.x, screenExt.yMax);
        var endPos = position;

        position = startPos;

        if(takeLanding != -1)
            animator.ResetTake(takeLanding);

        if(landingActiveGO) landingActiveGO.SetActive(true);

        if(!string.IsNullOrEmpty(sfxShipThruster))
            M8.SoundPlaylist.instance.Play(sfxShipThruster, false);

        var curTime = 0f;
        while(curTime < landingDelay) {
            yield return null;

            curTime += Time.deltaTime;

            var t = easeFunc(curTime, landingDelay, 0f, 0f);

            position = Vector2.Lerp(startPos, endPos, t);
        }

        if(landingActiveGO) landingActiveGO.SetActive(false);

        if(!string.IsNullOrEmpty(sfxShipLand))
            M8.SoundPlaylist.instance.Play(sfxShipLand, false);

        if(takeLanding != -1)
            yield return animator.PlayWait(takeLanding);

        mRout = null;

		//add initial population
		var colonyCtrl = ColonyController.instance;

		population = houseData.citizenStartCount;

		colonyCtrl.populationCapacity += populationMax;
		colonyCtrl.population += population;		
		//

		state = StructureState.Active;
    }

    private void DecreasePopulation() {
        if(population > 1) {
            population--;

            ColonyController.instance.population--;

            if(!string.IsNullOrEmpty(sfxPopDecrease)) M8.SoundPlaylist.instance.Play(sfxPopDecrease, false);

            if(popDecreaseFX) popDecreaseFX.Play();

            //also go back one level for requirements
            if(populationLevelIndex > population - 1) {
                populationLevelIndex = population - 1;

                //clamp values
                if(mFoodCount > foodMax) mFoodCount = foodMax;
                if(mWaterCount > waterMax) mWaterCount = waterMax;

                UpdatePowerQuota();
            }

            if(state == StructureState.Active)
                UpdatePopulation();
        }
    }

    private void UpdatePopulation() {
        bool isWaterComplete = waterMax == 0 || waterCount == waterMax, 
             isFoodComplete  = foodMax == 0 || foodCount == foodMax, 
             isPowerComplete = mPowerQuota == null || mPowerQuota.isFulfilled;

        //can't increase population if there's hazzard
        if(!ColonyController.instance.cycleController.cycleCurWeather.isHazzard && isWaterComplete && isFoodComplete && isPowerComplete) {
            if(population < populationMax) {
                population++;

                if(populationLevelIndex < houseData.populationLevelCount - 1) {
                    populationLevelIndex++;

                    UpdatePowerQuota();
                }

                mFoodCount = 0;
                mWaterCount = 0;

                ColonyController.instance.population++;

                if(!string.IsNullOrEmpty(sfxPopIncrease)) M8.SoundPlaylist.instance.Play(sfxPopIncrease, false);

                if(popIncreaseFX) popIncreaseFX.Play();
            }
        }

        //refresh status display
        if(population < populationMax && hitpointsCurrent == hitpointsMax) {
            var colonyCtrl = ColonyController.instance;
            var structureCtrl = colonyCtrl.structurePaletteController;

            //check if there are any food structures on the map, and update progress based on foodCount/foodMax
            if(foodCount < foodMax) {
                int foodSourceCount = structureCtrl.GetStructureActiveCount(GameData.instance.structureFoodSources);
                SetStatusStateAndProgress(StructureStatus.Food, foodSourceCount > 0 ? StructureStatusState.Progress : StructureStatusState.Require, Mathf.Clamp01((float)foodCount/foodMax));
            }
            else
                SetStatusState(StructureStatus.Food, StructureStatusState.None);

            //check if there are water reserves for the colony, and update progress based on waterCount/waterMax
            if(waterCount < waterMax) {
                int waterSourceCount = structureCtrl.GetStructureActiveCount(GameData.instance.structureWaterSources);
                SetStatusStateAndProgress(StructureStatus.Water, waterSourceCount > 0 ? StructureStatusState.Progress : StructureStatusState.Require, Mathf.Clamp01((float)waterCount / waterMax));
            }
            else
                SetStatusState(StructureStatus.Water, StructureStatusState.None);
        }
        else {
            SetStatusState(StructureStatus.Food, StructureStatusState.None);
            SetStatusState(StructureStatus.Water, StructureStatusState.None);
        }
    }

    private void UpdatePowerQuota() {
        var popInf = houseData.GetPopulationLevelInfo(populationLevelIndex);

        if(popInf.powerConsumption > 0f) {
            if(mPowerQuota == null)
                mPowerQuota = ColonyController.instance.AddResourceQuota(StructureResourceData.ResourceType.Power, popInf.powerConsumption);
            else {
                mPowerQuota.SetAmount(popInf.powerConsumption);
                ColonyController.instance.RefreshResources(StructureResourceData.ResourceType.Power);
            }
        }
        else if(mPowerQuota != null) {
            ColonyController.instance.RemoveResourceQuota(StructureResourceData.ResourceType.Power, mPowerQuota);
            mPowerQuota = null;
        }
    }
}
