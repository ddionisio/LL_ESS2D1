using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class ColonyController : GameModeController<ColonyController> {

    [Header("Setup")]
    public HotspotData hotspotData; //correlates to the hotspot we launched from the overworld
    public CriteriaData criteriaData; //used to determine house req. params, correlates to hotspot group from the overworld

    public StructurePaletteData structurePalette;

    [Header("Controllers")]
    public StructureController structureController;

    [Header("Landscape")]
    public Bounds bounds;
    public GameObject regionRootGO; //grab cycle controllers here

    [Header("Colony Ship")]
    public StructureColonyShip colonyShip;
        
    [Header("Debug")]
    public bool debugEnabled;
    public SeasonData debugSeason;
    public int debugRegionIndex;

    public CycleController cycleController { get; private set; }
        
    public Camera mainCamera { get; private set; }
    public Transform mainCameraTransform { get; private set; }

    protected override void OnInstanceDeinit() {
        base.OnInstanceDeinit();
    }

    protected override void OnInstanceInit() {
        base.OnInstanceInit();

        var gameDat = GameData.instance;

        mainCamera = Camera.main;
        if(mainCamera)
            mainCameraTransform = mainCamera.transform.parent;

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
        structureController.Setup(structurePalette);
    }

    protected override IEnumerator Start() {
        yield return base.Start();

        //weather forecast

        //dialog, etc.

        StartCoroutine(DoCycle());
    }

    protected IEnumerator DoCycle() {
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