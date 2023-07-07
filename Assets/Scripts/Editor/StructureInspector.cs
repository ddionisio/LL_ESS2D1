using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using UnityEditor.IMGUI.Controls;

[CustomEditor(typeof(Structure), true)]
public class StructureInspector : Editor {
    

    void OnSceneGUI() {
        var gameDat = GameData.instance;

        var dat = target as Structure;

        var worldPos = dat.transform.position;

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