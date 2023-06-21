using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverworldOverlaySeasons : MonoBehaviour {
    [System.Serializable]
    public class SeasonInfo {
        public SeasonData data;
        public GameObject activeGO;

        public bool active {
            get { return activeGO ? activeGO.activeSelf : false; }
            set { if(activeGO) activeGO.SetActive(value); }
        }
    }

    [Header("Data")]
    public SeasonInfo[] seasons;

    [Header("Signal Listen")]
    public SignalSeasonData signalListenSeasonToggle;

    private int mCurSeasonIndex;

    void OnDisable() {
        if(signalListenSeasonToggle) signalListenSeasonToggle.callback -= OnSeasonToggle;
    }

    void OnEnable() {
        //hide everything initially
        for(int i = 0; i < seasons.Length; i++)
            seasons[i].active = false;

        if(signalListenSeasonToggle) signalListenSeasonToggle.callback += OnSeasonToggle;

        mCurSeasonIndex = -1;
    }

    void OnSeasonToggle(SeasonData season) {
        if(mCurSeasonIndex != -1)
            seasons[mCurSeasonIndex].active = false;

        mCurSeasonIndex = -1;
        for(int i = 0; i < seasons.Length; i++) {
            if(seasons[i].data == season) {
                mCurSeasonIndex = i;
                break;
            }
        }

        if(mCurSeasonIndex != -1)
            seasons[mCurSeasonIndex].active = true;
    }
}
