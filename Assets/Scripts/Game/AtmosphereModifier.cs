using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct AtmosphereModifier {
    public AtmosphereAttributeBase atmosphere;

    public bool isOverride;

    public M8.RangeFloat range;

    public M8.RangeFloat ApplyTo(M8.RangeFloat src) {
        return isOverride ? range : atmosphere.ClampRange(src.min + range.min, src.max + range.max);
    }

    public static void Apply(AtmosphereStat[] stats, AtmosphereModifier[] mods) {
        if(mods == null) return;

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
