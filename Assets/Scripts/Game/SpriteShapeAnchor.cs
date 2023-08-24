using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Demo Script Usage:
//   When you want multiple SpriteShapes to share a common Spline,
//   attach this script to the secondary objects you would like to 
//   copy the Spline and set the ParentObject to the original object
//   you are copying from.

[ExecuteInEditMode]
public class SpriteShapeAnchor : MonoBehaviour {

    public GameObject m_ParentObject;

    public bool renderEnabled = true;

    private SpriteShapeController mTarget;

    void Awake() {
        mTarget = GetComponent<SpriteShapeController>();
    }

    // Use this for initialization
    void Start() {
        if(mTarget) {
            mTarget.spriteShapeRenderer.enabled = renderEnabled;
        }
    }

#if UNITY_EDITOR
    // Update is called once per frame
    void Update() {
        if(m_ParentObject != null) {
            CopySpline(m_ParentObject, gameObject);
        }
    }

    private static void CopySpline(GameObject src, GameObject dst) {
        var parentSpriteShapeController = src.GetComponent<SpriteShapeController>();
        var mirrorSpriteShapeController = dst.GetComponent<SpriteShapeController>();

        if(parentSpriteShapeController != null && mirrorSpriteShapeController != null) {
            SerializedObject srcController = new SerializedObject(parentSpriteShapeController);
            SerializedObject dstController = new SerializedObject(mirrorSpriteShapeController);
            SerializedProperty srcSpline = srcController.FindProperty("m_Spline");
            dstController.CopyFromSerializedProperty(srcSpline);
            dstController.ApplyModifiedProperties();
            EditorUtility.SetDirty(mirrorSpriteShapeController);
        }

    }
#endif
}