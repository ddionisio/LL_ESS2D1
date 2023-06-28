using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LandscapePreviewSizeWidget : MonoBehaviour {
    public bool RefreshTransform() {
        var rectTrans = transform as RectTransform;
        if(!rectTrans)
            return false;

        var canvasScaler = GetComponentInParent<CanvasScaler>(true);
        if(!canvasScaler)
            return false;

        var curSize = rectTrans.sizeDelta;

        //var resScale = new Vector2(canvasScaler.referenceResolution.x / Screen.width, canvasScaler.referenceResolution.y / Screen.height);
        var ppu = canvasScaler.referencePixelsPerUnit;

        var landscapeSize = GameData.instance.landscapePreviewSize;

        //landscapeSize.Scale(resScale * ppu);
        landscapeSize *= ppu;

        if(curSize != landscapeSize) {
            rectTrans.sizeDelta = landscapeSize;

            return true;
        }

        return false;
    }

    void Awake() {
        RefreshTransform();
    }

    void OnDrawGizmos() {
        var t = transform;

        var rectT = t as RectTransform;
        if(!rectT)
            return;

        Gizmos.color = GameData.instance.landscapePreviewBoundsColor;

        //grab corners and draw wire
        var rect = rectT.rect;

        var p0 = t.TransformPoint(new Vector2(rect.xMin, rect.yMin));
        var p1 = t.TransformPoint(new Vector2(rect.xMax, rect.yMin));
        var p2 = t.TransformPoint(new Vector2(rect.xMax, rect.yMax));
        var p3 = t.TransformPoint(new Vector2(rect.xMin, rect.yMax));

        Gizmos.DrawLine(p0, p1);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p0);
    }
}
