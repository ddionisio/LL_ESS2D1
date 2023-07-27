using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;

[CustomEditor(typeof(Unit), true)]
public class UnitInspector : Editor {

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        //debug stuff
        if(Application.isPlaying) {
            M8.EditorExt.Utility.DrawSeparator();

            var dat = target as Unit;

            if(GUILayout.Button("Damage")) {
                dat.hitpointsCurrent--;
            }

            if(GUILayout.Button("Kill")) {
                dat.hitpointsCurrent = 0;
            }
        }
    }
}
