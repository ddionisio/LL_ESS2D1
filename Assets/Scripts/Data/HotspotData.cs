using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "hotspot", menuName = "Game/Hotspot")]
public class HotspotData : ScriptableObject {
    [System.Serializable]
    public struct SeasonAtmosphereStat {
        public AtmosphereAttributeBase atmosphere;
        
        public M8.RangeFloat winterRange;
        public M8.RangeFloat springRange;
        public M8.RangeFloat summerRange;
        public M8.RangeFloat autumnRange;
    }

    [Header("Info")]
    [M8.Localize]
    public string nameRef;

    public ClimateData climate;

    [SerializeField]
    SeasonAtmosphereStat[] _atmosphereInfos;

    public float winterDaylightScale = 0.5f;
    public float springDaylightScale = 0.5f;
    public float summerDaylightScale = 0.5f;
    public float autumnDaylightScale = 0.5f;

    [Header("Inspection")]
    public LandscapePreviewTelemetry landscapePrefab;
        
    public M8.SceneAssetPath colonyScene; //if viable, this is the scene to load when launching

    private AtmosphereStat[][] mAtmosphereStats;

    public AtmosphereStat[] GetAtmosphereStats(SeasonData season) {
        var seasonInd = GameData.instance.GetSeasonIndex(season);
        if(seasonInd != -1) {
            AtmosphereStat[] stats = null;

            if(mAtmosphereStats == null)
                mAtmosphereStats = new AtmosphereStat[GameData.instance.seasons.Length][];
            else
                stats = mAtmosphereStats[seasonInd];

            if(stats == null) {
                stats = new AtmosphereStat[_atmosphereInfos.Length];

                switch(seasonInd) {
                    case GameData.seasonWinterIndex:
                        for(int i = 0; i < _atmosphereInfos.Length; i++)
                            stats[i] = new AtmosphereStat { atmosphere = _atmosphereInfos[i].atmosphere, range = _atmosphereInfos[i].winterRange };
                        break;

                    case GameData.seasonSpringIndex:
                        for(int i = 0; i < _atmosphereInfos.Length; i++)
                            stats[i] = new AtmosphereStat { atmosphere = _atmosphereInfos[i].atmosphere, range = _atmosphereInfos[i].springRange };
                        break;

                    case GameData.seasonSummerIndex:
                        for(int i = 0; i < _atmosphereInfos.Length; i++)
                            stats[i] = new AtmosphereStat { atmosphere = _atmosphereInfos[i].atmosphere, range = _atmosphereInfos[i].summerRange };
                        break;

                    case GameData.seasonAutumnIndex:
                        for(int i = 0; i < _atmosphereInfos.Length; i++)
                            stats[i] = new AtmosphereStat { atmosphere = _atmosphereInfos[i].atmosphere, range = _atmosphereInfos[i].autumnRange };
                        break;
                }

                mAtmosphereStats[seasonInd] = stats;
            }

            return stats;
        }

        return null;
    }

    public AtmosphereModifier[] GetRegionAtmosphereModifiers(int regionIndex) {
        return landscapePrefab && regionIndex >= 0 && regionIndex < landscapePrefab.regions.Length ? landscapePrefab.regions[regionIndex].atmosphereMods : null;
    }

    public float GetDaylightScale(SeasonData season) {
        var seasonInd = GameData.instance.GetSeasonIndex(season);
        switch(seasonInd) {
            case GameData.seasonWinterIndex:
                return winterDaylightScale;

            case GameData.seasonSpringIndex:
                return springDaylightScale;

            case GameData.seasonSummerIndex:
                return summerDaylightScale;

            case GameData.seasonAutumnIndex:
                return autumnDaylightScale;

            default:
                return GameData.instance.cycleDaylightScaleDefault;
        }
    }

    public AtmosphereStat[] GenerateModifiedStats(SeasonData season, int regionIndex) {
        var seasonStats = GetAtmosphereStats(season);
        if(seasonStats != null) {
            var stats = new AtmosphereStat[seasonStats.Length];
            System.Array.Copy(seasonStats, stats, stats.Length);

            var regionMods = GetRegionAtmosphereModifiers(regionIndex);
            if(regionMods != null)
                AtmosphereModifier.Apply(stats, regionMods);

            return stats;
        }

        return null;
    }

    public void ApplyModifiedStats(AtmosphereStat[] stats, SeasonData season, int regionIndex) {
        var seasonStats = GetAtmosphereStats(season);

        var regionMods = GetRegionAtmosphereModifiers(regionIndex);

        //override stats with base
        System.Array.Copy(seasonStats, stats, stats.Length);

        AtmosphereModifier.Apply(stats, regionMods);
    }
}
