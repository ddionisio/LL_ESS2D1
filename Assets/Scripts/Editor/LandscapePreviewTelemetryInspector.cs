using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using UnityEditor.IMGUI.Controls;

[CustomEditor(typeof(LandscapePreviewTelemetry))]
public class LandscapePreviewTelemetryInspector : Editor {
    BoxBoundsHandle mBoxHandle = new BoxBoundsHandle();

    void OnSceneGUI() {
        var gameDat = GameData.instance;

        var dat = target as LandscapePreviewTelemetry;
        if(dat == null)
            return;

        //edit bounds
        using(new Handles.DrawingScope(gameDat.landscapePreviewBoundsColor)) {
            mBoxHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;

            Bounds b = dat.bounds;

            mBoxHandle.center = new Vector3(b.center.x, b.center.y, 0f);
            mBoxHandle.size = new Vector3(b.size.x, b.size.y, 0f);

            EditorGUI.BeginChangeCheck();
            mBoxHandle.DrawHandle();
            if(EditorGUI.EndChangeCheck()) {
                Vector2 min = mBoxHandle.center - mBoxHandle.size * 0.5f;

                float _minX = Mathf.Round(min.x / gameDat.landscapePreviewBoundsEditSnap);
                float _minY = Mathf.Round(min.y / gameDat.landscapePreviewBoundsEditSnap);

                min.x = _minX * gameDat.landscapePreviewBoundsEditSnap;
                min.y = _minY * gameDat.landscapePreviewBoundsEditSnap;

                Vector2 max = mBoxHandle.center + mBoxHandle.size * 0.5f;

                float _maxX = Mathf.Round(max.x / gameDat.landscapePreviewBoundsEditSnap);
                float _maxY = Mathf.Round(max.y / gameDat.landscapePreviewBoundsEditSnap);

                max.x = _maxX * gameDat.landscapePreviewBoundsEditSnap;
                max.y = _maxY * gameDat.landscapePreviewBoundsEditSnap;

                b.center = Vector2.Lerp(min, max, 0.5f);
                b.size = max - min;

                Undo.RecordObject(dat, "Change Bounds");
                dat.bounds = b;
            }
        }

        //edit regions
        var worldPos = dat.transform.position;

        var regionSnap = new Vector3 { x = gameDat.landscapePreviewRegionHandleSnap, y = gameDat.landscapePreviewRegionHandleSnap, z = 0f };

        if(dat.regions != null) {
            for(int i = 0; i < dat.regions.Length; i++) {
                var region = dat.regions[i];

                Vector3 wpos = worldPos + (Vector3)region.center;

                var size = HandleUtility.GetHandleSize(wpos) * gameDat.landscapePreviewRegionHandleScale;

                EditorGUI.BeginChangeCheck();
                var newPos = Handles.FreeMoveHandle(wpos, size, regionSnap, Handles.DotHandleCap);
                if(EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(dat, "Change Region Position");

                    region.center = newPos - worldPos;
                }
            }
        }
    }
}
