using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LandscapePreviewSizeSpriteMask))]
public class LandscapePreviewSizeSpriteMaskInspector : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        var dat = (LandscapePreviewSizeSpriteMask)target;
        var gameDat = GameData.instance;

        bool isGameDatDirty = false, isDatDirty;

        var newLandscapePreviewSize = EditorGUILayout.Vector2Field("Preview Size", gameDat.landscapePreviewSize);

        if(gameDat.landscapePreviewSize != newLandscapePreviewSize) {
            Undo.RecordObject(gameDat, "Game Data Landscape Preview Size");
            gameDat.landscapePreviewSize = newLandscapePreviewSize;

            isGameDatDirty = true;
        }

        isDatDirty = dat.RefreshTransform();

        if(isGameDatDirty)
            EditorUtility.SetDirty(gameDat);

        if(isDatDirty)
            EditorUtility.SetDirty(dat.transform);
    }
}