using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModalWeatherForecast : M8.ModalController, M8.IModalPush, M8.IModalPop {
    public const string parmCycleController = "cycleCtrl"; //CycleController (optional. if null, forecast is not generated)
    public const string parmCycleCurrentIndex = "cycleInd"; //int (optional. set this to determine which cycles are currently active)
    public const string parmPause = "pause"; //bool

    [Header("Item Info")]
    public WeatherForecastItemWidget forecastItemTemplate;
    public int forecastItemCapacity = 7;
    public Transform forecastItemRoot;

    [Header("Layout Info")]
    public float layoutPadding = 4f;
    public float layoutItemSpace = 8f;

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

    private float mLayoutTotalWidth;

    private float mLastTime;
    private float mDeltaTime;

    private bool mIsPause;

    public void PagePrevious() {
        if(mScrollCurPageIndex > 0) {
            mScrollCurPageIndex--;

            mScrollCurPositionX = -mScrollPageWidth * mScrollCurPageIndex;

            mLastTime = GetTime();
        }
    }

    public void PageNext() {
        if(mScrollCurPageIndex < mScrollPageCount - 1) {
            mScrollCurPageIndex++;

            mScrollCurPositionX = -mScrollPageWidth * mScrollCurPageIndex;

            if(mScrollCurPositionX + mLayoutTotalWidth < mScrollPageWidth) {
                mScrollCurPositionX += mScrollPageWidth - (mScrollCurPositionX + mLayoutTotalWidth);
            }

            mLastTime = GetTime();
        }
    }

    void M8.IModalPop.Pop() {
        mItemExpanded = null;

        if(mIsPause) {
            M8.SceneManager.instance.Resume();
            mIsPause = false;
        }
    }

    void M8.IModalPush.Push(M8.GenericParams parms) {
        if(!mIsInit)
            Init();

        CycleController cycleCtrl = null;
        mCycleIndex = -1;

        mItemExpanded = null;

        mIsPause = false;

        if(parms != null) {
            if(parms.ContainsKey(parmCycleController))
                cycleCtrl = parms.GetValue<CycleController>(parmCycleController);
            if(parms.ContainsKey(parmCycleCurrentIndex))
                mCycleIndex = parms.GetValue<int>(parmCycleCurrentIndex);
            if(parms.ContainsKey(parmPause))
                mIsPause = parms.GetValue<bool>(parmPause);
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

        RefreshLayout();

        if(mIsPause)
            M8.SceneManager.instance.Pause();
    }

    private float GetTime() {
        return mIsPause ? Time.realtimeSinceStartup : Time.time;
    }

    void Update() {
        var scrollPos = scrollRoot.anchoredPosition;
        if(scrollPos.x != mScrollCurPositionX) {
            var t = GetTime();
            var dt = t - mLastTime;
            mLastTime = t;

            scrollPos.x = Mathf.SmoothDamp(scrollPos.x, mScrollCurPositionX, ref mScrollVelX, scrollMoveDelay, float.MaxValue, dt);

            scrollRoot.anchoredPosition = scrollPos;
        }
    }

    void OnItemClick(WeatherForecastItemWidget itm) {
        if(mItemExpanded != itm) {
            if(mItemExpanded) mItemExpanded.isExpand = false;

            itm.isExpand = true;

            mItemExpanded = itm;

            RefreshLayout();
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

    private void RefreshLayout() {
        var curX = layoutPadding;

        for(int i = 0; i < mItemActives.Count; i++) {
            var itm = mItemActives[i];

            if(itm.active) {
                itm.positionX = curX;

                curX += itm.width + layoutItemSpace;
            }
        }

        mLayoutTotalWidth = curX - layoutItemSpace + layoutPadding;
    }
}
