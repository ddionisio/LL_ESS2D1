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
        public Vector2 center; //local space
        public float altitudeOffset; //determine position of altitude relative to its bounds
        public AtmosphereModifier[] atmosphereMods;
    }

    public Bounds bounds = new Bounds(Vector3.zero, Vector3.one); //local space
    public M8.RangeFloat altitudeRange; //relative to bounds, use with region's bounds to determine altitude display in preview

    public RegionInfo[] regions;

    public float GetAltitude(int regionIndex) {
        var landscapePreviewSize = GameData.instance.landscapePreviewSize;

        var region = regions[regionIndex];

        var regionY = region.center.y - landscapePreviewSize.y * 0.5f + region.altitudeOffset;

        return altitudeRange.Lerp((regionY - bounds.min.y) / bounds.size.y);
    }

    public float GetAltitudeScale(float altitude) {
        return Mathf.Clamp01((altitude - bounds.min.y) / bounds.size.y);
    }

    void OnDrawGizmos() {
        var gameDat = GameData.instance;

        Vector2 center;
        var pts = new Vector2[4];

        //draw bounds
        Gizmos.color = gameDat.landscapePreviewBoundsColor;

        center = transform.position;

        pts[0] = new Vector2 { x = center.x + bounds.min.x, y = center.y + bounds.min.y };
        pts[1] = new Vector2 { x = center.x + bounds.min.x, y = center.y + bounds.max.y };
        pts[2] = new Vector2 { x = center.x + bounds.max.x, y = center.y + bounds.max.y };
        pts[3] = new Vector2 { x = center.x + bounds.max.x, y = center.y + bounds.min.y };

        Gizmos.DrawLine(pts[0], pts[1]);
        Gizmos.DrawLine(pts[1], pts[2]);
        Gizmos.DrawLine(pts[2], pts[3]);
        Gizmos.DrawLine(pts[3], pts[0]);

        //draw regions
        var regionExt = gameDat.landscapePreviewSize * 0.5f;

        Gizmos.color = gameDat.landscapePreviewRegionColor;

        for(int i = 0; i < regions.Length; i++) {
            var region = regions[i];

            center = (Vector2)transform.position + region.center;

            pts[0] = new Vector2 { x = center.x - regionExt.x, y = center.y - regionExt.y };
            pts[1] = new Vector2 { x = center.x - regionExt.x, y = center.y + regionExt.y };
            pts[2] = new Vector2 { x = center.x + regionExt.x, y = center.y + regionExt.y };
            pts[3] = new Vector2 { x = center.x + regionExt.x, y = center.y - regionExt.y };

            Gizmos.DrawLine(pts[0], pts[1]);
            Gizmos.DrawLine(pts[1], pts[2]);
            Gizmos.DrawLine(pts[2], pts[3]);
            Gizmos.DrawLine(pts[3], pts[0]);

            pts[0] = new Vector2 { x = center.x - regionExt.x, y = center.y - regionExt.y + region.altitudeOffset };
            pts[1] = new Vector2 { x = center.x + regionExt.x, y = center.y - regionExt.y + region.altitudeOffset };

            Gizmos.DrawLine(pts[0], pts[1]);
        }
    }
}
