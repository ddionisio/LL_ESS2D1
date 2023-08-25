using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CycleControlColorBase : CycleControlBase {
    [System.Serializable]
    public struct WeatherInfo {
        public WeatherTypeData weather;
        public Color color;
    }

    [Header("Color Info")]
    public WeatherInfo[] weatherTransitions;
    public Color startColor = Color.white;
    public Color[] dayColors; //if no weather matches
    public Color[] nightColors;
    public Color[] endColors;

    [Header("Animation")]
    public float delayTransitionScale = 0.25f;
    public float endDelay = 0.5f;

    private Color mCurColor;

    private Color[] mColorTransition;
    private int mColorTransitionLength;

    private float mColorDayTransitionDelay;
    private float mColorNightTransitionDelay;

    private Coroutine mRout;

    protected abstract void ApplyColor(Color color);

    protected override void Init() {
        int maxColorLength = 0;

        if(dayColors.Length > maxColorLength)
            maxColorLength = dayColors.Length;

        if(nightColors.Length > maxColorLength)
            maxColorLength = nightColors.Length;

        if(endColors.Length > maxColorLength)
            maxColorLength = endColors.Length;

        mColorTransition = new Color[maxColorLength + 1];
                
        var cycleCtrl = ColonyController.instance.cycleController;

        mColorDayTransitionDelay = cycleCtrl.cycleDayDuration * delayTransitionScale;
        mColorNightTransitionDelay = cycleCtrl.cycleNightDuration * delayTransitionScale;
    }

    protected override void Begin() {
        if(mRout != null)
            StopCoroutine(mRout);

        //fail safe
        if(!gameObject.activeInHierarchy) {
            mRout = null;
            return;
        }

        var cycleCtrl = ColonyController.instance.cycleController;

        mRout = StartCoroutine(DoCycleTransition(cycleCtrl.cycleCurWeather));
    }

    protected override void Next() {
        if(mRout != null)
            StopCoroutine(mRout);

        //fail safe
        if(!gameObject.activeInHierarchy) {
            mRout = null;
            return;
        }

        var cycleCtrl = ColonyController.instance.cycleController;

        mRout = StartCoroutine(DoCycleTransition(cycleCtrl.cycleCurWeather));
    }

    protected override void End() {
        if(mRout != null)
            StopCoroutine(mRout);

        //fail safe
        if(!gameObject.activeInHierarchy) {
            mRout = null;
            return;
        }

        mRout = StartCoroutine(DoEndTransition());
    }

    protected override void Awake() {
        base.Awake();

        mCurColor = startColor;
        ApplyColor(mCurColor);
    }

    void OnDisable() {
        if(mRout != null) {
            StopCoroutine(mRout);
            mRout = null;
        }
    }

    IEnumerator DoCycleTransition(WeatherTypeData weather) {
        var cycleCtrl = ColonyController.instance.cycleController;

        //get matching weather
        Color colorMod = Color.white;
        for(int i = 0; i < weatherTransitions.Length; i++) {
            var inf = weatherTransitions[i];
            if(inf.weather == weather) {
                colorMod = inf.color;
                break;
            }
        }

        //transition to day
        ApplyColors(dayColors, colorMod);

        yield return DoColorTransition(mColorDayTransitionDelay);

        //wait for day to be over
        var waitDelay = cycleCtrl.cycleDayDuration - mColorDayTransitionDelay;
        while(cycleCtrl.cycleCurElapsed < waitDelay)
            yield return null;

        //wait for day to be over
        //while(cycleCtrl.cycleIsDay)
            //yield return null;

        //day to night
        ApplyColors(nightColors, colorMod);

        yield return DoColorTransition(mColorNightTransitionDelay);

        mRout = null;
    }

    IEnumerator DoEndTransition() {
        ApplyColors(endColors, Color.white);
        yield return DoColorTransition(endDelay);

        mRout = null;
    }

    IEnumerator DoColorTransition(float delay) {
        var cycleCtrl = ColonyController.instance.cycleController;

        var curTime = 0f;
        while(curTime < delay) {
            yield return null;

            curTime += Time.deltaTime * cycleCtrl.cycleTimeScale;

            var t = Mathf.Clamp01(curTime / delay);
            mCurColor = M8.ColorUtil.Lerp(mColorTransition, 0, mColorTransitionLength, t);
            ApplyColor(mCurColor);
        }
    }

    private void ApplyColors(Color[] colors, Color colorMod) {
        mColorTransition[0] = mCurColor;

        for(int i = 0; i < colors.Length; i++)
            mColorTransition[i + 1] = colors[i] * colorMod;

        mColorTransitionLength = colors.Length + 1;
    }
}
