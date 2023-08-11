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
    /// Check if otherRange is in our range
    /// 0 = neutral (intersects)
    /// -1 = bad (out of bounds)
    /// 1 = good (inside bounds)
    /// </summary>
    public int CheckBounds(M8.RangeFloat otherRange) {
        return CheckBounds(range, otherRange);
    }

    /// <summary>
    /// Check if given otherRange is within range
    /// 0 = neutral (intersects)
    /// -1 = bad (out of bounds)
    /// 1 = good (inside bounds)
    /// </summary>
    public static int CheckBounds(M8.RangeFloat range, M8.RangeFloat otherRange) {
        if(otherRange.max < range.min || otherRange.min > range.max)
            return -1;
        else if(otherRange.min >= range.min && otherRange.max <= range.max)
            return 1;

        return 0;
    }

    /// <summary>
    /// 0 = neutral (intersects)
    /// -1 = range is less than otherRange (out of bounds)
    /// 1 = range is greater than otherRange (out of bounds)
    /// </summary>
    public static int Compare(M8.RangeFloat range, M8.RangeFloat otherRange) {
        if(range.max < otherRange.min)
            return -1;

        if(range.min > otherRange.max)
            return 1;

        return 0;
    }
}
