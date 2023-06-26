using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AtmosphereAttributeBase : ScriptableObject {
    [Header("Info")]
    [M8.Localize]
    public string nameRef;

    public Sprite icon;

    [Header("Range Limit")]
    public bool isMinLimit;
    public bool isMaxLimit;
    public M8.RangeFloat rangeLimit;

    public abstract string symbolString { get; }

    public float ClampValue(float val) {
        if(isMinLimit && val < rangeLimit.min)
            val = rangeLimit.min;
        else if(isMaxLimit && val > rangeLimit.max)
            val = rangeLimit.max;

        return val;
    }

    public M8.RangeFloat ClampRange(M8.RangeFloat range) {
        return ClampRange(range.min, range.max);
    }

    public M8.RangeFloat ClampRange(float min, float max) {
        if(isMinLimit && min < rangeLimit.min)
            min = rangeLimit.min;

        if(isMaxLimit && max > rangeLimit.max)
            max = rangeLimit.max;

        if(min > max)
            min = max;

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
