using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "hotspot", menuName = "Game/Hotspot")]
public class HotspotData : ScriptableObject {
    [Header("Info")]
    [M8.Localize]
    public string nameRef;

    public ClimateData climate;

    public AtmosphereStat[] atmosphereStats;

    [Header("Inspection")]
    public LandscapePreviewTelemetry landscapePrefab;
        
    public M8.SceneAssetPath colonyScene; //if viable, this is the scene to load when launching

    public AtmosphereStat[] GenerateModifiedStats(SeasonData season, int regionIndex) {
        var stats = new AtmosphereStat[atmosphereStats.Length];

        ApplyModifiedStats(stats, season, regionIndex);

        return stats;
    }

    public void ApplyModifiedStats(AtmosphereStat[] stats, SeasonData season, int regionIndex) {
        AtmosphereModifier[] seasonMods = climate ? climate.GetModifiers(season) : new AtmosphereModifier[0];

        AtmosphereModifier[] regionMods;

        if(landscapePrefab && regionIndex >= 0 && regionIndex < landscapePrefab.regions.Length)
            regionMods = landscapePrefab.regions[regionIndex].atmosphereMods;
        else
            regionMods = new AtmosphereModifier[0];

        //override stats with base
        System.Array.Copy(atmosphereStats, stats, atmosphereStats.Length);

        ApplyMods(stats, seasonMods);

        ApplyMods(stats, regionMods);
    }

    private void ApplyMods(AtmosphereStat[] stats, AtmosphereModifier[] mods) {
        for(int i = 0; i < mods.Length; i++) {
            var mod = mods[i];

            int statInd = -1;
            for(int j = 0; j < stats.Length; j++) {
                if(stats[j].atmosphere == mod.atmosphere) {
                    statInd = j;
                    break;
                }
            }

            if(statInd != -1) {
                var stat = stats[statInd];

                stat.range = mod.ApplyTo(stats[statInd].range);

                stats[statInd] = stat;
            }
        }
    }
}
