using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverworldAtmosphereOverlay : MonoBehaviour {
    [System.Serializable]
    public class SeasonInfo {
        public SeasonData season;
        public GameObject activeGO;

        public bool active {
            get { return activeGO ? activeGO.activeSelf : false; }
            set { if(activeGO) activeGO.SetActive(value); }
        }
    }

    [Header("Data")]
    public AtmosphereAttributeBase data;

    [Header("Display")]
    public GameObject activeGO; //use for general toggle for this atmosphere (e.g. measure guide)
    public SeasonInfo[] seasons;

    [Header("Signal Listen")]
    public SignalAtmosphereAttribute signalListenAtmosphereToggle;
    public SignalSeasonData signalListenSeasonToggle;

    private bool mIsAtmosphereMatch;
    private int mCurSeasonIndex;

    void OnDestroy() {
        if(signalListenAtmosphereToggle) signalListenAtmosphereToggle.callback -= OnAtmosphereToggle;
        if(signalListenSeasonToggle) signalListenSeasonToggle.callback -= OnSeasonToggle;
    }

    void Awake() {
        //hide everything initially
        if(activeGO) activeGO.SetActive(false);

        for(int i = 0; i < seasons.Length; i++)
            seasons[i].active = false;

        if(signalListenAtmosphereToggle) signalListenAtmosphereToggle.callback += OnAtmosphereToggle;
        if(signalListenSeasonToggle) signalListenSeasonToggle.callback += OnSeasonToggle;

        mIsAtmosphereMatch = false;
        mCurSeasonIndex = -1;
    }

    void OnAtmosphereToggle(AtmosphereAttributeBase attr) {
        mIsAtmosphereMatch = data == attr;

        if(activeGO) activeGO.SetActive(mIsAtmosphereMatch);

        if(mCurSeasonIndex != -1)
            seasons[mCurSeasonIndex].active = mIsAtmosphereMatch;
    }

    void OnSeasonToggle(SeasonData season) {
        if(mCurSeasonIndex != -1)
            seasons[mCurSeasonIndex].active = false;

        mCurSeasonIndex = -1;
        for(int i = 0; i < seasons.Length; i++) {
            if(seasons[i].season == season) {
                mCurSeasonIndex = i;
                break;
            }
        }

        if(mCurSeasonIndex != -1)
            seasons[mCurSeasonIndex].active = mIsAtmosphereMatch;
    }
}
