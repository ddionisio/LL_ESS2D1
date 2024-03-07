using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LandscapePreviewBoxCollder))]
public class LandscapePreviewBoxColliderInspector : Editor {
	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		var dat = (LandscapePreviewBoxCollder)target;

		bool isDatDirty;

		isDatDirty = dat.RefreshCollider();

		if(isDatDirty)
			EditorUtility.SetDirty(dat.boxCollider);
	}
}