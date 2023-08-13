using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PingController : MonoBehaviour, IPointerClickHandler {
    [Header("Display")]
    public Transform pingRoot;

    [Header("Fade Display")]
    public GameObject fadeRootGO;
    public SpriteRenderer fadeSprite;
    public Color fadeInColor = Color.black;
    public float fadeDelay = 0.3f;

    [Header("Animation")]
    public M8.Animator.Animate animator;
    [M8.Animator.TakeSelector]
    public string takePing;

    public bool isBusy { get { return mRout != null; } }

    private Coroutine mRout;

    private int mTakePingInd = -1;

    private M8.CacheList<Hotspot> mHotspotPings = new M8.CacheList<Hotspot>(8);
    private M8.CacheList<Hotspot> mHotspotReveals = new M8.CacheList<Hotspot>(8);

    void OnEnable() {
        if(pingRoot) pingRoot.gameObject.SetActive(false);

        if(fadeRootGO) fadeRootGO.SetActive(false);
    }

    void Awake() {
        if(animator) {
            mTakePingInd = animator.GetTakeIndex(takePing);
        }
    }

    IEnumerator DoPing(Vector2 pos) {
        if(pingRoot) {
            pingRoot.gameObject.SetActive(true);
            pingRoot.position = pos;
        }

        //do animation
        if(mTakePingInd != -1)
            yield return animator.PlayWait(mTakePingInd);

        if(pingRoot)
            pingRoot.gameObject.SetActive(false);

        //do hotspot reveals        
        for(int i = 0; i < mHotspotReveals.Count; i++)
            mHotspotReveals[i].Reveal();

        while(mHotspotReveals.Count > 0) {
            yield return null;

            for(int i = mHotspotReveals.Count - 1; i >= 0; i--) {
                if(!mHotspotReveals[i].isBusy)
                    mHotspotReveals.RemoveAt(i);
            }
        }

        //do hotspot pings
        if(mHotspotPings.Count > 0) {
            //fade in
            if(fadeRootGO) fadeRootGO.SetActive(true);

            if(fadeSprite) {
                var clr = Color.clear;
                fadeSprite.color = clr;

                var curTime = 0f;
                while(curTime < fadeDelay) {
                    yield return null;

                    curTime += Time.deltaTime;
                    var t = Mathf.Clamp01(curTime / fadeDelay);

                    fadeSprite.color = Color.LerpUnclamped(clr, fadeInColor, t);
                }
            }

            //do pings
            for(int i = 0; i < mHotspotPings.Count; i++)
                mHotspotPings[i].Ping(pos);

            while(mHotspotPings.Count > 0) {
                yield return null;

                for(int i = mHotspotPings.Count - 1; i >= 0; i--) {
                    if(!mHotspotPings[i].isBusy)
                        mHotspotPings.RemoveAt(i);
                }
            }

            //fade out
            if(fadeSprite) {
                var curTime = 0f;
                while(curTime < fadeDelay) {
                    yield return null;

                    curTime += Time.deltaTime;
                    var t = Mathf.Clamp01(curTime / fadeDelay);

                    fadeSprite.color = Color.LerpUnclamped(fadeInColor, Color.clear, t);
                }
            }

            if(fadeRootGO) fadeRootGO.SetActive(false);
        }

        mRout = null;
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        if(isBusy)
            return;

        var hit = eventData.pointerPressRaycast;
        if(!hit.isValid)
            return;

        Vector2 pos = hit.worldPosition;

        //check hotspots
        var hotspotGrp = OverworldController.instance.hotspotGroup;
        if(!hotspotGrp)
            return;

        mHotspotPings.Clear();
        mHotspotReveals.Clear();

        for(int i = 0; i < hotspotGrp.hotspots.Length; i++) {
            var hotspot = hotspotGrp.hotspots[i].hotspot;
            if(!hotspot.isHidden)
                continue;

            var pingResult = hotspot.CheckPing(pos);
            switch(pingResult) {
                case Hotspot.PingResult.Near:
                    mHotspotPings.Add(hotspot);
                    break;

                case Hotspot.PingResult.Reveal:
                    mHotspotReveals.Add(hotspot);
                    break;
            }
        }

        mRout = StartCoroutine(DoPing(pos));
    }
}