using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class ColonyController : GameModeController<ColonyController> {
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
    public GameObject regionRootGO; //grab cycle controllers here

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

    private ResourceInfo[] mResources;

    private int mPopulation;
    private int mPopulationCapacity;

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

    protected override void OnInstanceDeinit() {

        base.OnInstanceDeinit();
    }

    protected override void OnInstanceInit() {
        base.OnInstanceInit();

        var gameDat = GameData.instance;

        mainCamera = Camera.main;
        if(mainCamera)
            mainCameraTransform = mainCamera.transform.parent;

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

            if(seasonInd >= 0 && seasonInd < hotspotData.climate.seasons.Length)
                season = hotspotData.climate.seasons[seasonInd].season;
            else
                season = debugSeason;

            regionInd = gameDat.savedRegionIndex;
        }
        else {
            season = debugSeason;
            regionInd = debugRegionIndex;
        }

        //determine landscape
        var cycleCtrls = regionRootGO.GetComponentsInChildren<CycleController>(true);
        for(int i = 0; i < cycleCtrls.Length; i++) {
            var cycleCtrl = cycleCtrls[i];
            if(cycleCtrl.regionIndex == regionInd) {
                cycleController = cycleCtrl;

                cycleCtrl.gameObject.SetActive(true);
            }
            else
                cycleCtrl.gameObject.SetActive(false);
        }

        //setup cycle control
        if(!cycleController && cycleCtrls.Length > 0) { //no region index match, use first ctrl (fail-safe)
            cycleController = cycleCtrls[0];
            cycleController.gameObject.SetActive(true);
        }

        cycleController.Setup(hotspotData, season);

        //setup structure control
        structurePaletteController.Setup(structurePalette);

        //setup unit control
        unitPaletteController.Setup(unitController, unitPalette);

        //setup unit spawning for structures
        for(int i = 0; i < structurePalette.groups.Length; i++) {
            var grp = structurePalette.groups[i];
            var capacity = grp.capacity;

            for(int j = 0; j < grp.structures.Length; j++) {
                var structureData = grp.structures[j].data;
                structureData.SetupUnitSpawns(unitController, capacity);
            }
        }

        //setup colony ship
        colonyShip.Init(unitController);

        //setup signals
    }

    protected override IEnumerator Start() {
        yield return base.Start();

        //weather forecast

        //dialog, etc.

        StartCoroutine(DoCycle());
    }

    IEnumerator DoCycle() {
        //colony ship enter
        colonyShip.Spawn();

        //show hud

        cycleController.Begin();

        while(cycleController.isRunning)
            yield return null;

        //determine population count, reset cycle if player needs more population

        //hide hud

        //victory
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.green;
        M8.Gizmo.DrawWireRect(bounds.center, 0f, bounds.extents);
    }
}