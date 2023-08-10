using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "hotspot", menuName = "Game/Hotspot")]
public class HotspotData : ScriptableObject {
    [System.Serializable]
    public struct AtmosphereInfo {
        public SeasonData season;
        public AtmosphereStat[] stats;
        [Tooltip("Length of daylight relative to cycle duration (e.g. vernal equinox: 0.5)")]
        public float daylightScale;
    }

    [Header("Info")]
    [M8.Localize]
    public string nameRef;

    public ClimateData climate;

    public AtmosphereInfo[] atmosphereInfos;

    [Header("Inspection")]
    public LandscapePreviewTelemetry landscapePrefab;
        
    public M8.SceneAssetPath colonyScene; //if viable, this is the scene to load when launching

    public int GetAtmosphereInfoIndex(SeasonData season) {
        for(int i = 0; i < atmosphereInfos.Length; i++) {
            if(atmosphereInfos[i].season == season)
                return i;
        }

        return -1;
    }

    public AtmosphereStat[] GetAtmosphereStats(SeasonData season) {
        for(int i = 0; i < atmosphereInfos.Length; i++) {
            if(atmosphereInfos[i].season == season)
                return atmosphereInfos[i].stats;
        }

        return null;
    }

    public AtmosphereModifier[] GetRegionAtmosphereModifiers(int regionIndex) {
        return landscapePrefab && regionIndex >= 0 && regionIndex < landscapePrefab.regions.Length ? landscapePrefab.regions[regionIndex].atmosphereMods : null;
    }

    public float GetDaylightScale(SeasonData season) {
        for(int i = 0; i < atmosphereInfos.Length; i++) {
            var info = atmosphereInfos[i];
            if(info.season == season)
                return info.daylightScale;
        }

        return GameData.instance.cycleDaylightScaleDefault;
    }

    public AtmosphereStat[] GenerateModifiedStats(SeasonData season, int regionIndex) {
        var infoInd = GetAtmosphereInfoIndex(season);
        if(infoInd == -1)
            return null;

        var atmosphereInfo = atmosphereInfos[infoInd];

        var stats = new AtmosphereStat[atmosphereInfo.stats.Length];

        var regionMods = GetRegionAtmosphereModifiers(regionIndex);
        if(regionMods != null)
            AtmosphereModifier.Apply(stats, regionMods);

        return stats;
    }

    public void ApplyModifiedStats(AtmosphereStat[] stats, SeasonData season, int regionIndex) {
        var infoInd = GetAtmosphereInfoIndex(season);
        if(infoInd == -1)
            return;

        var atmosphereInfo = atmosphereInfos[infoInd];

        var regionMods = GetRegionAtmosphereModifiers(regionIndex);

        //override stats with base
        System.Array.Copy(atmosphereInfo.stats, stats, stats.Length);

        AtmosphereModifier.Apply(stats, regionMods);
    }
}
