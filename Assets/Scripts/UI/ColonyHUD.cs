using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColonyHUD : MonoBehaviour {
    [Header("Display")]
    public GameObject mainRootGO;

    [Header("Placement Display")]
    public GameObject placementRootGO;
    public RectTransform placementConfirmRoot;

    [Header("Palette Display")]
    public StructurePaletteWidget paletteStructureWidget;

    //animations

    [Header("Signal Listen")]
    public M8.SignalBoolean signalListenPlacementActive;

    private bool mIsPlacementActive;

    void OnDestroy() {
        if(signalListenPlacementActive) signalListenPlacementActive.callback -= OnPlacementActive;

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

        if(signalListenPlacementActive) signalListenPlacementActive.callback += OnPlacementActive;

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

            //animation
        }
    }

    void OnCycleBegin() {        
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
