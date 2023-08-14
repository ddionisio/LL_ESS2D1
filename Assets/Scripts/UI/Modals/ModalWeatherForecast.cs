using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModalWeatherForecast : M8.ModalController, M8.IModalPush, M8.IModalPop {
    public const string parmCycleController = "cycleCtrl"; //CycleController (optional. if null, forecast is not generated)
    public const string parmCycleCurrentIndex = "cycleInd"; //int (optional. set this to determine which cycles are currently active)

    [Header("Item Info")]
    public WeatherForecastItemWidget forecastItemTemplate;
    public int forecastItemCapacity = 7;
    public Transform forecastItemRoot;

    [Header("Scroll Info")]
    public RectTransform scrollRoot;
    public int scrollItemPerPage = 3;
    public float scrollMoveDelay = 0.5f;

    private M8.CacheList<WeatherForecastItemWidget> mItemActives;
    private M8.CacheList<WeatherForecastItemWidget> mItemCache;

    private bool mIsInit;
    private int mCycleIndex = -1;

    private WeatherForecastItemWidget mItemExpanded;

    private int mScrollCurPageIndex;
    private int mScrollPageCount;
    private float mScrollPageWidth;

    private float mScrollCurPositionX;
    private float mScrollVelX;

    public void PagePrevious() {
        if(mScrollCurPageIndex > 0) {
            mScrollCurPageIndex--;

            mScrollVelX = -mScrollPageWidth * mScrollCurPageIndex;
        }
    }

    public void PageNext() {
        if(mScrollCurPageIndex < mScrollPageCount - 1) {
            mScrollCurPageIndex++;

            mScrollVelX = -mScrollPageWidth * mScrollCurPageIndex;
        }
    }

    void M8.IModalPop.Pop() {
        mItemExpanded = null;
    }

    void M8.IModalPush.Push(M8.GenericParams parms) {
        if(!mIsInit)
            Init();

        CycleController cycleCtrl = null;
        mCycleIndex = -1;

        mItemExpanded = null;

        if(parms != null) {
            if(parms.ContainsKey(parmCycleController))
                cycleCtrl = parms.GetValue<CycleController>(parmCycleController);
            if(parms.ContainsKey(parmCycleCurrentIndex))
                mCycleIndex = parms.GetValue<int>(parmCycleCurrentIndex);
        }

        if(cycleCtrl) {
            ClearForecastItems();

            if(mCycleIndex == -1)
                mCycleIndex = cycleCtrl.cycleCurIndex;

            var cycleCount = Mathf.Min(cycleCtrl.cycleData.cycles.Length, forecastItemCapacity);

            for(int i = 0; i < cycleCount; i++) {
                var itm = mItemCache.RemoveLast();

                var stats = cycleCtrl.GenerateAtmosphereStats(i);

                itm.SetCycleName(i);
                itm.Setup(cycleCtrl.cycleData.cycles[i].weather, stats, false);

                itm.clickCallback += OnItemClick;


                itm.transform.SetAsLastSibling();

                mItemActives.Add(itm);
            }
        }

        //disable previous cycles
        if(mCycleIndex >= 0 && mCycleIndex < mItemActives.Count) {
            for(int i = 0; i < mItemActives.Count; i++) {
                var itm = mItemActives[i];

                if(i >= mCycleIndex) {
                    itm.active = true;

                    if(i == mCycleIndex) {
                        itm.SetCycleNameToCurrent();
                        itm.isExpand = true;

                        mItemExpanded = itm;
                    }
                    else
                        itm.isExpand = false;
                }
                else
                    itm.active = false;
            }

            var visibleCount = mItemActives.Count - mCycleIndex;
            mScrollPageCount = Mathf.CeilToInt(((float)visibleCount) / scrollItemPerPage);
        }

        //initialize scroll        
        scrollRoot.anchoredPosition = Vector2.zero;
                
        mScrollCurPageIndex = 0;

        mScrollCurPositionX = 0f;
        mScrollVelX = 0f;
    }

    void Update() {
        var scrollPos = scrollRoot.anchoredPosition;
        if(scrollPos.x != mScrollCurPositionX) {
            scrollPos.x = Mathf.SmoothDamp(scrollPos.x, mScrollCurPositionX, ref mScrollVelX, scrollMoveDelay);

            scrollRoot.anchoredPosition = scrollPos;
        }
    }

    void OnItemClick(WeatherForecastItemWidget itm) {
        if(mItemExpanded != itm) {
            if(mItemExpanded) mItemExpanded.isExpand = false;

            itm.isExpand = true;

            mItemExpanded = itm;
        }
    }

    private void Init() {
        //initialize item cache
        mItemActives = new M8.CacheList<WeatherForecastItemWidget>(forecastItemCapacity);
        mItemCache = new M8.CacheList<WeatherForecastItemWidget>(forecastItemCapacity);

        for(int i = 0; i < forecastItemCapacity; i++) {
            var newItem = Instantiate(forecastItemTemplate, forecastItemRoot);
            newItem.active = false;
            mItemCache.Add(newItem);
        }

        forecastItemTemplate.active = false;

        //ensure correct anchor of scroll root
        scrollRoot.pivot = new Vector2(0f, 0.5f);

        var scrollRootRect = scrollRoot.rect;
        mScrollPageWidth = scrollRootRect.width;

        mIsInit = true;
    }

    private void ClearForecastItems() {
        for(int i = 0; i < mItemActives.Count; i++) {
            var itm = mItemActives[i];
            itm.clickCallback -= OnItemClick;
            itm.active = false;
            mItemCache.Add(itm);
        }

        mItemActives.Clear();
    }
}
