using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverworldView : MonoBehaviour {
    [Header("Zoom Info")]
    public DG.Tweening.Ease zoomInEase = DG.Tweening.Ease.OutSine;
    public float zoomInDelay = 0.3f;
    public DG.Tweening.Ease zoomOutEase = DG.Tweening.Ease.InSine;
    public float zoomOutDelay = 0.3f;

    public bool isZoomIn { get; private set; }

    public bool isBusy { get { return mRout != null; } }

    private DG.Tweening.EaseFunction mZoomInEaseFunc;
    private DG.Tweening.EaseFunction mZoomOutEaseFunc;

    private Coroutine mRout;

    private Vector2 mOriginLocalPos = Vector2.zero;
    private float mOriginLocalScale = 1f;

    public void ZoomIn(Vector2 worldPos, float zoomAmount) {
        if(isZoomIn || isBusy)
            return;

        mRout = StartCoroutine(DoZoomIn(worldPos, zoomAmount));
    }

    public void ZoomOut() {
        if(!isZoomIn || isBusy)
            return;

        mRout = StartCoroutine(DoZoomOut());
    }

    /// <summary>
    /// Force reset
    /// </summary>
    public void ResetZoom() {
        if(mRout != null) {
            StopCoroutine(mRout);
            mRout = null;
        }

        transform.localPosition = mOriginLocalPos;
        transform.localScale = new Vector3(mOriginLocalScale, mOriginLocalScale, 1f);

        isZoomIn = false;
    }

    void OnDisable() {
        ResetZoom();
    }

    void Awake() {
        mZoomInEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(zoomInEase);
        mZoomOutEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(zoomOutEase);

        mOriginLocalPos = transform.localPosition;
        mOriginLocalScale = transform.localScale.x;
    }

    IEnumerator DoZoomIn(Vector2 worldPos, float zoomAmount) {
        isZoomIn = true;

        var mtx = transform.worldToLocalMatrix;

        Vector2 sPos = transform.localPosition;
        Vector2 ePos = -mtx.MultiplyPoint3x4(worldPos);

        var sScale = transform.localScale.x;
        var eScale = zoomAmount;

        var curTime = 0f;
        while(curTime < zoomOutDelay) {
            yield return null;

            curTime += Time.deltaTime;

            var t = mZoomInEaseFunc(curTime, zoomOutDelay, 0f, 0f);

            var s = Mathf.Lerp(sScale, eScale, t);

            var pos = Vector2.Lerp(sPos, ePos, t) * s;

            transform.localPosition = pos;
            transform.localScale = new Vector3(s, s, 1f);
        }

        mRout = null;
    }

    IEnumerator DoZoomOut() {
        isZoomIn = false;

        Vector2 sPos = transform.localPosition;
        Vector2 ePos = mOriginLocalPos;

        var sScale = transform.localScale.x;
        var eScale = mOriginLocalScale;

        var curTime = 0f;
        while(curTime < zoomOutDelay) {
            yield return null;

            curTime += Time.deltaTime;

            var t = mZoomOutEaseFunc(curTime, zoomOutDelay, 0f, 0f);

            var pos = Vector2.Lerp(sPos, ePos, t);
            var s = Mathf.Lerp(sScale, eScale, t);

            transform.localPosition = pos;
            transform.localScale = new Vector3(s, s, 1f);
        }

        mRout = null;
    }
}
