using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;

[CustomEditor(typeof(StructureSpecialAction), true)]
public class StructureSpecialActionInspector : Editor {
	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		if(Application.isPlaying) {
			M8.EditorExt.Utility.DrawSeparator();

			var dat = target as StructureSpecialAction;

			if(dat.isActive) {
				if(GUILayout.Button("Deactivate"))
					dat.Activate(false);
			}
			else {
				if(GUILayout.Button("Activate"))
					dat.Activate(true);
			}
		}
	}
}
