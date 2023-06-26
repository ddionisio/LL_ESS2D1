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
}
