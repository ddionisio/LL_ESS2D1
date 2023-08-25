using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycleControlAnimator : CycleControlBase {
    public WeatherTypeData[] weathers;
    public bool isEnd; //if true, play at end

    public GameObject rootGO;

    public M8.Animator.Animate animator;
    [M8.Animator.TakeSelector]
    public int takeEnter = -1;
    [M8.Animator.TakeSelector]
    public int takeExit = -1;

    private bool mIsActive;
    private Coroutine mExitRout;

    protected override void Begin() {
        Next();
    }

    protected override void Next() {
        var weather = ColonyController.instance.cycleController.cycleCurWeather;

        var isMatch = false;
        for(int i = 0; i < weathers.Length; i++) {
            if(weathers[i] == weather) {
                isMatch = true;
                break;
            }
        }

        if(mIsActive != isMatch) {
            if(isMatch)
                Enter();
            else
                Exit();
        }
    }

    protected override void End() {
        if(isEnd)
            Enter();
        else
            Exit();
    }

    protected override void Awake() {
        if(rootGO) rootGO.SetActive(false);

        base.Awake();
    }

    void OnDisable() {
        ClearRout();

        if(rootGO) rootGO.SetActive(false);

        mIsActive = false;
    }

    private void Enter() {
        if(mIsActive)
            return;

        ClearRout();

        mIsActive = true;

        if(rootGO) rootGO.SetActive(true);

        if(takeEnter != -1)
            animator.Play(takeEnter);
    }

    private void Exit() {
        if(mExitRout != null)
            return;

        mIsActive = false;

        if(gameObject.activeInHierarchy) {
            mExitRout = StartCoroutine(DoExit());
        }
        else {
            if(rootGO) rootGO.SetActive(false);
        }
    }

    IEnumerator DoExit() {
        if(takeExit != -1)
            yield return animator.PlayWait(takeExit);

        if(rootGO) rootGO.SetActive(false);

        mExitRout = null;
    }

    private void ClearRout() {
        if(mExitRout != null) {
            StopCoroutine(mExitRout);
            mExitRout = null;
        }
    }
}
