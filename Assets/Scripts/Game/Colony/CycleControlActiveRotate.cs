using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycleControlActiveRotate : CycleControlBase {
    [System.Serializable]
    public class RotateInfo {
        public GameObject rootGO;
        public float startAngle;
        public float endAngle;

        public bool active { get { return rootGO ? rootGO.activeSelf : false; } set { if(rootGO) rootGO.SetActive(value); }  }
        public float t {
            get { return mT; }
            set {
                var val = Mathf.Clamp01(value);
                if(mT != val) {
                    mT = val;
                    ApplyRotate();
                }
            }
        }

        private Transform mTransform;
        private float mT;

        public void Init() {
            if(rootGO) {
                rootGO.SetActive(false);
                mTransform = rootGO.transform;
            }

            mT = 0f;
            ApplyRotate();
        }

        private void ApplyRotate() {
            if(mTransform)
                mTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(startAngle, endAngle, mT));
        }
    }

    [Header("Info")]
    public RotateInfo dayRotate;
    public RotateInfo nightRotate;

    private Coroutine mRout;
    private bool mIsEnd;

    protected override void Begin() {
        if(mRout != null) {
            StopCoroutine(mRout);
            mRout = null;
        }

        mIsEnd = false;

        if(gameObject.activeInHierarchy) //fail-safe
            mRout = StartCoroutine(DoCycle());
    }

    protected override void Next() {
        if(mRout == null) {
            if(gameObject.activeInHierarchy) //fail-safe
                mRout = StartCoroutine(DoCycle());
            else {
                StopCoroutine(mRout);
                mRout = null;
            }
        }
    }

    protected override void End() {
        mIsEnd = true;
    }
        
    protected override void Awake() {
        base.Awake();

        dayRotate.Init();
        nightRotate.Init();
    }

    void OnDisable() {
        if(mRout != null) {
            StopCoroutine(mRout);
            mRout = null;
        }
    }

    IEnumerator DoCycle() {
        var cycleCtrl = ColonyController.instance.cycleController;

        var isDayCur = cycleCtrl.cycleIsDay;
        var isSunVisibleCur = cycleCtrl.cycleCurWeather.isSunVisible;

        ApplyVisible(isDayCur, isSunVisibleCur);

        while(!mIsEnd) {
            var isDay = cycleCtrl.cycleIsDay;
            var isSunVisible = cycleCtrl.cycleCurWeather.isSunVisible;
            if(isDayCur != isDay || isSunVisibleCur != isSunVisible) {
                isDayCur = isDay;
                isSunVisibleCur = isSunVisible;

                ApplyVisible(isDayCur, isSunVisibleCur);
            }

            if(dayRotate.active) {
                dayRotate.t = cycleCtrl.cycleDayElapsedNormalized;
            }
            else if(nightRotate.active) {
                nightRotate.t = cycleCtrl.cycleNightElapsedNormalized;
            }

            yield return null;
        }

        ApplyVisible(false, false);

        mRout = null;
    }

    private void ApplyVisible(bool isDay, bool isSunVisible) {
        if(isSunVisible) {
            dayRotate.active = isDay;
            nightRotate.active = !isDay;
        }
        else {
            dayRotate.active = false;
            nightRotate.active = false;
        }
    }
}
