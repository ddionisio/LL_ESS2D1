using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AtmosphereAttributeBase : ScriptableObject {
    [Header("Info")]
    [M8.Localize]
    public string nameRef;

    public Sprite icon;

    public abstract string symbolString { get; }

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
