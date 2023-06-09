using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverworldOverlayAtmosphere : MonoBehaviour {
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

    [Header("Signal Listen")]
    public SignalAtmosphereAttribute signalListenAtmosphereToggle;

    public bool isMatch { get; private set; }

    void OnDisable() {
        if(signalListenAtmosphereToggle) signalListenAtmosphereToggle.callback -= OnAtmosphereToggle;
    }

    void OnEnable() {
        //hide everything initially
        if(activeGO) activeGO.SetActive(false);

        if(signalListenAtmosphereToggle) signalListenAtmosphereToggle.callback += OnAtmosphereToggle;

        isMatch = false;
    }

    void OnAtmosphereToggle(AtmosphereAttributeBase attr) {
        isMatch = data == attr;

        if(activeGO) activeGO.SetActive(isMatch);
    }
}
