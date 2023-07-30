using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;

[CustomEditor(typeof(CycleUnitSpawnerBase), true)]
public class CycleUnitSpawnerBaseInspector : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        if(Application.isPlaying) {
            M8.EditorExt.Utility.DrawSeparator();

            var dat = target as CycleUnitSpawnerBase;

            if(GUILayout.Button("Spawn")) {
                dat.Spawn();
            }
        }
    }
}