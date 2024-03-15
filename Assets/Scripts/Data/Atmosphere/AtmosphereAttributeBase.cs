using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AtmosphereAttributeBase : ScriptableObject {
    [Header("Info")]
    [M8.Localize]
    public string nameRef;

    public Sprite icon;
    public Sprite legendRange;
    public Color legendRangeMinColor = Color.black;
    public Color legendRangeMaxColor = Color.black;

    [Header("Range Limit")]
    public M8.RangeFloat rangeLimit;

    public abstract string symbolString { get; }

    public virtual string legendRangeString { get { return ""; } }

    public float ClampValue(float val) {
        if(val < rangeLimit.min)
            val = rangeLimit.min;
        else if(val > rangeLimit.max)
            val = rangeLimit.max;

        return val;
    }

    public M8.RangeFloat ClampRange(M8.RangeFloat range) {
        return ClampRange(range.min, range.max);
    }

    public M8.RangeFloat ClampRange(float min, float max) {
        if(min < rangeLimit.min)
            min = rangeLimit.min;
        else if(min > rangeLimit.max)
            min = rangeLimit.max;

        if(max > rangeLimit.max)
            max = rangeLimit.max;
        else if(max < rangeLimit.min)
            max = rangeLimit.min;

        if(min > max)
            min = max;
        else if(max < min)
            max = min;

        return new M8.RangeFloat() { min=min, max=max };
    }

    public string GetValueString(int val) {
        return string.Format("{0}{1}", val, symbolString);
    }

    public string GetValueRangeString(M8.RangeFloat range) {
        return GetValueRangeString(Mathf.RoundToInt(range.min), Mathf.RoundToInt(range.max));
    }

    public string GetValueRangeString(int minVal, int maxVal) {
        if(minVal >= maxVal)
            return GetValueString(minVal);
        else
            return string.Format("{0} - {1}{2}", minVal, maxVal, symbolString);
    }
}
