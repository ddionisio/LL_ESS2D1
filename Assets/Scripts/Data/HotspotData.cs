using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "hotspot", menuName = "Game/Hotspot")]
public class HotspotData : ScriptableObject {
    [System.Serializable]
    public struct DaylightInfo {
        public SeasonData season;
        public float daylightScale;
    }

    [Header("Info")]
    [M8.Localize]
    public string nameRef;

    public ClimateData climate;

    [Header("Atmosphere")]
    public AtmosphereStat[] atmosphereStats;

    [Header("Daylight")]
    public DaylightInfo[] daylightInfos;

    [Header("Inspection")]
    public LandscapePreviewTelemetry landscapePrefab;
        
    public M8.SceneAssetPath colonyScene; //if viable, this is the scene to load when launching

    public AtmosphereModifier[] GetRegionAtmosphereModifiers(int regionIndex) {
        return landscapePrefab && regionIndex >= 0 && regionIndex < landscapePrefab.regions.Length ? landscapePrefab.regions[regionIndex].atmosphereMods : null;
    }

    public float GetDaylightScale(SeasonData season) {
        for(int i = 0; i < daylightInfos.Length; i++) {
            var info = daylightInfos[i];
            if(info.season == season)
                return info.daylightScale;
        }

        return GameData.instance.cycleDaylightScaleDefault;
    }

    public AtmosphereStat[] GenerateModifiedStats(SeasonData season, int regionIndex) {
        var stats = new AtmosphereStat[atmosphereStats.Length];

        ApplyModifiedStats(stats, season, regionIndex);

        return stats;
    }

    public void ApplyModifiedStats(AtmosphereStat[] stats, SeasonData season, int regionIndex) {
        AtmosphereModifier[] seasonMods = climate ? climate.GetModifiers(season) : null;

        AtmosphereModifier[] regionMods = GetRegionAtmosphereModifiers(regionIndex);

        //override stats with base
        System.Array.Copy(atmosphereStats, stats, atmosphereStats.Length);

        AtmosphereModifier.Apply(stats, seasonMods);
        AtmosphereModifier.Apply(stats, regionMods);
    }
}
