using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverworldOverlaySeasons : MonoBehaviour {
    [System.Serializable]
    public class SeasonInfo {
        public SeasonData data;
        public GameObject activeGO;

        private SpriteRenderer[] mSpriteRenders;
        private float[] mSpriteDefaultAlphas;
        private float[] mSpriteStartAlphas;

        private Coroutine mRout;

        public bool active {
            get { return activeGO ? activeGO.activeSelf : false; }
            set { if(activeGO) activeGO.SetActive(value); }
        }

        public bool isBusy { get { return mRout != null; } }

        public void Init() {
            if(mSpriteRenders == null) {
                mSpriteRenders = activeGO.GetComponentsInChildren<SpriteRenderer>(true);
                mSpriteDefaultAlphas = new float[mSpriteRenders.Length];
                mSpriteStartAlphas = new float[mSpriteRenders.Length];

                for(int i = 0; i < mSpriteRenders.Length; i++) {
                    var c = mSpriteRenders[i].color;

                    mSpriteDefaultAlphas[i] = c.a;

                    c.a = 0f; //start as fade out completely

                    mSpriteRenders[i].color = c;
                }
            }
            else {
                for(int i = 0; i < mSpriteRenders.Length; i++) {
                    var c = mSpriteRenders[i].color;
                    c.a = 0f; //start as fade out completely

                    mSpriteRenders[i].color = c;
                }
            }

            active = false; //start as hidden
        }

        public void FadeIn(MonoBehaviour behaviour, DG.Tweening.EaseFunction easeFunc, float delay) {
            if(delay <= 0f || mSpriteRenders.Length == 0) {
                active = true;
                return;
            }

            if(active && !isBusy) //already fully active
                return;

            if(mRout != null)
                behaviour.StopCoroutine(mRout);

            mRout = behaviour.StartCoroutine(DoAlphaTween(easeFunc, delay, 1f, false));
        }

        public void FadeOut(MonoBehaviour behaviour, DG.Tweening.EaseFunction easeFunc, float delay) {
            if(delay <= 0f || mSpriteRenders.Length == 0) {
                active = false;
                return;
            }

            if(!active && !isBusy) //already fully hidden
                return;

            if(mRout != null)
                behaviour.StopCoroutine(mRout);

            mRout = behaviour.StartCoroutine(DoAlphaTween(easeFunc, delay, 0f, true));
        }

        private IEnumerator DoAlphaTween(DG.Tweening.EaseFunction easeFunc, float delay, float alphaScale, bool hideOnEnd) {
            if(!hideOnEnd)
                active = true;

            for(int i = 0; i < mSpriteRenders.Length; i++)
                mSpriteStartAlphas[i] = mSpriteRenders[i].color.a;

            var curTime = 0f;
            while(curTime < delay) {
                yield return null;

                curTime += Time.deltaTime;

                var t = easeFunc(curTime, delay, 0f, 0f);

                for(int i = 0; i < mSpriteRenders.Length; i++) {
                    var a = Mathf.Lerp(mSpriteStartAlphas[i], mSpriteDefaultAlphas[i] * alphaScale, t);

                    var c = mSpriteRenders[i].color;

                    mSpriteRenders[i].color = new Color(c.r, c.g, c.b, a);
                }
            }

            if(hideOnEnd)
                active = false;
            
            mRout = null;
        }
    }

    [Header("Data")]
    public AtmosphereAttributeBase atmosphereData; //optional, otherwise disregard what atmosphere to use
    public SeasonInfo[] seasons;

    [Header("Animation")]
    public DG.Tweening.Ease fadeInEase = DG.Tweening.Ease.OutSine;
    public DG.Tweening.Ease fadeOutEase = DG.Tweening.Ease.InSine;
    public float fadeDelay = 0.3f;

    [Header("Signal Listen")]
    public SignalAtmosphereAttribute signalListenAtmosphereToggle;
    public SignalSeasonData signalListenSeasonToggle;

    private bool mIsAtmosphereMatch;

    private int mCurSeasonIndex;

    private SeasonData mSeasonToggled;

    private DG.Tweening.EaseFunction mFadeInEaseFunc;
    private DG.Tweening.EaseFunction mFadeOutEaseFunc;

    void OnDisable() {
        if(signalListenAtmosphereToggle) signalListenAtmosphereToggle.callback -= OnAtmosphereToggle;
        if(signalListenSeasonToggle) signalListenSeasonToggle.callback -= OnSeasonToggle;

        mSeasonToggled = null;
    }

    void OnEnable() {
        //hide everything initially
        for(int i = 0; i < seasons.Length; i++)
            seasons[i].Init();

        if(signalListenAtmosphereToggle) signalListenAtmosphereToggle.callback += OnAtmosphereToggle;
        if(signalListenSeasonToggle) signalListenSeasonToggle.callback += OnSeasonToggle;

        mCurSeasonIndex = -1;

        mIsAtmosphereMatch = !(atmosphereData && signalListenAtmosphereToggle);
    }

    void Awake() {
        mFadeInEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(fadeInEase);
        mFadeOutEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(fadeOutEase);
    }

    void OnAtmosphereToggle(AtmosphereAttributeBase attr) {
        mIsAtmosphereMatch = atmosphereData == null || atmosphereData == attr;

        if(mIsAtmosphereMatch) {
            if(mSeasonToggled) {
                ApplySeason(mSeasonToggled);
                mSeasonToggled = null;
            }
        }
        else {
            if(mCurSeasonIndex != -1) {
                seasons[mCurSeasonIndex].FadeOut(this, mFadeOutEaseFunc, fadeDelay);
                mSeasonToggled = seasons[mCurSeasonIndex].data;
                mCurSeasonIndex = -1;
            }
        }
    }

    void OnSeasonToggle(SeasonData season) {
        if(!mIsAtmosphereMatch) {
            mSeasonToggled = season;
            return;
        }

        ApplySeason(season);
    }

    private void ApplySeason(SeasonData season) {
        int seasonInd = -1;
        for(int i = 0; i < seasons.Length; i++) {
            if(seasons[i].data == season) {
                seasonInd = i;
                break;
            }
        }

        if(mCurSeasonIndex != seasonInd) {
            if(mCurSeasonIndex != -1)
                seasons[mCurSeasonIndex].FadeOut(this, mFadeOutEaseFunc, fadeDelay);

            mCurSeasonIndex = seasonInd;

            if(mCurSeasonIndex != -1)
                seasons[mCurSeasonIndex].FadeIn(this, mFadeInEaseFunc, fadeDelay);
        }

    }
}
