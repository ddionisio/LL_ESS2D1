using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct AtmosphereStat {
    public AtmosphereAttributeBase atmosphere;

    public M8.RangeFloat range;

    public float median {
        get {
            return range.min == range.max ? range.min : range.Lerp(0.5f);
        }
    }
}
