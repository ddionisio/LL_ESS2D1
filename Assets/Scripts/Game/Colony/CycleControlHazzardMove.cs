using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycleControlHazzardMove : CycleControlBase {
    [Header("Display")]
    public GameObject warningActiveGO;

    [Header("Move")]
    public Transform moveRoot;
    public Transform moveToRoot;
    public DG.Tweening.Ease moveInEase = DG.Tweening.Ease.OutSine;
    public DG.Tweening.Ease moveOutEase = DG.Tweening.Ease.InSine;
    public float moveDelay = 1.5f;

    private Coroutine mRout;

    private Vector2 mMoveStartPos;
    private Vector2 mMoveEndPos;

    private DG.Tweening.EaseFunction mMoveInEaseFunc;
    private DG.Tweening.EaseFunction mMoveOutEaseFunc;

    protected override void Begin() {
        
    }

    protected override void Next() {
        var curWeather = ColonyController.instance.cycleController.cycleCurWeather;
        if(curWeather.isHazzard) {
            if(gameObject.activeInHierarchy && moveRoot) {
                if(mRout != null) 
                    StopCoroutine(mRout);

                mRout = StartCoroutine(DoHazzard());
            }
        }
    }

    void OnDisable() {
        if(mRout != null) {
            StopCoroutine(mRout);
            mRout = null;
        }
    }

    protected override void Awake() {
        base.Awake();

        mMoveInEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(moveInEase);
        mMoveOutEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(moveOutEase);

        if(moveRoot)
            mMoveStartPos = moveRoot.position;
        if(moveToRoot)
            mMoveEndPos = moveToRoot.position;

        if(warningActiveGO) warningActiveGO.SetActive(false);
    }

    IEnumerator DoHazzard() {
		yield return null;
		
        var cycleCtrl = ColonyController.instance.cycleController;

        while(cycleCtrl.cycleTimeScale <= 0f)
            yield return null;

        //wait for hazzard to actually happen
        if(warningActiveGO) warningActiveGO.SetActive(true);

        while(!cycleCtrl.isHazzard)
            yield return null;

        if(warningActiveGO) warningActiveGO.SetActive(false);

        //move in
        var curTime = 0f;
        while(curTime < moveDelay) {
            yield return null;

            curTime += Time.deltaTime * cycleCtrl.cycleTimeScale;

            var t = mMoveInEaseFunc(curTime, moveDelay, 0f, 0f);

            moveRoot.position = Vector2.Lerp(mMoveStartPos, mMoveEndPos, t);
        }

        //wait for hazzard to be over
        while(cycleCtrl.isHazzard)
            yield return null;

        //move out
        curTime = 0f;
        while(curTime < moveDelay) {
            yield return null;

            curTime += Time.deltaTime * cycleCtrl.cycleTimeScale;

            var t = mMoveOutEaseFunc(curTime, moveDelay, 0f, 0f);

            moveRoot.position = Vector2.Lerp(mMoveEndPos, mMoveStartPos, t);
        }

        mRout = null;
    }
}
