using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class ModalHotspotInvestigate : M8.ModalController, M8.IModalPush, M8.IModalPop {
    public const string parmSeason = "inspectSeason"; //SeasonData
    public const string parmCriteriaGroup = "inspectCriteriaGrp"; //CriteriaGroup
    public const string parmLandscape = "inspectLandscape"; //LandscapePreview

    public struct AtmosphereStatWidgetItem {
        public AtmosphereAttributeRangeWidget widget;

        public bool active { get { return widget.gameObject.activeSelf; } set { widget.gameObject.SetActive(value); } }
        public Transform transform { get { return widget.transform; } }
    }

    [Header("Hotspot Display")]
    public Image hotspotIconImage;
    public bool hotspotIconUseNativeSize;

    public TMP_Text hotspotRegionNameLabel;
    public TMP_Text hotspotClimateNameLabel;

    [Header("Season Display")]
    public Image seasonIconImage;
    public bool seasonIconUseNativeSize;
    public TMP_Text seasonNameLabel;

    [Header("Atmosphere Attributes Display")]
    public AtmosphereAttributeRangeWidget atmosphereStatTemplate; //not a prefab
    public int atmosphereStatCapacity = 8;
    public Transform atmosphereStatRoot;

    [Header("Altitude Display")]
    public AtmosphereAttributeBase altitudeAttributeData; //for display
    public TMP_Text altitudeValueLabel;
    public Slider altitudeSlider;

    [Header("Landscape Preview")]
    public Slider landscapeRegionSlider;

    [Header("Season Select")]
    public SeasonSelectWidget seasonSelect;

    [Header("Launch")]
    public GameObject launchAvailableGO;
    public Selectable launchSelectable;

    [Header("Signal Listen")]
    public SignalSeasonData signalListenSeasonChange;

    [Header("Signal Invoke")]
    public M8.Signal signalInvokeBack;
    public M8.SignalInteger signalInvokeLaunch; //param = regionIndex

    private SeasonData mCurSeason;
    private CriteriaGroup mCriteriaGroup;
    private LandscapePreview mLandscapePreview;

    private int mRegionIndex; //updated by slider

    private AtmosphereStat[] mCurStats;

    private M8.CacheList<AtmosphereStatWidgetItem> mAtmosphereStatWidgetActives;
    private M8.CacheList<AtmosphereStatWidgetItem> mAtmosphereStatWidgetCache;

    private bool mIsInit;

    private bool mIsLaunchValid;

    private float mAltitudeEnd;
    private Coroutine mAltitudeChangeRout;

    public void Back() {
        signalInvokeBack?.Invoke();
    }

    public void Launch() {
        //determine if all critics are satisfied, otherwise activate the hint system
        if(mIsLaunchValid) {
            //Debug.Log("Can Launch!");
            if(signalInvokeLaunch) signalInvokeLaunch.Invoke(mRegionIndex);
        }
        else {
            Debug.Log("Cannot Launch!");
        }
    }

    public void RegionPrevious() {
        var val = Mathf.RoundToInt(landscapeRegionSlider.value);
        if(val > 0)
            landscapeRegionSlider.value = val - 1;
    }

    public void RegionNext() {
        var val = Mathf.RoundToInt(landscapeRegionSlider.value);
        if(val < mLandscapePreview.regionCount - 1)
            landscapeRegionSlider.value = val + 1;
    }

    void M8.IModalPop.Pop() {
        if(mAltitudeChangeRout != null) {
            StopCoroutine(mAltitudeChangeRout);
            mAltitudeChangeRout = null;
        }

        if(signalListenSeasonChange) signalListenSeasonChange.callback -= OnSeasonToggle;

        ClearAttributeWidgets();

        mCurSeason = null;
        mCriteriaGroup = null;
		mLandscapePreview = null;

        mCurStats = null;
    }

    void M8.IModalPush.Push(M8.GenericParams parms) {
        if(!mIsInit) {
            InitAttributeWidgets();

            mIsInit = true;
        }

        if(parms != null) {
            if(parms.ContainsKey(parmSeason))
                mCurSeason = parms.GetValue<SeasonData>(parmSeason);

            if(parms.ContainsKey(parmCriteriaGroup))
                mCriteriaGroup = parms.GetValue<CriteriaGroup>(parmCriteriaGroup);

            if(parms.ContainsKey(parmLandscape))
                mLandscapePreview = parms.GetValue<LandscapePreview>(parmLandscape);
        }

        if(signalListenSeasonChange) signalListenSeasonChange.callback += OnSeasonToggle;

        //set landscape preview display, and apply current season
        mRegionIndex = mLandscapePreview.curRegionIndex;

        //setup hotspot info display
        var hotspotData = mLandscapePreview.hotspotData;
        if(hotspotData) {
            if(hotspotIconImage) {
                hotspotIconImage.sprite = hotspotData.climate.icon;
                if(hotspotIconUseNativeSize)
                    hotspotIconImage.SetNativeSize();
            }

            if(hotspotRegionNameLabel) hotspotRegionNameLabel.text = M8.Localize.Get(hotspotData.nameRef);

            if(hotspotClimateNameLabel) hotspotClimateNameLabel.text = M8.Localize.Get(hotspotData.climate.nameRef);
        }

        //setup season info display
        if(mCurSeason) {
            if(seasonIconImage) {
                seasonIconImage.sprite = mCurSeason.icon;
                if(seasonIconUseNativeSize)
                    seasonIconImage.SetNativeSize();
            }

            if(seasonNameLabel)
                seasonNameLabel.text = M8.Localize.Get(mCurSeason.nameRef);
        }

        //setup landscape slider
        var landscapePreviewTelemetry = mLandscapePreview.landscapePreviewTelemetry;

        landscapeRegionSlider.minValue = 0f;
        landscapeRegionSlider.maxValue = landscapePreviewTelemetry.regions.Length - 1;
        landscapeRegionSlider.value = mRegionIndex;
        landscapeRegionSlider.wholeNumbers = true;
        landscapeRegionSlider.onValueChanged.AddListener(OnLandscapeRegionChange);

        //setup altitude display
        altitudeSlider.minValue = landscapePreviewTelemetry.altitudeRange.min;
        altitudeSlider.maxValue = landscapePreviewTelemetry.altitudeRange.max;
        altitudeSlider.value = mLandscapePreview.altitude;

        altitudeValueLabel.text = altitudeAttributeData.GetValueString(Mathf.RoundToInt(mLandscapePreview.altitude));

        seasonSelect.Setup(mCurSeason);

        UpdateAtmosphereStats();
    }

    void OnSeasonToggle(SeasonData season) {
        mCurSeason = season;

        //update attributes
        UpdateAtmosphereStats();
    }

    void OnLandscapeRegionChange(float val) {
        var newRegionIndex = Mathf.RoundToInt(val);

        if(mRegionIndex != newRegionIndex) {
            mRegionIndex = newRegionIndex;

            //update attributes
            UpdateAtmosphereStats();

            //move landscape view
            mLandscapePreview.curRegionIndex = mRegionIndex;

            //update altitude
            mAltitudeEnd = mLandscapePreview.altitude;

            if(mAltitudeChangeRout == null)
                StartCoroutine(DoAltitudeChange());
        }
    }

    IEnumerator DoAltitudeChange() {
        float delay = mLandscapePreview.regionMoveDelay;
        float vel = 0f;
        float curAltitude = altitudeSlider.value;

        while(curAltitude != mAltitudeEnd) {
            curAltitude = Mathf.SmoothDamp(curAltitude, mAltitudeEnd, ref vel, delay);
            altitudeSlider.value = curAltitude;

            altitudeValueLabel.text = altitudeAttributeData.GetValueString(Mathf.RoundToInt(curAltitude));

            yield return null;
        }

        mAltitudeChangeRout = null;
    }

    private void UpdateAtmosphereStats() {
        var hotspotData = mLandscapePreview.hotspotData;

        if(mCurStats == null) { //first time
            mCurStats = hotspotData.GenerateModifiedStats(mCurSeason, mRegionIndex);

            //setup active items
            for(int i = 0; i < mCurStats.Length; i++) {
                var stat = mCurStats[i];

                if(mAtmosphereStatWidgetCache.Count == 0) {
                    Debug.LogWarning("Ran out of atmosphere widget for: " + hotspotData.name);
                    break;
                }

                var newItm = mAtmosphereStatWidgetCache.RemoveLast();

                newItm.widget.Setup(stat.atmosphere, stat.range);

                newItm.transform.SetAsLastSibling();
                newItm.active = true;

                mAtmosphereStatWidgetActives.Add(newItm);
            }
        }
        else { //update values
            hotspotData.ApplyModifiedStats(mCurStats, mCurSeason, mRegionIndex);

            for(int i = 0; i < mAtmosphereStatWidgetActives.Count; i++) {
                var itm = mAtmosphereStatWidgetActives[i];

                itm.widget.SetRange(mCurStats[i].range);
            }
        }

        //update criteria
        var landscapePreviewTelemetry = mLandscapePreview.landscapePreviewTelemetry;
        if(landscapePreviewTelemetry.regions[mRegionIndex].criticsOverride)
            mCriteriaGroup.ApplyCompares(landscapePreviewTelemetry.regions[mRegionIndex].criticsCompares);
        else
            mCriteriaGroup.Evaluate(mCurStats, false);

        //check if we can launch, show glow if so
        mIsLaunchValid = mCriteriaGroup.criticCountBad == 0 && mCriteriaGroup.criticCountGood >= GameData.instance.overworldLaunchCriticGoodCount;

        if(launchAvailableGO) launchAvailableGO.SetActive(mIsLaunchValid);
        if(launchSelectable) launchSelectable.interactable = mIsLaunchValid;

        //TODO: hint system
    }

    private void InitAttributeWidgets() {
        mAtmosphereStatWidgetActives = new M8.CacheList<AtmosphereStatWidgetItem>(atmosphereStatCapacity);
        mAtmosphereStatWidgetCache = new M8.CacheList<AtmosphereStatWidgetItem>(atmosphereStatCapacity);

        for(int i = 0; i < atmosphereStatCapacity; i++) {
            var newItm = Instantiate(atmosphereStatTemplate);

            newItm.transform.SetParent(atmosphereStatRoot, false);
            newItm.gameObject.SetActive(false);

            mAtmosphereStatWidgetCache.Add(new AtmosphereStatWidgetItem { widget = newItm });
        }

        atmosphereStatTemplate.gameObject.SetActive(false);
    }

    private void ClearAttributeWidgets() {
        for(int i = 0; i < mAtmosphereStatWidgetActives.Count; i++) {
            var itm = mAtmosphereStatWidgetActives[i];

            itm.active = false;

            mAtmosphereStatWidgetCache.Add(itm);
        }

        mAtmosphereStatWidgetActives.Clear();
    }
}
