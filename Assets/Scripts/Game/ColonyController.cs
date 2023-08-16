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

    public struct ResourceInfo {
        public float amount {
            get { return mAmount; }
            set { mAmount = Mathf.Clamp(value, 0f, mCapacity); }
        }

        public float capacity {
            get { return mCapacity; }
            set {
                mCapacity = value;

                if(mAmount > mCapacity)
                    mAmount = mCapacity;
            }
        }

        public float scale { get { return Mathf.Clamp01(mAmount / mCapacity); } }

        public bool isFull { get { return mAmount >= mCapacity; } }

        private float mAmount;
        private float mCapacity;
    }

    [Header("Setup")]
    public HotspotData hotspotData; //correlates to the hotspot we launched from the overworld
    public CriteriaData criteriaData; //used to determine house req. params, correlates to hotspot group from the overworld

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

    [Header("Signal Invoke")]
    public M8.Signal signalInvokePopulationUpdate;
    public M8.Signal signalInvokeResourceUpdate;
        
    [Header("Debug")]
    public bool debugEnabled;
    public SeasonData debugSeason;
    public int debugRegionIndex;

    public CycleController cycleController { get; private set; }

    public Camera mainCamera { get; private set; }
    public M8.Camera2D mainCamera2D { get; private set; }
    public Transform mainCameraTransform { get; private set; }

    public int population { 
        get { return mPopulation; }
        set {
            var _val = Mathf.Clamp(value, 0, mPopulationCapacity);
            if(mPopulation != _val) {
                mPopulation = _val;

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
        set {
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

    public event System.Action<TimeState> fastforwardChangedCallback;

    private ResourceInfo[] mResources;

    private int mPopulation;
    private int mPopulationCapacity;

    private TimeState mTimeState = TimeState.None;

    private M8.GenericParams mWeatherForecastParms = new M8.GenericParams();

    public float GetResourceAmount(StructureResourceData.ResourceType resourceType) {
        return mResources[(int)resourceType].amount;
    }

    public void SetResourceAmount(StructureResourceData.ResourceType resourceType, float amount) {
        var resInd = (int)resourceType;

        if(mResources[resInd].amount != amount) {
            mResources[resInd].amount = amount;

            //signal
            signalInvokeResourceUpdate?.Invoke();
        }
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

        //setup structure control
        structurePaletteController.Setup(structurePalette);

        //setup unit control
        unitPaletteController.Setup(this);

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
    }

    protected override IEnumerator Start() {
        yield return base.Start();

        ColonyHUD.instance.active = true;

        GameData.instance.signalColonyStart?.Invoke();

        //dialog, etc.

        //weather forecast
        ShowWeatherForecast(true, false);
                
        //dialog, etc.

        yield return DoCycle();
                
        //victory
    }

    IEnumerator DoCycle() {
        //wait for forecast to close
        while(M8.ModalManager.main.IsInStack(GameData.instance.modalWeatherForecast) || M8.ModalManager.main.isBusy) {
            yield return null;
        }

        //colony ship enter
        colonyShip.Spawn();

        cycleController.Begin();

        while(cycleController.isRunning)
            yield return null;

        timeState = TimeState.None;

        //determine population count, reset cycle if player needs more population
    }

    void OnCycleNext() {
        //stop any interaction during hazzard event
        if(cycleController.isHazzard) {
            structurePaletteController.PlacementCancel();

            timeState = TimeState.None;
        }
        else {
            timeState = TimeState.Normal;
        }
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.cyan;
        M8.Gizmo.DrawWireRect(bounds.center, 0f, bounds.extents);
    }
}