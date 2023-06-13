using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "hotspot", menuName = "Game/Hotspot")]
public class HotspotData : ScriptableObject {
    [System.Serializable]
    public class SeasonInfo {
        public SeasonData season;
        public AtmosphereStat[] atmosphereModifiers;
    }

    [Header("Info")]
    [M8.Localize]
    public string nameRef;

    public ClimateData climate;

    public AtmosphereStat[] atmosphereStats;

    [Header("Inspection")]
    public GameObject landscapePrefab;

    public SeasonInfo[] seasons;
}
