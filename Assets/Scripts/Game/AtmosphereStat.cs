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

    /// <summary>
    /// 0 = neutral (intersects)
    /// -1 = bad (out of bounds)
    /// 1 = good (inside bounds)
    /// </summary>
    public int Compare(M8.RangeFloat otherRange) {
        if(otherRange.max < range.min || otherRange.min > range.max)
            return -1;
        else if(otherRange.min >= range.min && otherRange.max <= range.max)
            return 1;

        return 0;
    }
}
