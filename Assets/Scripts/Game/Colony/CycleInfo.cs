using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used for signals when cycle starts
/// </summary>
public struct CycleInfo {
    public WeatherTypeData weather; //which weather is active
    public AtmosphereStat[] atmosphereStats; //current stats based on cycle
    public float daylightScale;
    public float cycleDuration;
    public int cycleIndex;
    public int cycleCount;
}
