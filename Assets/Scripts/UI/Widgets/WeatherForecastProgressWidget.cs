using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeatherForecastProgressWidget : MonoBehaviour {
    public struct IconItem {
        public Image image;
        public M8.UI.Graphics.ColorFromPalette palette;

        public bool active { get { return image.gameObject.activeSelf; } set { image.gameObject.SetActive(value); } }

        public float positionX {
            get { return image.rectTransform.anchoredPosition.x; }
            set {
                var pos = image.rectTransform.anchoredPosition;
                pos.x = value;
                image.rectTransform.anchoredPosition = pos;
            }
        }

        public void ApplySprite(Sprite spr) {
            image.sprite = spr;
        }

        public void ApplyPaletteIndex(int ind) {
            if(palette) palette.index = ind;
        }

        public IconItem(Image aImage) {
            image = aImage;
            palette = image.GetComponent<M8.UI.Graphics.ColorFromPalette>();
        }
    }

    [Header("Progress Display")]
    public Slider progressSlider;

    [Header("Weather Forecast Display")]
    public RectTransform forecastRoot; //ensure anchor pivot is middle left
    public Image forecastWeatherIconTemplate; //ensure anchor pivot is middle left
    public int forecastWeatherIconCapacity = 7;
    public int forecastWeatherIconPaletteIndexCurrent;
    public int forecastWeatherIconPaletteIndexNext;

    public bool isPlay {
        get { return mIsPlay; }
        set {
            if(mIsPlay != value) {
                mIsPlay = value;

                if(mRout != null)
                    StopCoroutine(mRout);

                if(mIsPlay) {
                    if(gameObject.activeInHierarchy)
                        mRout = StartCoroutine(DoPlay());
                }
                else
                    mRout = null;
            }
        }
    }

    private float mForecastWeatherIconWidth;
    private float mForecastTotalWidth;

    private Coroutine mRout;

    private M8.CacheList<IconItem> mForecastIconActives;
    private M8.CacheList<IconItem> mForecastIconCache;

    private bool mIsPlay;
    private bool mIsInit;

    public void Setup(CycleData cycleData) {
        if(mIsInit)
            ClearForecastIconActives();
        else {
            mForecastIconActives = new M8.CacheList<IconItem>(forecastWeatherIconCapacity);
            mForecastIconCache = new M8.CacheList<IconItem>(forecastWeatherIconCapacity);

            for(int i = 0; i < forecastWeatherIconCapacity; i++) {
                var img = Instantiate(forecastWeatherIconTemplate, forecastRoot);
                img.gameObject.SetActive(false);
                mForecastIconCache.Add(new IconItem(img));
            }

            mForecastWeatherIconWidth = forecastWeatherIconTemplate.rectTransform.rect.width;

            forecastWeatherIconTemplate.gameObject.SetActive(false);
        }

        float iconExt = mForecastWeatherIconWidth * 0.5f;
        float curX = iconExt;

        var weatherInfs = cycleData.cycles;
        for(int i = 0; i < weatherInfs.Length; i++) {
            if(mForecastIconCache.Count == 0)
                break;

            var img = mForecastIconCache.RemoveLast();
            img.ApplySprite(weatherInfs[i].weather.icon);
            img.ApplyPaletteIndex(forecastWeatherIconPaletteIndexNext);
            img.positionX = curX + iconExt;
            img.active = true;

            curX += mForecastWeatherIconWidth;

            mForecastIconActives.Add(img);
        }

        mForecastTotalWidth = curX;

        progressSlider.value = 0f;

        forecastRoot.anchoredPosition = Vector2.zero;

        mIsPlay = false;
    }

    void OnDisable() {
        if(mRout != null) {
            StopCoroutine(mRout);
            mRout = null;
        }
    }

    void OnEnable() {
        if(mIsPlay)
            mRout = StartCoroutine(DoPlay());
    }

    IEnumerator DoPlay() {
        yield return null;

        var cycleCtrl = ColonyController.instance.cycleController;

        int curCycleInd = -1;
                
        while(cycleCtrl.isRunning) {
            var cycleInd = cycleCtrl.cycleCurIndex;
            if(curCycleInd != cycleInd) {
                if(curCycleInd != -1) mForecastIconActives[curCycleInd].ApplyPaletteIndex(forecastWeatherIconPaletteIndexNext);

                curCycleInd = cycleInd;

                if(curCycleInd != -1) mForecastIconActives[curCycleInd].ApplyPaletteIndex(forecastWeatherIconPaletteIndexCurrent);
            }

            var cycleCurCount = (float)cycleInd;
            cycleCurCount += Mathf.Clamp01(cycleCtrl.cycleCurElapsed / cycleCtrl.cycleDuration);

            var t = Mathf.Clamp01(cycleCurCount / cycleCtrl.cycleCount);

            progressSlider.normalizedValue = t;

            forecastRoot.anchoredPosition = new Vector2(Mathf.Lerp(0f, -mForecastTotalWidth, t), 0f);

            yield return null;
        }

        mRout = null;
    }

    private void ClearForecastIconActives() {
        for(int i = 0; i < mForecastIconActives.Count; i++) {
            var img = mForecastIconActives[i];
            img.active = false;
            mForecastIconCache.Add(img);
        }

        mForecastIconActives.Clear();
    }
}
