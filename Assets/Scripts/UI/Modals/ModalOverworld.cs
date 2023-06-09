using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModalOverworld : M8.ModalController, M8.IModalPush, M8.IModalPop {
    public const string parmAtmosphereActives = "overworldAtmos"; //AtmosphereAttributeBase[], determines toggles to activate
    public const string parmAtmosphere = "overworldAtmosphere"; //AtmosphereAttributeBase (current selected/default)
    public const string parmSeason = "overworldSeason"; //SeasonData
    public const string parmCriteria = "overworldCriteria"; //CriteriaData

    [System.Serializable]
    public class AtmosphereOverlayInfo {
        public AtmosphereAttributeBase data;
        public GameObject activeGO;

        public void SetActive(AtmosphereAttributeBase atmosphere) {
            if(activeGO)
                activeGO.SetActive(data == atmosphere);
        }
    }

    [Header("Controls")]
    public AtmosphereAttributeSelectWidget atmosphereToggle;

    public SeasonSelectWidget seasonToggle;

    [Header("Display")]
    public CriteriaWidget criteriaDisplay;
    public AtmosphereOverlayInfo[] atmosphereOverlays; //used for measurement legend    

    [Header("Signal Listen")]
    public SignalAtmosphereAttribute signalListenAtmosphereToggle;

    void M8.IModalPop.Pop() {
        if(signalListenAtmosphereToggle) signalListenAtmosphereToggle.callback -= OnAtmosphereToggle;
    }

    void M8.IModalPush.Push(M8.GenericParams parms) {

        AtmosphereAttributeBase atmosphereSelected = null;

        if(parms != null) {            
            if(parms.ContainsKey(parmAtmosphere))
                atmosphereSelected = parms.GetValue<AtmosphereAttributeBase>(parmAtmosphere);

            //setup atmosphere overlay toggle controls
            if(parms.ContainsKey(parmAtmosphereActives)) {
                var atmosphereActives = parms.GetValue<AtmosphereAttributeBase[]>(parmAtmosphereActives);

                //index 0 is the 'none' overlay
                atmosphereToggle.Setup(atmosphereSelected, atmosphereActives);
            }

            //setup season toggle controls
            if(parms.ContainsKey(parmSeason)) {
                var curSeasonDat = parms.GetValue<SeasonData>(parmSeason);

                seasonToggle.Setup(curSeasonDat);
            }

            //setup criteria display
            if(parms.ContainsKey(parmCriteria)) {
                var criteriaDat = parms.GetValue<CriteriaData>(parmCriteria);

                criteriaDisplay.Setup(criteriaDat);
            }
        }

        for(int i = 0; i < atmosphereOverlays.Length; i++)
            atmosphereOverlays[i].SetActive(atmosphereSelected);

        if(signalListenAtmosphereToggle) signalListenAtmosphereToggle.callback += OnAtmosphereToggle;
    }

    void OnAtmosphereToggle(AtmosphereAttributeBase atmosphereAttribute) {
        for(int i = 0; i < atmosphereOverlays.Length; i++)
            atmosphereOverlays[i].SetActive(atmosphereAttribute);
    }
}
