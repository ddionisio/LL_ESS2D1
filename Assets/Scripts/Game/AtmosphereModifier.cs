using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct AtmosphereModifier {
    public AtmosphereAttributeBase atmosphere;

    public bool isOverride;

    public M8.RangeFloat range;

    public M8.RangeFloat ApplyTo(M8.RangeFloat src) {
        return isOverride ? range : new M8.RangeFloat { min=src.min+range.min, max=src.max+range.max };
    }
}
