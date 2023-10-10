using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class ColonyController : GameModeController<ColonyController> {
    public enum TimeState {
        None, //disabled
        Normal, //time scale = 1
        FastForward, //time scale > 1
        Pause,
        CyclePause
    }

    public class ResourceQuota {
        public bool isFulfilled { get { return amount <= amountFulfilled; } }

        public float amount { get; private set; }
        public float amountFulfilled { get; private set; }

        public ResourceQuota(float aAmount) {
            amount = aAmount;
        }

        public void SetAmount(float aAmount) {
            amount = aAmount;
            if(amountFulfilled > amount)
                amountFulfilled = amount;
        }

        public void ClearFulfillment() {
            amountFulfilled = 0f;
        }

        public float UpdateFulfillment(float curTotalAmount) {
            var totalAmount = curTotalAmount;

            if(amount <= totalAmount) {
                amountFulfilled = amount;
                totalAmount -= amount;
            }
            else {
                amountFulfilled = amount - totalAmount;
                totalAmount = 0f;
            }

            return totalAmount;
        }
    }

    public class ResourceFixedAmount {
        public CycleResourceType inputType { get; private set; }
        public float amount { get { return mAmountBase * mAmountScale; } }

        private float mAmountBase;
        private float mAmountScale;

        public ResourceFixedAmount(float aAmount, CycleResourceType aInputType, float initialScale) {
            inputType = aInputType;
            mAmountBase = aAmount;
            mAmountScale = initialScale;
        }

        public void UpdateBase(float val) {
            mAmountBase = val;
        }

        public void UpdateScale(float scale) {
            mAmountScale = scale;
        }
    }

    public class ResourceInfo {
        public float amount { 
            get { return mAmountUpdated; }
            set {
                var amt = Mathf.Clamp(value, 0f, capacity);
                if(mAmount != amt) {
                    mAmount = amt;
                    Refresh();
                }
            }
        }

        public float capacity { 
            get { return mCapacity; } 
            set {
                if(mCapacity != value) {
                    mCapacity = value;

                    if(mAmount > mCapacity)
                        mAmount = mCapacity;

                    Refresh();
                }
            }
        }

        public float scale { get { return Mathf.Clamp01(amount / capacity); } }

        public bool isFull { get { return amount >= capacity; } }
                
        public List<ResourceFixedAmount> resourceFixed { get { return mResourceFixed; } }
        public List<ResourceQuota> resourceQuotas { get { return mResourceQuotas; } }

        private float mAmount;
        private float mAmountUpdated;
        private float mCapacity;

        private List<ResourceFixedAmount> mResourceFixed = new List<ResourceFixedAmount>(16);
        private List<ResourceQuota> mResourceQuotas = new List<ResourceQuota>(16);

        public ResourceFixedAmount AddFixed(float amt, CycleResourceType inputType, float initialScale) {
            var newFixed = new ResourceFixedAmount(amt, inputType, initialScale);
            mResourceFixed.Add(newFixed);
            Refresh();
            return newFixed;
        }

        public void RemoveFixed(ResourceFixedAmount fixedAmount) {
            if(mResourceFixed.Remove(fixedAmount))
                Refresh();
        }

        public ResourceQuota AddQuota(float quotaAmount) {
            var newQuota = new ResourceQuota(quotaAmount);
            mResourceQuotas.Add(newQuota);
            Refresh();
            return newQuota;
        }

        public void RemoveQuota(ResourceQuota quota) {
            if(mResourceQuotas.Remove(quota))
                Refresh();
        }

        public void Refresh() {
            mAmountUpdated = mAmount;

            for(int i = 0; i < mResourceFixed.Count; i++)
                mAmountUpdated += mResourceFixed[i].amount;

            for(int i = 0; i < mResourceQuotas.Count; i++) {
                var quota = mResourceQuotas[i];
                if(mAmountUpdated > 0f)
                    mAmountUpdated = quota.UpdateFulfillment(mAmountUpdated);
                else
                    quota.ClearFulfillment();
            }
        }
    }

    [Header("Setup")]
    public HotspotData hotspotData; //correlates to the hotspot we launched from the overworld
    //public CriteriaData criteriaData; //used to determine house req. params, correlates to hotspot group from the overworld

    public StructurePaletteData structurePalette;
    public UnitPaletteData unitPalette;

    [Header("Controllers")]
    public StructurePaletteController structurePaletteController;

    public UnitController unitController;
    public UnitPaletteController unitPaletteController;

    [Header("Landscape")]
    public Bounds bounds;
    public Transform regionRoot; //grab cycle controllers here

    [Header("Colony Ship")]
    public StructureColonyShip colonyShip;

    [Header("Audio")]
    [M8.MusicPlaylist]
    public string musicPlay;

    [Header("Signal Invoke")]
    public M8.Signal signalInvokePopulationUpdate;
    public M8.Signal signalInvokeResourceUpdate;

    [Header("Sequence")]
    [Tooltip("Use this for dialog, lessons, etc.")]
    public ColonySequenceBase sequence;
                
    [Header("Debug")]
    public bool debugEnabled;
    public SeasonData debugSeason;
    public int debugRegionIndex;

    public CycleController cycleController { get; protected set; }

    public Camera mainCamera { get; protected set; }
    public M8.Camera2D mainCamera2D { get; protected set; }
    public Transform mainCameraTransform { get; protected set; }

    public int population { 
        get { return mPopulation; }
        set {
            var _val = Mathf.Clamp(value, 0, mPopulationCapacity);
            if(mPopulation != _val) {
                var prevPopulation = mPopulation;
                mPopulation = _val;

                //update palettes
                if(mPopulation > prevPopulation) {
                    structurePaletteController.RefreshGroupInfos(mPopulation);
                    unitPaletteController.RefreshUnitInfos(mPopulation);
                }

                signalInvokePopulationUpdate?.Invoke();
            }
        }
    }

    public int populationCapacity { 
        get { return mPopulationCapacity; }
        set {
            if(mPopulationCapacity != value) {
                mPopulationCapacity = value;

                if(mPopulation > mPopulationCapacity)
                    mPopulation = mPopulationCapacity;

                signalInvokePopulationUpdate?.Invoke();
            }
        }
    }

    public TimeState timeState {
        get { return mTimeState; }
        private set {
            if(mTimeState != value) {
                var prevState = mTimeState;
                mTimeState = value;

                if(M8.SceneManager.isInstantiated) {
                    var sceneMgr = M8.SceneManager.instance;

                    switch(prevState) {
                        case TimeState.FastForward:
                            sceneMgr.timeScale = 1f;
                            break;

                        case TimeState.Pause:
                            sceneMgr.Resume();
                            break;

                        case TimeState.CyclePause:
                            cycleController.cycleTimeScale = 1f;
                            break;
                    }

                    switch(mTimeState) {
                        case TimeState.FastForward:
                            sceneMgr.timeScale = GameData.instance.fastForwardScale;
                            break;

                        case TimeState.Pause:
                            sceneMgr.Pause();
                            break;

                        case TimeState.CyclePause:
                            cycleController.cycleTimeScale = 0f;
                            break;
                    }

                    fastforwardChangedCallback?.Invoke(mTimeState);
                }
            }
        }
    }

    public bool cycleAllowProgress { 
        get {
            if(cycleController.cycleTimeScale > 0f)
                return true;

            return sequence && sequence.cyclePauseAllowProgress;
        } 
    }

    public event System.Action<TimeState> fastforwardChangedCallback;

    protected ResourceInfo[] mResources;

    protected int mPopulation;
    protected int mPopulationCapacity;

    protected TimeState mTimeState = TimeState.None;
    protected bool mIsHazzard;
    protected bool mIsCyclePause;

    protected M8.GenericParams mWeatherForecastParms = new M8.GenericParams();
    protected M8.GenericParams mVictoryParms = new M8.GenericParams();

    public void FastForward() {
        if(!mIsHazzard && !mIsCyclePause)
            timeState = TimeState.FastForward;
    }

    public void Pause() {
        timeState = TimeState.Pause;
    }

    public void Resume() {
        if(mIsHazzard)
            timeState = TimeState.None;
        else if(mIsCyclePause)
            timeState = TimeState.CyclePause;
        else
            timeState = TimeState.Normal;
    }

    public float GetResourceAmount(StructureResourceData.ResourceType resourceType) {
        return mResources[(int)resourceType].amount;
    }

    public void AddResourceAmount(StructureResourceData.ResourceType resourceType, float amount) {
        var resInd = (int)resourceType;

        mResources[resInd].amount += amount;

        //signal
        signalInvokeResourceUpdate?.Invoke();
    }

    public float GetResourceScale(StructureResourceData.ResourceType resourceType) {
        return mResources[(int)resourceType].scale;
    }

    public float GetResourceCapacity(StructureResourceData.ResourceType resourceType) {
        return mResources[(int)resourceType].capacity;
    }

    public void SetResourceCapacity(StructureResourceData.ResourceType resourceType, float capacity) {
        var resInd = (int)resourceType;

        if(mResources[resInd].capacity != capacity) {
            mResources[resInd].capacity = capacity;

            signalInvokeResourceUpdate?.Invoke();
        }
    }

    public bool IsResourceFull(StructureResourceData.ResourceType resourceType) {
        return mResources[(int)resourceType].isFull;
    }

    public ResourceFixedAmount AddResourceFixedAmount(StructureResourceData.ResourceType resourceType, CycleResourceType inputType, float amt) {
        var scale = cycleController.GetResourceScale(inputType);
        return mResources[(int)resourceType].AddFixed(amt, inputType, scale);
    }

    public void RemoveResourceFixedAmount(StructureResourceData.ResourceType resourceType, ResourceFixedAmount resourceFixedAmount) {
        mResources[(int)resourceType].RemoveFixed(resourceFixedAmount);
    }

    public ResourceQuota AddResourceQuota(StructureResourceData.ResourceType resourceType, float amt) {
        return mResources[(int)resourceType].AddQuota(amt);
    }

    public void RemoveResourceQuota(StructureResourceData.ResourceType resourceType, ResourceQuota resourceQuota) {
        mResources[(int)resourceType].RemoveQuota(resourceQuota);
    }

    public void RefreshResources(StructureResourceData.ResourceType resourceType) {
        mResources[(int)resourceType].Refresh();
    }

    public void ShowWeatherForecast(bool initialCycle, bool pause) {
        if(initialCycle)
            mWeatherForecastParms[ModalWeatherForecast.parmCycleController] = cycleController;
        else
            mWeatherForecastParms[ModalWeatherForecast.parmCycleController] = null;

        mWeatherForecastParms[ModalWeatherForecast.parmCycleCurrentIndex] = cycleController.cycleCurIndex;

        mWeatherForecastParms[ModalWeatherForecast.parmPause] = pause;

        M8.ModalManager.main.Open(GameData.instance.modalWeatherForecast, mWeatherForecastParms);
    }

    protected override void OnInstanceDeinit() {
        if(sequence)
            sequence.Deinit();

        var gameDat = GameData.instance;

        if(gameDat.signalCycleNext) gameDat.signalCycleNext.callback -= OnCycleNext;

        timeState = TimeState.None;

        if(ColonyHUD.isInstantiated)
            ColonyHUD.instance.active = false;

        base.OnInstanceDeinit();
    }

    protected override void OnInstanceInit() {
        base.OnInstanceInit();

        var gameDat = GameData.instance;

        if(gameDat.disableSequence)
            sequence = null;

        mainCamera = Camera.main;
        if(mainCamera) {
            mainCamera2D = mainCamera.GetComponent<M8.Camera2D>();
            mainCameraTransform = mainCamera.transform.parent;
        }

        //initialize resource array
        var structureResVals = System.Enum.GetValues(typeof(StructureResourceData.ResourceType));
        mResources = new ResourceInfo[structureResVals.Length];
        for(int i = 0; i < mResources.Length; i++)
            mResources[i] = new ResourceInfo();

        //grab season and region info
        SeasonData season;
        int regionInd;

        if(gameDat.isProceed && !debugEnabled) {
            var seasonInd = gameDat.savedSeasonIndex;

            if(seasonInd >= 0 && seasonInd < GameData.instance.seasons.Length)
                season = GameData.instance.seasons[seasonInd];
            else
                season = debugSeason;

            regionInd = gameDat.savedRegionIndex;
        }
        else {
            season = debugSeason;
            regionInd = debugRegionIndex;
        }

        //determine landscape
        CycleController firstCycleController = null;

        for(int i = 0; i < regionRoot.childCount; i++) {
            var cycleCtrl = regionRoot.GetChild(i).GetComponent<CycleController>();
            if(!cycleCtrl)
                continue;

            if(!firstCycleController)
                firstCycleController = cycleCtrl;

            if(cycleCtrl.regionIndex == regionInd)
                cycleController = cycleCtrl;
            else
                cycleCtrl.gameObject.SetActive(false);
        }

        //setup cycle control
        if(!cycleController && firstCycleController) //no region index match, use first ctrl (fail-safe)
            cycleController = firstCycleController;

        cycleController.gameObject.SetActive(true);

        cycleController.Setup(hotspotData, season);
        //

        //setup structure control
        structurePaletteController.Setup(structurePalette);

        //setup unit control
        unitPaletteController.Setup(this);

        //setup structures (unit spawning, etc)
        for(int i = 0; i < structurePalette.groups.Length; i++) {
            var grp = structurePalette.groups[i];
            var capacity = grp.capacity;

            for(int j = 0; j < grp.structures.Length; j++) {
                var structureData = grp.structures[j].data;
                structureData.Setup(this, capacity);
            }
        }

        //setup colony ship
        colonyShip.Init(this);

        //setup signals
        if(gameDat.signalCycleNext) gameDat.signalCycleNext.callback += OnCycleNext;

        if(sequence)
            sequence.Init();
    }

    protected override IEnumerator Start() {
        yield return base.Start();

        if(!string.IsNullOrEmpty(musicPlay))
            M8.MusicPlaylist.instance.Play(musicPlay, true, true);

        ColonyHUD.instance.active = true;

        var gameDat = GameData.instance;

        gameDat.signalColonyStart?.Invoke();

        if(sequence)
            yield return sequence.Intro();

        //weather forecast
        ShowWeatherForecast(true, false);

        if(sequence)
            yield return sequence.Forecast();

        //wait for forecast to close
        while(M8.ModalManager.main.IsInStack(gameDat.modalWeatherForecast) || M8.ModalManager.main.isBusy)
            yield return null;

        if(sequence)
            yield return sequence.ColonyShipPreEnter();

        //colony ship enter
        colonyShip.Spawn();

        //wait for colony to be active
        while(colonyShip.state != StructureState.Active)
            yield return null;

        if(sequence)
            yield return sequence.ColonyShipPostEnter();

        yield return DoCycle();

        //victory
        if(gameDat.signalVictory) gameDat.signalVictory.Invoke();

        var houseCount = structurePaletteController.GetHouseCount();
        var houseCapacity = structurePaletteController.GetHouseMaxCapacity();
        var populationMaxCapacity = structurePaletteController.GetPopulationMaxCapacity();

        mVictoryParms[ModalVictory.parmPopulation] = population;
        mVictoryParms[ModalVictory.parmPopulationMax] = populationMaxCapacity;
        mVictoryParms[ModalVictory.parmHouse] = houseCount;
        mVictoryParms[ModalVictory.parmHouseMax] = houseCapacity;

        M8.ModalManager.main.Open(gameDat.modalVictory, mVictoryParms);

        //wait for victory to close
        while(M8.ModalManager.main.IsInStack(gameDat.modalVictory) || M8.ModalManager.main.isBusy)
            yield return null;

        //other things

        gameDat.totalPopulation += population;
        gameDat.totalPopulationCapacity += populationMaxCapacity;

        gameDat.ProgressNextToOverworld();
    }

    IEnumerator DoCycle() {
        mIsHazzard = false;

        cycleController.Begin();

        if(sequence)
            sequence.CycleBegin();
                
        int curCycleInd = cycleController.cycleCurIndex;
        bool curCycleIsDay = cycleController.cycleIsDay;

        while(cycleController.isRunning) {
            yield return null;

            if(mIsHazzard != cycleController.isHazzard) {
                mIsHazzard = cycleController.isHazzard;

                if(mIsHazzard) {
                    ColonyHUD.instance.paletteActive = false;

                    timeState = TimeState.None;
                }
                else {
                    ColonyHUD.instance.paletteActive = true;

                    timeState = TimeState.Normal;
                }
            }

            //check if we need to pause cycle
            if(!mIsHazzard) {
                var isCyclePause = (sequence && sequence.isPauseCycle);
                if(mIsCyclePause != isCyclePause) {
                    mIsCyclePause = isCyclePause;
                    timeState = mIsCyclePause ? TimeState.CyclePause : TimeState.Normal;
                }
            }

            //refresh resources if we are at next cycle or changing from day to night
            var updateResources = false;
            if(curCycleInd != cycleController.cycleCurIndex) {
                curCycleInd = cycleController.cycleCurIndex;
                updateResources = true;
            }
            if(curCycleIsDay != cycleController.cycleIsDay) {
                curCycleIsDay = cycleController.cycleIsDay;
                updateResources = true;
            }

            if(updateResources) {
                for(int i = 0; i < mResources.Length; i++) {
                    var res = mResources[i];
                    for(int j = 0; j < res.resourceFixed.Count; j++) {
                        var fixedRes = res.resourceFixed[j];
                        fixedRes.UpdateScale(cycleController.GetResourceScale(fixedRes.inputType));
                    }
                    res.Refresh();
                }
            }
        }

        timeState = TimeState.None;

        if(sequence)
            yield return sequence.CycleEnd();
    }

    void OnCycleNext() {
        if(sequence)
            sequence.CycleNext();
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.cyan;
        M8.Gizmo.DrawWireRect(bounds.center, 0f, bounds.extents);
    }
}