using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColonyHUD : M8.SingletonBehaviour<ColonyHUD> {
    [Header("Display")]
    public GameObject mainRootGO;
    public GameObject playRootGO;

    [Header("Weather Display")]
    public WeatherForecastProgressWidget weatherForecastProgress;
    public WeatherForecastOverlayWidget weatherForecastOverlay;

    [Header("Placement Display")]
    public GameObject placementRootGO;
    public Transform placementConfirmRoot;

    [Header("Palette Display")]
    public GameObject paletteRootGO;
    public StructurePaletteWidget paletteStructureWidget;
    public UnitPaletteWidget paletteUnitWidget;

    [Header("Structure Action Display")]
    public StructureActionsWidget structureActionsWidget;

    [Header("Animation")]
    public M8.Animator.Animate animator;
    [M8.Animator.TakeSelector]
    public int takePlayEnter = -1;
    [M8.Animator.TakeSelector]
    public int takePlayExit = -1;
    [M8.Animator.TakeSelector]
    public int takePlacementEnter = -1;
    [M8.Animator.TakeSelector]
    public int takePlacementExit = -1;

    [Header("Signal Listen")]
    public M8.SignalBoolean signalListenPlacementActive;
    public M8.SignalBoolean signalListenPlacementClick;

    public SignalStructure signalListenStructureSpawned;
    public SignalStructure signalListenStructureDespawned;
    public M8.SignalInteger signalListenStructureGroupRefresh;

    public bool active {
        get { return mainRootGO ? mainRootGO.activeSelf : false; }
        set {
            if(active != value) {
                if(mainRootGO) mainRootGO.SetActive(value);

                var gameDat = GameData.instance;

                if(value) {         
                    if(placementRootGO) placementRootGO.SetActive(false);
                    mIsPlacementActive = false;

                    if(placementConfirmRoot) placementConfirmRoot.gameObject.SetActive(false);

                    if(structureActionsWidget) {
                        structureActionsWidget.clickCallback += OnStructureActionClick;

                        structureActionsWidget.active = false;
                    }

                    if(signalListenPlacementActive) signalListenPlacementActive.callback += OnPlacementActive;
                    if(signalListenPlacementClick) signalListenPlacementClick.callback += OnPlacementClick;

                    if(signalListenStructureSpawned) signalListenStructureSpawned.callback += OnStructureSpawned;
                    if(signalListenStructureDespawned) signalListenStructureDespawned.callback += OnStructureDespawned;
                    if(signalListenStructureGroupRefresh) signalListenStructureGroupRefresh.callback += OnStructureGroupRefresh;

                    if(signalListenUnitPaletteRefresh) signalListenUnitPaletteRefresh.callback += OnUnitPaletteRefresh;

                    if(gameDat.signalClickCategory) gameDat.signalClickCategory.callback += OnClickCategory;
                    if(gameDat.signalStructureClick) gameDat.signalStructureClick.callback += OnStructureClick;

                    if(gameDat.signalCycleBegin) gameDat.signalCycleBegin.callback += OnCycleBegin;
                    if(gameDat.signalCycleNext) gameDat.signalCycleNext.callback += OnCycleNext;
                    if(gameDat.signalCycleEnd) gameDat.signalCycleEnd.callback += OnCycleEnd;
                }
                else {
                    if(structureActionsWidget)
                        structureActionsWidget.clickCallback -= OnStructureActionClick;

                    if(signalListenPlacementActive) signalListenPlacementActive.callback -= OnPlacementActive;
                    if(signalListenPlacementClick) signalListenPlacementClick.callback -= OnPlacementClick;

                    if(signalListenStructureSpawned) signalListenStructureSpawned.callback -= OnStructureSpawned;
                    if(signalListenStructureDespawned) signalListenStructureDespawned.callback -= OnStructureDespawned;
                    if(signalListenStructureGroupRefresh) signalListenStructureGroupRefresh.callback -= OnStructureGroupRefresh;

                    if(signalListenUnitPaletteRefresh) signalListenUnitPaletteRefresh.callback -= OnUnitPaletteRefresh;

                    if(gameDat.signalClickCategory) gameDat.signalClickCategory.callback -= OnClickCategory;
                    if(gameDat.signalStructureClick) gameDat.signalStructureClick.callback -= OnStructureClick;

                    if(gameDat.signalCycleBegin) gameDat.signalCycleBegin.callback -= OnCycleBegin;
                    if(gameDat.signalCycleNext) gameDat.signalCycleNext.callback -= OnCycleNext;
                    if(gameDat.signalCycleEnd) gameDat.signalCycleEnd.callback -= OnCycleEnd;

                    mStructureClicked = null;

                    if(mSwitchModeRout != null) {
                        StopCoroutine(mSwitchModeRout);
                        mSwitchModeRout = null;
                    }
                }
            }
        }
    }

    public bool paletteActive {
        get { return paletteRootGO ? paletteRootGO.activeSelf : false; }
        set { if(paletteRootGO) paletteRootGO.SetActive(value); }
    }

    public M8.Signal signalListenUnitPaletteRefresh;

    private bool mIsPlacementActive;

    private Structure mStructureClicked;

    private Coroutine mSwitchModeRout;

    public void PlacementAccept() {
        var colonyCtrl = ColonyController.instance;

        colonyCtrl.structurePaletteController.PlacementAccept();
    }

    public void PlacementCancel() {
        var colonyCtrl = ColonyController.instance;

        colonyCtrl.structurePaletteController.PlacementCancel();
    }

    void Awake() {
        if(mainRootGO) mainRootGO.SetActive(false);
    }

    void OnCycleBegin() {
        var colonyCtrl = ColonyController.instance;

        //initialize atmosphere info
        if(weatherForecastProgress)
            weatherForecastProgress.Setup(colonyCtrl.cycleController.cycleData);

        //initialize resource info

        //initialize palettes
        paletteStructureWidget.Setup(colonyCtrl.structurePalette);
        paletteStructureWidget.RefreshGroups();

        paletteUnitWidget.Setup(colonyCtrl.unitPalette);
        paletteUnitWidget.RefreshInfo();

        StartCoroutine(DoEnter());
    }

    IEnumerator DoEnter() {
        if(takePlayEnter != -1)
            yield return animator.PlayWait(takePlayEnter);

        var cycleCtrl = ColonyController.instance.cycleController;

        if(weatherForecastProgress)
            weatherForecastProgress.Play();

        if(weatherForecastOverlay)
            weatherForecastOverlay.SetCycleInfo(cycleCtrl.cycleCurWeather, cycleCtrl.atmosphereStats);
    }

    void OnCycleNext() {
        var cycleCtrl = ColonyController.instance.cycleController;

        if(weatherForecastOverlay)
            weatherForecastOverlay.SetCycleInfo(cycleCtrl.cycleCurWeather, cycleCtrl.atmosphereStats);

        //hazzard
        if(cycleCtrl.isHazzard) {
            paletteActive = false;
        }
        else {
            paletteActive = true;
        }
    }

    void OnCycleEnd() {
        if(mIsPlacementActive) {
            if(placementRootGO) placementRootGO.SetActive(false);

            //hide placement

            mIsPlacementActive = false;
        }
        else {
            //hide palette
        }
    }

    void OnPlacementActive(bool active) {
        mIsPlacementActive = active;

        if(active) {
            //main stuff
            if(playRootGO) playRootGO.SetActive(false);

            if(structureActionsWidget) structureActionsWidget.active = false;
            mStructureClicked = null;

            //placement stuff
            if(placementRootGO) placementRootGO.SetActive(true);

            //animation
        }
        else {
            //main stuff
            if(playRootGO) playRootGO.SetActive(true);

            //placement stuff
            if(placementRootGO) placementRootGO.SetActive(false);
            if(placementConfirmRoot) placementConfirmRoot.gameObject.SetActive(false);

            //animation
        }
    }

    void OnPlacementClick(bool isClick) {
        if(mIsPlacementActive) {
            if(placementConfirmRoot) {
                if(isClick) {
                    var colonyCtrl = ColonyController.instance;
                    var placementInput = colonyCtrl.structurePaletteController.placementInput;

                    placementConfirmRoot.gameObject.SetActive(true);

                    var worldPos = placementInput.cursor.position;

                    //ensure the position is at least above the ghost display
                    var ghostPos = (Vector2)placementInput.currentGhost.transform.position;
                    var ghostBounds = placementInput.currentGhost.placementBounds;

                    if(worldPos.y < ghostPos.y + ghostBounds.max.y)
                        worldPos.y = ghostPos.y + ghostBounds.max.y;

                    var screenPos = RectTransformUtility.WorldToScreenPoint(colonyCtrl.mainCamera, worldPos);

                    placementConfirmRoot.position = screenPos;
                }
                else
                    placementConfirmRoot.gameObject.SetActive(false);
            }
        }
    }

    void OnStructureGroupRefresh(int groupIndex) {
        paletteStructureWidget.RefreshGroup(groupIndex);

        paletteStructureWidget.ClearGroupActive();

        //TODO: play flashy "update" to group in palette structure
    }

    void OnStructureClick(Structure structure) {
        if(!structureActionsWidget) return;

        if(mStructureClicked == structure) { //toggle
            mStructureClicked = null;
            structureActionsWidget.active = false;
        }
        else if(structure.isClicked) {
            structureActionsWidget.SetActionsActive(structure.actionFlags);

            if(structureActionsWidget.activeActionCount > 0) {
                mStructureClicked = structure;

                var colonyCtrl = ColonyController.instance;

                var screenPos = RectTransformUtility.WorldToScreenPoint(colonyCtrl.mainCamera, structure.overlayAnchorPosition);

                structureActionsWidget.transform.position = screenPos;
                structureActionsWidget.active = true;
            }
            else { //cancels other structure
                mStructureClicked = null;
                structureActionsWidget.active = false;
            }
        }
    }

    void OnStructureActionClick(StructureAction action) {
        if(!mStructureClicked) return;

        switch(action) {
            case StructureAction.Move:
                ColonyController.instance.structurePaletteController.PlacementStart(mStructureClicked);
                break;

            case StructureAction.Demolish:
                mStructureClicked.Demolish();
                break;

            case StructureAction.Cancel:
                mStructureClicked.CancelAction();
                break;
        }

        if(structureActionsWidget) structureActionsWidget.active = false;
        mStructureClicked = null;
    }

    void OnStructureSpawned(Structure structure) {
        paletteStructureWidget.RefreshGroup(structure.data);
    }

    void OnStructureDespawned(Structure structure) {
        paletteStructureWidget.RefreshGroup(structure.data);
    }

    void OnUnitPaletteRefresh() {
        paletteUnitWidget.RefreshInfo();
    }

    void OnClickCategory(int category) {
        if(category != GameData.clickCategoryStructure) {
            mStructureClicked = null;

            if(structureActionsWidget)
                structureActionsWidget.active = false;
        }
    }
}
