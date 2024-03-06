using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class AtmosphereAttributeRangeWidget : MonoBehaviour {
    [Header("Display")]
    public Image iconImage;
    public bool iconUseNativeSize;

    public TMP_Text nameLabel;
    public string nameFormat = "{0}:";

    public TMP_Text rangeLabel;
    public bool rangeUseSingleValue;

	[Header("Range Bar")]
    public Slider rangeSlider;
	public RectTransform rangeValidBaseArea;
	public RectTransform rangeValidArea;

	[Header("Range Change")]
    public float rangeChangeDelay = 0.5f;
    public DG.Tweening.Ease rangeChangeEase = DG.Tweening.Ease.OutSine;
        
    private AtmosphereAttributeBase mAtmosphereAttr;

    private M8.RangeFloat mCurRange;

    private M8.RangeFloat mFromRange;
    private M8.RangeFloat mToRange;

    private DG.Tweening.EaseFunction mRangeChangeEaseFunc;

    private Coroutine mRout;

    void OnDisable() {
        if(mRout != null) {
            StopCoroutine(mRout);
            mRout = null;
        }
    }

    void Awake() {
        mRangeChangeEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(rangeChangeEase);
    }

    public void Setup(AtmosphereAttributeBase atmosphereAttribute, M8.RangeFloat range) {
        mAtmosphereAttr = atmosphereAttribute;

        if(rangeUseSingleValue)
            mCurRange = new M8.RangeFloat(range.Lerp(0.5f));
        else
            mCurRange = range;

        if(iconImage) {
            iconImage.sprite = mAtmosphereAttr.icon;

            if(iconUseNativeSize)
                iconImage.SetNativeSize();
        }

        if(nameLabel)
            nameLabel.text = string.Format(nameFormat, M8.Localize.Get(mAtmosphereAttr.nameRef));

        ApplyCurRange();
    }

    /// <summary>
    /// Call this after Setup if using range display
    /// </summary>
    public void SetupRangeValid(M8.RangeFloat rangeValid) {
        if(!(mAtmosphereAttr && rangeValidBaseArea && rangeValidArea))
            return;

        var lenValid = rangeValid.length;
        if(lenValid > 0f) {
			rangeValidArea.gameObject.SetActive(true);

            var minT = mAtmosphereAttr.rangeLimit.GetT(rangeValid.min);
			var maxT = mAtmosphereAttr.rangeLimit.GetT(rangeValid.max);

            var pos = rangeValidArea.anchoredPosition;
            var size = rangeValidArea.sizeDelta;

            pos.x = minT * rangeValidBaseArea.sizeDelta.x;
            size.x = (maxT - minT) * rangeValidBaseArea.sizeDelta.x;

            rangeValidArea.anchoredPosition = pos;
            rangeValidArea.sizeDelta = size;
		}
        else {
            rangeValidArea.gameObject.SetActive(false);
        }
    }

    public void SetRange(M8.RangeFloat range) {
        if(mRout == null) {
            mFromRange = mCurRange;

            if(rangeUseSingleValue)
                mToRange = new M8.RangeFloat(range.Lerp(0.5f));
            else
                mToRange = range;

            mRout = StartCoroutine(DoRangeChange());
        }
        else {
            mToRange = range;
        }
    }

    IEnumerator DoRangeChange() {
        var curTime = 0f;

        while(curTime < rangeChangeDelay) {
            yield return null;

            curTime += Time.deltaTime;

            var t = mRangeChangeEaseFunc(curTime, rangeChangeDelay, 0f, 0f);

            if(rangeUseSingleValue)
                mCurRange = new M8.RangeFloat(Mathf.LerpUnclamped(mFromRange.min, mToRange.min, t));
            else
                mCurRange = new M8.RangeFloat(Mathf.LerpUnclamped(mFromRange.min, mToRange.min, t), Mathf.LerpUnclamped(mFromRange.max, mToRange.max, t));

            ApplyCurRange();
        }

        mRout = null;
    }

    private void ApplyCurRange() {
        if(rangeLabel)
            rangeLabel.text = mAtmosphereAttr.GetValueRangeString(mCurRange);

        if(rangeSlider) {
            var rangeAvg = Mathf.Lerp(mCurRange.min, mCurRange.max, 0.5f);
            rangeSlider.normalizedValue = mAtmosphereAttr.rangeLimit.GetT(rangeAvg);
        }
    }
}
