using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CycleResourceType {
    None,
    Sun,
    Wind,
    Water,
    Growth
}

[System.Serializable]
public struct CycleResource {
    public float sun;
    public float wind;
    public float water;
    public float growth;

    public static CycleResource operator +(CycleResource a, CycleResource b) {
        return new CycleResource {
            sun = a.sun + b.sun,
            wind = a.wind + b.wind,
            water = a.water + b.water,
            growth = a.growth + b.growth,
        };
    }

    public static CycleResource operator -(CycleResource a, CycleResource b) {
        return new CycleResource {
            sun = a.sun - b.sun,
            wind = a.wind - b.wind,
            water = a.water + b.water,
            growth = a.growth - b.growth,
        };
    }
}