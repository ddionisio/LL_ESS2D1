using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoLExt;

/// <summary>
/// Used for overworld preview
/// </summary>
public class LandscapePreviewTelemetry : MonoBehaviour {
    [System.Serializable]
    public class RegionInfo {
        public GameBounds2D bounds;
        public AtmosphereStat[] atmosphereModifiers;
    }

    public GameBounds2D bounds;
    public M8.RangeFloat altitudeRange; //relative to bounds, use with region's bounds to determine altitude display in preview

    public RegionInfo[] regions;
}
