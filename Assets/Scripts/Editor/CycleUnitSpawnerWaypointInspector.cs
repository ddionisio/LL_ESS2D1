using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;

[CustomEditor(typeof(CycleUnitSpawnerWaypoint))]
public class CycleUnitSpawnerWaypointInspector : Editor {
    void OnSceneGUI() {
        var gameDat = GameData.instance;

        var dat = target as CycleUnitSpawnerWaypoint;

        var worldPos = dat.transform.position;

        var waypointSnap = new Vector3 { x = gameDat.structureWaypointHandleSnap, y = gameDat.structureWaypointHandleSnap, z = 0f };

        var waypointItems = serializedObject.FindProperty("_waypoints");

        Handles.color = gameDat.unitSpawnerWaypointColor;

        var waypointCount = waypointItems.arraySize;
        for(int i = 0; i < waypointCount; i++) {
            var wayptProp = waypointItems.GetArrayElementAtIndex(i);

            var wayptPosProp = wayptProp.FindPropertyRelative("_point");

            Vector3 wpos = worldPos + (Vector3)wayptPosProp.vector2Value;

            var size = HandleUtility.GetHandleSize(wpos) * gameDat.structureWaypointHandleScale;

            EditorGUI.BeginChangeCheck();
            var newPos = Handles.FreeMoveHandle(wpos, Quaternion.identity, size, waypointSnap, Handles.DotHandleCap);
            if(EditorGUI.EndChangeCheck()) {
                wayptPosProp.vector2Value = newPos - worldPos;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
