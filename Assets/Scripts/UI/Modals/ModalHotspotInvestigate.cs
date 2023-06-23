using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class ModalHotspotInvestigate : M8.ModalController, M8.IModalPush, M8.IModalPop {
    public const string parmHotspot = "inspectHotspot"; //HotspotData
    public const string parmSeason = "inspectSeason"; //SeasonData
    public const string parmLandscape = "inspectLandscape"; //LandscapePreview

    [Header("Hotspot Info Display")]
    public Image hotspotClimateImage;
    public TMP_Text hotspotRegionNameLabel;
    public TMP_Text hotspotClimateNameLabel;

    [Header("Atmosphere Attributes Display")]
    public AtmosphereAttributeRangeWidget atmosphereStatTemplate; //not a prefab
    public int atmosphereStatCapacity = 10;
    public Transform atmosphereStatRoot;

    [Header("Altitude Display")]
    public TMP_Text altitudeValueLabel;
    public Slider altitudeSlider;

    [Header("Landscape Preview")]
    public Slider landscapeRegionSlider;

    [Header("Season Select")]
    public SeasonSelectWidget seasonSelect;

    [Header("Launch")]
    public Button launchButton;

    [Header("Signal Listen")]
    public SignalSeasonData signalListenSeasonChange;

    [Header("Signal Invoke")]
    public M8.Signal signalInvokeBack;
    public M8.Signal signalInvokeLaunch;

    private HotspotData mHotspot;
    private SeasonData mCurSeason;
    private LandscapePreview mLandscapePreview;

    private int mRegionIndex; //updated by slider

    private AtmosphereStat[] mCurStats;

    private M8.CacheList<AtmosphereAttributeRangeWidget> mAtmosphereStatWidgetActives;
    private M8.CacheList<AtmosphereAttributeRangeWidget> mAtmosphereStatWidgetCache;

    private bool mIsInit;

    public void Back() {
        signalInvokeBack?.Invoke();
    }

    public void Launch() {
        //determine if all critics are satisfied, use hint system
    }

    void M8.IModalPop.Pop() {
        if(signalListenSeasonChange) signalListenSeasonChange.callback -= OnSeasonToggle;

        ClearAttributeWidgets();

        mHotspot = null;
        mCurSeason = null;
        mLandscapePreview = null;
    }

    void M8.IModalPush.Push(M8.GenericParams parms) {
        if(!mIsInit) {
            InitAttributeWidgets();

            mIsInit = true;
        }

        if(parms != null) {
            if(parms.ContainsKey(parmHotspot))
                mHotspot = parms.GetValue<HotspotData>(parmHotspot);

            if(parms.ContainsKey(parmSeason))
                mCurSeason = parms.GetValue<SeasonData>(parmSeason);

            if(parms.ContainsKey(parmLandscape))
                mLandscapePreview = parms.GetValue<LandscapePreview>(parmLandscape);
        }

        if(signalListenSeasonChange) signalListenSeasonChange.callback += OnSeasonToggle;

        UpdateAtmosphereStats();
    }

    void OnSeasonToggle(SeasonData season) {
        mCurSeason = season;

        //update attributes
        UpdateAtmosphereStats();
    }

    private void UpdateAtmosphereStats() {
        if(mCurStats == null) { //first time
            mCurStats = mHotspot.GenerateModifiedStats(mCurSeason, mRegionIndex);

            for(int i = 0; i < mCurStats.Length; i++) {
                var stat = mCurStats[i];

                if(mAtmosphereStatWidgetCache.Count == 0) {
                    Debug.LogWarning("Ran out of atmosphere widget for: " + mHotspot.name);
                    break;
                }

                var newItm = mAtmosphereStatWidgetCache.RemoveLast();

                newItm.Setup(stat.atmosphere, stat.range);

                newItm.transform.SetAsLastSibling();
                newItm.gameObject.SetActive(true);

                mAtmosphereStatWidgetActives.Add(newItm);
            }
        }
        else { //update values
            mHotspot.ApplyModifiedStats(mCurStats, mCurSeason, mRegionIndex);

            //assume 1-1 correspondence between stats and stat widgets
            for(int i = 0; i < mCurStats.Length; i++) {
                var widget = mAtmosphereStatWidgetActives[i];

                widget.SetRange(mCurStats[i].range);
            }
        }
    }

    private void InitAttributeWidgets() {
        mAtmosphereStatWidgetActives = new M8.CacheList<AtmosphereAttributeRangeWidget>(atmosphereStatCapacity);
        mAtmosphereStatWidgetCache = new M8.CacheList<AtmosphereAttributeRangeWidget>(atmosphereStatCapacity);

        for(int i = 0; i < atmosphereStatCapacity; i++) {
            var newItm = Instantiate(atmosphereStatTemplate);

            newItm.transform.SetParent(atmosphereStatRoot, false);
            newItm.gameObject.SetActive(false);

            mAtmosphereStatWidgetCache.Add(newItm);
        }

        atmosphereStatTemplate.gameObject.SetActive(false);
    }

    private void ClearAttributeWidgets() {
        for(int i = 0; i < mAtmosphereStatWidgetActives.Count; i++) {
            var itm = mAtmosphereStatWidgetActives[i];

            itm.gameObject.SetActive(false);

            mAtmosphereStatWidgetCache.Add(itm);
        }

        mAtmosphereStatWidgetActives.Clear();
    }
}
