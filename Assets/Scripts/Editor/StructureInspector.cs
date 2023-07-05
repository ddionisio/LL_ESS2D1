using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using UnityEditor.IMGUI.Controls;

[CustomEditor(typeof(Structure), true)]
public class StructureInspector : Editor {
    BoxBoundsHandle mBoxHandle = new BoxBoundsHandle();

    void OnSceneGUI() {
        var gameDat = GameData.instance;

        var dat = target as Structure;

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

        //edit waypoints
        var curWaypointName = "";
        var curWaypointClrInd = 0;
                
        var waypointSnap = new Vector3 { x = gameDat.structureWaypointHandleSnap, y = gameDat.structureWaypointHandleSnap, z = 0f };

        var waypointsGroupProp = serializedObject.FindProperty("_waypointGroups");

        var wayptGroupCount = waypointsGroupProp.arraySize;
        for(int i = 0; i < wayptGroupCount; i++) {
            var wayptItemProp = waypointsGroupProp.GetArrayElementAtIndex(i);

            //setup color
            var wayptGroupNameProp = wayptItemProp.FindPropertyRelative("name");

            if(curWaypointName == "") {
                curWaypointName = wayptGroupNameProp.stringValue;

                Handles.color = gameDat.structureWaypointColors[curWaypointClrInd];
            }
            else if(curWaypointName != wayptGroupNameProp.stringValue) {
                curWaypointClrInd++;
                if(curWaypointClrInd == gameDat.structureWaypointColors.Length)
                    curWaypointClrInd = 0;

                Handles.color = gameDat.structureWaypointColors[curWaypointClrInd];
            }

            //go through points
            var wayptGroupItems = wayptItemProp.FindPropertyRelative("waypoints");

            var wayptCount = wayptGroupItems.arraySize;
            for(int j = 0; j < wayptCount; j++) {
                var wayptProp = wayptGroupItems.GetArrayElementAtIndex(j);

                var wayptPosProp = wayptProp.FindPropertyRelative("_point");

                Vector3 wpos = worldPos + (Vector3)wayptPosProp.vector2Value;

                var size = HandleUtility.GetHandleSize(wpos) * gameDat.structureWaypointHandleScale;

                EditorGUI.BeginChangeCheck();
                var newPos = Handles.FreeMoveHandle(wpos, Quaternion.identity, size, waypointSnap, Handles.DotHandleCap);
                if(EditorGUI.EndChangeCheck()) {
                    wayptPosProp.vector2Value = newPos - worldPos;
                }
            }
        }
                
        serializedObject.ApplyModifiedProperties();
    }
}