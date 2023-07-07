using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using UnityEditor.IMGUI.Controls;

[CustomEditor(typeof(StructureGhost), true)]
public class StructureGhostInspector : Editor {
    BoxBoundsHandle mBoxHandle = new BoxBoundsHandle();

    void OnSceneGUI() {
        var gameDat = GameData.instance;

        var dat = target as StructureGhost;

        var worldPos = dat.transform.position;

        //edit bounds
        var placementBoundsProp = serializedObject.FindProperty("_placementBounds");

        using(new Handles.DrawingScope(gameDat.landscapePreviewBoundsColor)) {
            mBoxHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;

            Bounds b = placementBoundsProp.boundsValue;

            b.center += worldPos;

            mBoxHandle.center = new Vector3(b.center.x, b.center.y, 0f);
            mBoxHandle.size = new Vector3(b.size.x, b.size.y, 0f);

            EditorGUI.BeginChangeCheck();
            mBoxHandle.DrawHandle();
            if(EditorGUI.EndChangeCheck()) {
                Vector2 min = mBoxHandle.center - mBoxHandle.size * 0.5f;

                float _minX = Mathf.Round(min.x / gameDat.structurePlacementBoundsEditSnap);
                float _minY = Mathf.Round(min.y / gameDat.structurePlacementBoundsEditSnap);

                min.x = _minX * gameDat.structurePlacementBoundsEditSnap;
                min.y = _minY * gameDat.structurePlacementBoundsEditSnap;

                Vector2 max = mBoxHandle.center + mBoxHandle.size * 0.5f;

                float _maxX = Mathf.Round(max.x / gameDat.structurePlacementBoundsEditSnap);
                float _maxY = Mathf.Round(max.y / gameDat.structurePlacementBoundsEditSnap);

                max.x = _maxX * gameDat.structurePlacementBoundsEditSnap;
                max.y = _maxY * gameDat.structurePlacementBoundsEditSnap;

                b.center = Vector2.Lerp(min, max, 0.5f) - (Vector2)worldPos;
                b.size = max - min;

                placementBoundsProp.boundsValue = b;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
