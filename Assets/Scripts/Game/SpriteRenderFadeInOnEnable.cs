using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteRenderFadeInOnEnable : MonoBehaviour {
    public SpriteRenderer target;

    [Header("Animation")]
    public float delay = 0.3f;
    public DG.Tweening.Ease ease = DG.Tweening.Ease.OutSine;
    public Color startColor;

    private DG.Tweening.EaseFunction mEaseFunc;
    private Color mDefaultColor;

    void OnEnable() {
        if(target)
            StartCoroutine(DoFadeIn());
    }

    void Awake() {
        if(!target) target = GetComponent<SpriteRenderer>();

        if(target)
            mDefaultColor = target.color;

        mEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(ease);
    }

    IEnumerator DoFadeIn() {
        target.color = startColor;

        var curTime = 0f;
        while(curTime < delay) {
            yield return null;

            curTime += Time.deltaTime;

            var t = mEaseFunc(curTime, delay, 0f, 0f);

            target.color = Color.Lerp(startColor, mDefaultColor, t);
        }
    }
}
