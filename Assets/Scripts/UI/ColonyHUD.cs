using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColonyHUD : MonoBehaviour {
    [Header("Display")]
    public GameObject mainRootGO;

    [Header("Placement Display")]
    public GameObject placementRootGO;
    public Transform placementConfirmRoot;

    [Header("Palette Display")]
    public StructurePaletteWidget paletteStructureWidget;

    //animations

    [Header("Signal Listen")]    
    public M8.SignalBoolean signalListenPlacementActive;
    public M8.Signal signalListenPlacementClick;

    private bool mIsInit;
    private bool mIsPlacementActive;

    public void PlacementAccept() {
        var colonyCtrl = ColonyController.instance;

        colonyCtrl.structureController.PlacementAccept();
    }

    public void PlacementCancel() {
        var colonyCtrl = ColonyController.instance;

        colonyCtrl.structureController.PlacementCancel();
    }

    void OnDestroy() {
        if(signalListenPlacementActive) signalListenPlacementActive.callback -= OnPlacementActive;
        if(signalListenPlacementClick) signalListenPlacementClick.callback -= OnPlacementClick;

        if(GameData.isInstantiated) {
            var gameDat = GameData.instance;

            if(gameDat.signalCycleBegin) gameDat.signalCycleBegin.callback -= OnCycleBegin;
            if(gameDat.signalCycleEnd) gameDat.signalCycleEnd.callback -= OnCycleEnd;
        }
    }

    void Awake() {
        var gameDat = GameData.instance;

        if(mainRootGO) mainRootGO.SetActive(false);

        if(placementRootGO) placementRootGO.SetActive(false);
        if(placementConfirmRoot) placementConfirmRoot.gameObject.SetActive(false);

        if(signalListenPlacementActive) signalListenPlacementActive.callback += OnPlacementActive;
        if(signalListenPlacementClick) signalListenPlacementClick.callback += OnPlacementClick;

        if(gameDat.signalCycleBegin) gameDat.signalCycleBegin.callback += OnCycleBegin;
        if(gameDat.signalCycleEnd) gameDat.signalCycleEnd.callback += OnCycleEnd;
    }

    void OnPlacementActive(bool active) {
        mIsPlacementActive = active;

        if(active) {
            if(mainRootGO) mainRootGO.SetActive(false);
            if(placementRootGO) placementRootGO.SetActive(true);

            //animation
        }
        else {
            if(mainRootGO) mainRootGO.SetActive(true);

            if(placementRootGO) placementRootGO.SetActive(false);
            if(placementConfirmRoot) placementConfirmRoot.gameObject.SetActive(false);

            //animation
        }
    }

    void OnPlacementClick() {
        if(!mIsPlacementActive) return;

        if(placementConfirmRoot) {
            var colonyCtrl = ColonyController.instance;

            placementConfirmRoot.gameObject.SetActive(true);

            placementConfirmRoot.position = colonyCtrl.structureController.placementCursor.position;
        }
    }

    void OnCycleBegin() {
        var colonyCtrl = ColonyController.instance;

        if(!mIsInit) {
            //initialize atmosphere info

            //initialize resource info

            //initialize palettes
            paletteStructureWidget.Setup(colonyCtrl.structurePalette);
            paletteStructureWidget.RefreshGroups();

            mIsInit = true;
        }

        if(mainRootGO) mainRootGO.SetActive(true);

        //animation
    }

    void OnCycleEnd() {
        if(mIsPlacementActive) {
            if(placementRootGO) placementRootGO.SetActive(false);

            //animation
        }
        else {
            if(mainRootGO) mainRootGO.SetActive(false);

            //animation
        }
    }
}
