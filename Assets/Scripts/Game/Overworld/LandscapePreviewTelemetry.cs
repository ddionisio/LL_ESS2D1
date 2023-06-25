using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using LoLExt;

/// <summary>
/// Used for overworld preview
/// </summary>
public class LandscapePreviewTelemetry : MonoBehaviour {
    [System.Serializable]
    public class RegionInfo {
        public Bounds bounds; //local to bounds
        public float altitudeOffset; //determine position of altitude relative to its bounds
        public AtmosphereModifier[] atmosphereMods;
    }

    public Bounds bounds; //local space
    public M8.RangeFloat altitudeRange; //relative to bounds, use with region's bounds to determine altitude display in preview

    public RegionInfo[] regions;

    public float GetAltitude(int regionIndex) {
        var region = regions[regionIndex];

        return altitudeRange.min + (((region.bounds.min.y + region.altitudeOffset) - bounds.min.y) / bounds.size.y) * altitudeRange.length;
    }
}
