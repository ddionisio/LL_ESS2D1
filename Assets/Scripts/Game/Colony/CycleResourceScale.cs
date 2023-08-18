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
public struct CycleResourceScale {
    public float sunDay;
    public float sunNight;
    public float wind;
    public float water;
    public float growth;

    public static CycleResourceScale operator +(CycleResourceScale a, CycleResourceScale b) {
        return new CycleResourceScale {
            sunDay = a.sunDay + b.sunDay,
            sunNight = a.sunNight + b.sunNight,
            wind = a.wind + b.wind,
            water = a.water + b.water,
            growth = a.growth + b.growth,
        };
    }

    public static CycleResourceScale operator -(CycleResourceScale a, CycleResourceScale b) {
        return new CycleResourceScale {
            sunDay = a.sunDay - b.sunDay,
            sunNight = a.sunNight + b.sunNight,
            wind = a.wind - b.wind,
            water = a.water + b.water,
            growth = a.growth - b.growth,
        };
    }
}