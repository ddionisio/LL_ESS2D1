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

    [Header("New House Info Display")]
    public GameObject newHouseInfoActiveGO;
    [M8.Localize]
    public string newHouseInfoTextRef;

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

    [Header("SFX")]
    [M8.SoundPlaylist]
    public string sfxPlacementEnter;
    [M8.SoundPlaylist]
    public string sfxPlacementExit;

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
                    if(playRootGO) playRootGO.SetActive(false);

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

                    CancelNewHouseInfo();
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

    private Coroutine mNewHouseInfoRout;
    private bool mNewHouseInfoSpeak;

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
        if(newHouseInfoActiveGO) newHouseInfoActiveGO.SetActive(false);
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
        if(playRootGO) playRootGO.SetActive(true);
        if(takePlayEnter != -1)
            yield return animator.PlayWait(takePlayEnter);

        var cycleCtrl = ColonyController.instance.cycleController;

        if(weatherForecastProgress)
            weatherForecastProgress.isPlay = true;

        if(weatherForecastOverlay)
            weatherForecastOverlay.SetCycleInfo(cycleCtrl.cycleCurWeather, cycleCtrl.atmosphereStats);
    }

    void OnCycleNext() {
        var cycleCtrl = ColonyController.instance.cycleController;

        if(weatherForecastOverlay)
            weatherForecastOverlay.SetCycleInfo(cycleCtrl.cycleCurWeather, cycleCtrl.atmosphereStats);
    }

    void OnCycleEnd() {
        if(weatherForecastProgress)
            weatherForecastProgress.isPlay = false;

        if(weatherForecastOverlay)
            weatherForecastOverlay.Clear();

        if(mIsPlacementActive) {
            //hide placement
            mIsPlacementActive = false;

            StartCoroutine(DoPlacementExit());
        }
        else {
            //hide palette
            StartCoroutine(DoPlayExit());
        }
    }

    void OnPlacementActive(bool active) {
        mIsPlacementActive = active;

        if(active) {
            if(!string.IsNullOrEmpty(sfxPlacementEnter))
                M8.SoundPlaylist.instance.Play(sfxPlacementEnter, false);

            //main stuff
            if(structureActionsWidget) structureActionsWidget.active = false;
            mStructureClicked = null;

            StartCoroutine(DoPlayToPlacement());

            CancelNewHouseInfo();
        }
        else {
            if(!string.IsNullOrEmpty(sfxPlacementExit))
                M8.SoundPlaylist.instance.Play(sfxPlacementExit, false);

            //placement stuff
            if(placementConfirmRoot) placementConfirmRoot.gameObject.SetActive(false);

            StartCoroutine(DoPlacementToPlay());

            ShowNewHouseInfo();
        }
    }

    IEnumerator DoPlayToPlacement() {
        yield return DoPlayExit();

        if(placementRootGO) placementRootGO.SetActive(true);
        if(takePlacementEnter != -1)
            animator.Play(takePlacementEnter);
    }

    IEnumerator DoPlacementToPlay() {
        yield return DoPlacementExit();

        if(playRootGO) playRootGO.SetActive(true);
        if(takePlayEnter != -1)
            animator.Play(takePlayEnter);
    }

    IEnumerator DoPlayExit() {
        if(takePlayExit != -1)
            yield return animator.PlayWait(takePlayExit);

        if(playRootGO) playRootGO.SetActive(false);
    }

    IEnumerator DoPlacementExit() {
        if(takePlacementExit != -1)
            yield return animator.PlayWait(takePlacementExit);

        if(placementRootGO) placementRootGO.SetActive(false);
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

        if(mNewHouseInfoRout == null)
            mNewHouseInfoSpeak = true;

        ShowNewHouseInfo();
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

                var screenPos = RectTransformUtility.WorldToScreenPoint(colonyCtrl.mainCamera, structure.clickPosition);

                structureActionsWidget.transform.position = screenPos;

                structureActionsWidget.active = true;

                colonyCtrl.Pause();
            }
            else { //cancels other structure
                mStructureClicked = null;

                if(structureActionsWidget.active) {
                    structureActionsWidget.active = false;

                    if(ColonyController.instance.timeState == ColonyController.TimeState.Pause)
                        ColonyController.instance.Resume();
                }
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

        if(structureActionsWidget) {
            structureActionsWidget.active = false;

            if(!ColonyController.instance.structurePaletteController.isPlacementActive) {
                if(ColonyController.instance.timeState == ColonyController.TimeState.Pause)
                    ColonyController.instance.Resume();
            }
        }

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

            if(structureActionsWidget && structureActionsWidget.active) {
                structureActionsWidget.active = false;

                if(ColonyController.instance.timeState == ColonyController.TimeState.Pause)
                    ColonyController.instance.Resume();
            }
        }
    }

    IEnumerator DoShowNewHouseInfo() {
        while(M8.ModalManager.main.IsInStack(LoLExt.ModalDialog.modalNameGeneric) || M8.ModalManager.main.isBusy)
            yield return null;

        if(!mIsPlacementActive) {
            if(newHouseInfoActiveGO) newHouseInfoActiveGO.SetActive(true);

            yield return new WaitForSeconds(0.3f);

            if(mNewHouseInfoSpeak) {
                if(!string.IsNullOrEmpty(newHouseInfoTextRef))
                    LoLExt.LoLManager.instance.SpeakText(newHouseInfoTextRef);
                mNewHouseInfoSpeak = false;
            }
        }

        mNewHouseInfoRout = null;
    }

    private void ShowNewHouseInfo() {
        if(mIsPlacementActive)
            return;

        if(mNewHouseInfoRout != null)
            return;

        //only show if available
        var structurePaletteCtrl = ColonyController.instance.structurePaletteController;

        var houseCount = structurePaletteCtrl.GetHouseCount();
        var houseCapacity = structurePaletteCtrl.GetHouseCapacity();

        if(structurePaletteCtrl.GetHouseCapacity() == 1) //don't show in the beginning
            return;

        if(houseCount < houseCapacity) {
            mNewHouseInfoRout = StartCoroutine(DoShowNewHouseInfo());
        }
    }

    private void CancelNewHouseInfo() {
        if(mNewHouseInfoRout != null) {
            StopCoroutine(mNewHouseInfoRout);
            mNewHouseInfoRout = null;
        }

        if(newHouseInfoActiveGO) newHouseInfoActiveGO.SetActive(false);
    }
}
