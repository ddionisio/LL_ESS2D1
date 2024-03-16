using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using TMPro;

public class WeatherForecastItemWidget : MonoBehaviour, IPointerClickHandler {
    [System.Serializable]
    public struct AtmosphereInfo {
        public AtmosphereAttributeBase atmosphere;
        public AtmosphereAttributeRangeWidget[] widgets;

        public void Apply(AtmosphereStat[] stats) {
            for(int i = 0; i < stats.Length; i++) {
                var stat = stats[i];
                if(stat.atmosphere == atmosphere) {
                    for(int j = 0; j < widgets.Length; j++) {
                        var widget = widgets[j];
                        if(widget)
                            widget.Setup(atmosphere, stat.range);
                    }
                    return;
                }
            }
        }
    }

    public AtmosphereInfo[] atmosphereWidgets;

    [Header("CycleInfo")]
    public TMP_Text dayLabel;

    [Header("Weather Info")]
    public Image weatherIconImage;
    public bool weatherIconImageUseNative;
    public TMP_Text weatherNameLabel;

    [Header("Simple Display")]
    public GameObject simpleRootGO;
    public float simpleWidth;

    [Header("Expand Display")]
    public GameObject expandHighlightGO;
    public GameObject expandRootGO;
    public float expandWidth;

    public bool active { get { return gameObject.activeSelf; } set { gameObject.SetActive(value); } }

    public float positionX { 
        get { return rectTransform.anchoredPosition.x; }
        set {
            var pos = rectTransform.anchoredPosition;
            pos.x = value;
            rectTransform.anchoredPosition = pos;
        }
    }

    public float width { get { return mIsExpand ? expandWidth : simpleWidth; } }

    public event System.Action<WeatherForecastItemWidget> clickCallback;

    public bool isExpand {
        get { return mIsExpand; }
        set {
            if(mIsExpand != value) {
                mIsExpand = value;
                ApplyExpand();
            }
        }
    }

    public RectTransform rectTransform { 
        get {
            if(!mRectTrans) mRectTrans = transform as RectTransform;
            return mRectTrans;
        } 
    }

    private RectTransform mRectTrans;
    private bool mIsExpand;

    public void SetCycleNameToCurrent() {
        var textRef = GameData.instance.cycleDayNameCurrentRef;
        if(dayLabel) dayLabel.text = string.IsNullOrEmpty(textRef) ? "" : M8.Localize.Get(GameData.instance.cycleDayNameCurrentRef);
    }

    public void SetCycleName(int cycleIndex) {
        if(dayLabel) dayLabel.text = GameData.instance.GetCycleName(cycleIndex);
    }

    public void Setup(WeatherTypeData weather, AtmosphereStat[] stats, bool aIsExpand) {
        if(weatherIconImage) {
            weatherIconImage.sprite = weather.image;
            if(weatherIconImageUseNative)
                weatherIconImage.SetNativeSize();
        }

        if(weatherNameLabel)
            weatherNameLabel.text = weather.GetNameType();

        for(int i = 0; i < atmosphereWidgets.Length; i++)
            atmosphereWidgets[i].Apply(stats);

        mIsExpand = aIsExpand;
        ApplyExpand();
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        clickCallback?.Invoke(this);
    }

    private void ApplyExpand() {
        var sizeDelta = rectTransform.sizeDelta;

        if(mIsExpand) {
            if(simpleRootGO) simpleRootGO.SetActive(false);
            if(expandRootGO) expandRootGO.SetActive(true);
            if(expandHighlightGO) expandHighlightGO.SetActive(true);

            sizeDelta.x = expandWidth;
        }
        else {
            if(simpleRootGO) simpleRootGO.SetActive(true);
            if(expandRootGO) expandRootGO.SetActive(false);
            if(expandHighlightGO) expandHighlightGO.SetActive(false);

            sizeDelta.x = simpleWidth;
        }

        rectTransform.sizeDelta = sizeDelta;
    }
}
