using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using TMPro;

public class WeatherForecastOverlayWidget : MonoBehaviour, IPointerClickHandler {
    [System.Serializable]
    public struct AtmosphereInfo {
        public AtmosphereAttributeBase atmosphere;
        public AtmosphereAttributeRangeWidget widget;

        public void Apply(AtmosphereStat[] stats) {
            for(int i = 0; i < stats.Length; i++) {
                var stat = stats[i];
                if(stat.atmosphere == atmosphere) {
                    widget.Setup(atmosphere, stat.range);
                    break;
                }
            }
        }
    }

    [Header("Weather Info")]
    public Image weatherIconImage;
    public bool weatherIconImageUseNative;
    public TMP_Text weatherNameLabel;
    public AtmosphereInfo[] atmosphereWidgets;

    [Header("Animation")]
    public M8.Animator.Animate animator;
    [M8.Animator.TakeSelector]
    public int takeDetailEnter = -1;
    [M8.Animator.TakeSelector]
    public int takeDetailExit = -1;

    private WeatherTypeData mWeather;
    private AtmosphereStat[] mWeatherStats;

    private Coroutine mChangeRout;

    public void CancelChange() {
        if(mChangeRout != null) {
            StopCoroutine(mChangeRout);
            mChangeRout = null;
        }

        if(takeDetailExit != -1)
            animator.ResetTake(takeDetailExit);

        ApplyWeatherInfo();
    }

    public void SetCycleInfo(WeatherTypeData weather, AtmosphereStat[] stats) {
        if(mChangeRout != null)
            StopCoroutine(mChangeRout);

        mChangeRout = StartCoroutine(DoChange(weather, stats));
    }

    void OnDisable() {
        mWeather = null;
        mWeatherStats = null;
    }

    void OnEnable() {
        mWeather = null;
        mWeatherStats = null;

        if(takeDetailEnter != -1)
            animator.ResetTake(takeDetailEnter);
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        ColonyController.instance.ShowWeatherForecast(false, true);
    }

    IEnumerator DoChange(WeatherTypeData weather, AtmosphereStat[] stats) {
        //only exit if we had previous weather
        if(mWeather) {
            if(takeDetailExit != -1)
                yield return animator.PlayWait(takeDetailExit);
        }

        //don't bother showing if no weather info
        if(weather) {
            mWeather = weather;
            mWeatherStats = stats;

            ApplyWeatherInfo();

            if(takeDetailEnter != -1)
                yield return animator.PlayWait(takeDetailEnter);
        }

        mChangeRout = null;
    }

    private void ApplyWeatherInfo() {
        if(mWeather) {
            if(weatherIconImage) {
                weatherIconImage.sprite = mWeather.image;
                if(weatherIconImageUseNative)
                    weatherIconImage.SetNativeSize();
            }

            if(weatherNameLabel)
                weatherNameLabel.text = M8.Localize.Get(mWeather.nameRef);
        }

        if(mWeatherStats != null) {
            for(int i = 0; i < atmosphereWidgets.Length; i++)
                atmosphereWidgets[i].Apply(mWeatherStats);
        }
    }
}
