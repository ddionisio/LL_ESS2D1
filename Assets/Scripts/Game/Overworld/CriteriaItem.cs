using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CriteriaItem : MonoBehaviour {
    public enum State {
        None,
        Neutral,
        Good,
        Bad
    }

    [Header("State Display")]
    public GameObject stateNeutralGO;
    public GameObject stateGoodGO;
    public GameObject stateBadGO;

    [Header("Animations")]
    public M8.Animator.Animate animator;
    public M8.RangeFloat waitDelayRange;
    [M8.Animator.TakeSelector]
    public int takeNeutral = -1;
    [M8.Animator.TakeSelector]
    public int takeBad = -1;
    [M8.Animator.TakeSelector]
    public int takeGood = -1;    

    public State state {
        get { return mState; }
        set {
            if(mState != value) {
                ClearState();

                mState = value;

                ApplyState();
            }
        }
    }

    private State mState;
    private Coroutine mRout;

    void OnDisable() {
        ClearState();
    }

    void OnEnable() {
        ApplyState();
    }

    //TODO: palette index
    public void Init() {
        if(stateNeutralGO) stateNeutralGO.SetActive(false);
        if(stateGoodGO) stateGoodGO.SetActive(false);
        if(stateBadGO) stateBadGO.SetActive(false);

        mState = State.None;
    }

    IEnumerator DoAnimationDelay(int takeInd) {
        if(takeInd == -1) {
            mRout = null;
            yield break;
        }

        while(true) {
            var curTime = 0f;
            var delay = waitDelayRange.random;
            while(curTime < delay) {
                yield return null;
                curTime += Time.deltaTime;
            }

            yield return animator.PlayWait(takeInd);
        }
    }

    private void ApplyState() {
        switch(mState) {
            case State.Neutral:
                if(stateNeutralGO) stateNeutralGO.SetActive(true);
                if(takeNeutral != -1) mRout = StartCoroutine(DoAnimationDelay(takeNeutral));
                break;
            case State.Good:
                if(stateGoodGO) stateGoodGO.SetActive(true);
                if(takeGood != -1) mRout = StartCoroutine(DoAnimationDelay(takeGood));
                break;
            case State.Bad:
                if(stateBadGO) stateBadGO.SetActive(true);
                if(takeBad != -1) mRout = StartCoroutine(DoAnimationDelay(takeBad));
                break;
            default:
                if(takeNeutral != -1)
                    animator.ResetTake(takeNeutral);
                break;
        }
    }

    private void ClearState() {
        if(mRout != null) {
            StopCoroutine(mRout);
            mRout = null;
        }

        switch(mState) {
            case State.Neutral:
                if(stateNeutralGO) stateNeutralGO.SetActive(false);
                break;
            case State.Good:
                if(stateGoodGO) stateGoodGO.SetActive(false);
                break;
            case State.Bad:
                if(stateBadGO) stateBadGO.SetActive(false);
                break;
        }

        if(takeNeutral != -1)
            animator.ResetTake(takeNeutral);
    }
}
