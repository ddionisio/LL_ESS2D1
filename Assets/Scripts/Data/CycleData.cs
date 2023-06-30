using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "cycle", menuName = "Game/Cycle")]
public class CycleData : ScriptableObject {
    [System.Serializable]
    public class CycleItemInfo {
        public WeatherTypeData weather;
        public AtmosphereModifier[] atmosphereMods;
    }

    [System.Serializable]
    public struct DaylightScaleInfo {
        public SeasonData season;
        public float dayScale; //use 0.5 for vernal equinox
    }

    public HotspotData hotspotData;

    [Header("Daylight Info")]
    public DaylightScaleInfo[] daylightScales;
    public float daylightScaleDefault = 0.5f; //if no season match, just use this

    [Header("Cycles")]
    public CycleItemInfo[] cycles;
}
