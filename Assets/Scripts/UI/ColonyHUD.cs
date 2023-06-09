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

    [Header("Structure Action Display")]
    public StructureActionsWidget structureActionsWidget;

    //animations

    [Header("Signal Listen")]
    public M8.SignalBoolean signalListenPlacementActive;
    public M8.SignalBoolean signalListenPlacementClick;

    private bool mIsPlacementActive;

    private Structure mStructureClicked;

    public void PlacementAccept() {
        var colonyCtrl = ColonyController.instance;

        colonyCtrl.structureController.PlacementAccept();
    }

    public void PlacementCancel() {
        var colonyCtrl = ColonyController.instance;

        colonyCtrl.structureController.PlacementCancel();
    }

    void OnDestroy() {
        if(structureActionsWidget)
            structureActionsWidget.clickCallback -= OnStructureActionClick;

        if(signalListenPlacementActive) signalListenPlacementActive.callback -= OnPlacementActive;
        if(signalListenPlacementClick) signalListenPlacementClick.callback -= OnPlacementClick;

        if(GameData.isInstantiated) {
            var gameDat = GameData.instance;

            if(gameDat.signalStructureClick) gameDat.signalStructureClick.callback -= OnStructureClick;

            if(gameDat.signalCycleBegin) gameDat.signalCycleBegin.callback -= OnCycleBegin;
            if(gameDat.signalCycleEnd) gameDat.signalCycleEnd.callback -= OnCycleEnd;
        }
    }

    void Awake() {
        var gameDat = GameData.instance;

        if(mainRootGO) mainRootGO.SetActive(false);

        if(placementRootGO) placementRootGO.SetActive(false);
        if(placementConfirmRoot) placementConfirmRoot.gameObject.SetActive(false);

        if(structureActionsWidget) {
            structureActionsWidget.clickCallback += OnStructureActionClick;

            structureActionsWidget.active = false;
        }

        if(signalListenPlacementActive) signalListenPlacementActive.callback += OnPlacementActive;
        if(signalListenPlacementClick) signalListenPlacementClick.callback += OnPlacementClick;

        if(gameDat.signalStructureClick) gameDat.signalStructureClick.callback += OnStructureClick;

        if(gameDat.signalCycleBegin) gameDat.signalCycleBegin.callback += OnCycleBegin;
        if(gameDat.signalCycleEnd) gameDat.signalCycleEnd.callback += OnCycleEnd;
    }

    void OnPlacementActive(bool active) {
        mIsPlacementActive = active;

        if(active) {
            //main stuff
            if(mainRootGO) mainRootGO.SetActive(false);

            if(structureActionsWidget) structureActionsWidget.active = false;
            mStructureClicked = null;

            //placement stuff
            if(placementRootGO) placementRootGO.SetActive(true);

            //animation
        }
        else {
            //main stuff
            if(mainRootGO) mainRootGO.SetActive(true);

            //placement stuff
            if(placementRootGO) placementRootGO.SetActive(false);
            if(placementConfirmRoot) placementConfirmRoot.gameObject.SetActive(false);

            paletteStructureWidget.RefreshGroups();

            //animation
        }
    }

    void OnPlacementClick(bool isClick) {
        if(mIsPlacementActive) {
            if(placementConfirmRoot) {
                if(isClick) {
                    var colonyCtrl = ColonyController.instance;
                    var structureCtrl = colonyCtrl.structureController;

                    placementConfirmRoot.gameObject.SetActive(true);

                    var worldPos = structureCtrl.placementCursor.position;

                    //ensure the position is at least above the ghost display
                    var ghostPos = (Vector2)structureCtrl.placementCurrentGhost.transform.position;
                    var ghostBounds = structureCtrl.placementCurrentGhost.placementBounds;

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
                ColonyController.instance.structureController.PlacementStart(mStructureClicked);
                break;

            case StructureAction.Demolish:
                mStructureClicked.Demolish();
                break;
        }

        if(structureActionsWidget) structureActionsWidget.active = false;
        mStructureClicked = null;
    }

    void OnCycleBegin() {
        var colonyCtrl = ColonyController.instance;

        //initialize atmosphere info

        //initialize resource info

        //initialize palettes
        paletteStructureWidget.Setup(colonyCtrl.structurePalette);
        paletteStructureWidget.RefreshGroups();

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
