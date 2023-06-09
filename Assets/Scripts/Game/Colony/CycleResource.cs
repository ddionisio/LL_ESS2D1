using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct CycleResource {
    public float sun;
    public float wind;
    public float growth;

    public static CycleResource operator +(CycleResource a, CycleResource b) {
        return new CycleResource {
            sun = a.sun + b.sun,
            wind = a.wind + b.wind,
            growth = a.growth + b.growth,
        };
    }

    public static CycleResource operator -(CycleResource a, CycleResource b) {
        return new CycleResource {
            sun = a.sun - b.sun,
            wind = a.wind - b.wind,
            growth = a.growth - b.growth,
        };
    }
}
