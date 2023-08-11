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
        public int statIndex;

        public bool active { get { return widget.gameObject.activeSelf; } set { widget.gameObject.SetActive(value); } }
        public Transform transform { get { return widget.transform; } }
    }

    [Header("Hotspot Info Display")]
    public Image hotspotIconImage;
    public bool hotspotIconUseNativeSize;

    public TMP_Text hotspotRegionNameLabel;
    [M8.Localize]
    public string hotspotRegionTitleRef;
    public string hotspotRegionNameFormat = "{0}: {1}";

    public TMP_Text hotspotClimateNameLabel;
    [M8.Localize]
    public string hotspotClimateTitleRef;
    public string hotspotClimateNameFormat = "{0}: {1}";

    [Header("Atmosphere Attributes Display")]
    public AtmosphereAttributeBase[] atmosphereStatDisplays; //which attributes to display, in order
    public AtmosphereAttributeRangeWidget atmosphereStatTemplate; //not a prefab
    public Transform atmosphereStatRoot;

    [Header("Altitude Display")]
    public AtmosphereAttributeBase altitudeAttributeData; //for display
    public TMP_Text altitudeValueLabel;
    public Slider altitudeSlider;

    [Header("Landscape Preview")]
    public Slider landscapeRegionSlider;

    [Header("Season Select")]
    public SeasonSelectWidget seasonSelect;

    //[Header("Launch")]

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
            Debug.Log("Can Launch!");
        }
        else {
            Debug.Log("Cannot Launch!");
        }
    }

    void M8.IModalPop.Pop() {
        if(mAltitudeChangeRout != null) {
            StopCoroutine(mAltitudeChangeRout);
            mAltitudeChangeRout = null;
        }

        if(signalListenSeasonChange) signalListenSeasonChange.callback -= OnSeasonToggle;

        ClearAttributeWidgets();

        mCurSeason = null;
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

        if(hotspotIconImage) {
            hotspotIconImage.sprite = hotspotData.climate.icon;
            if(hotspotIconUseNativeSize)
                hotspotIconImage.SetNativeSize();
        }

        if(hotspotRegionNameLabel) hotspotRegionNameLabel.text = string.Format(hotspotRegionNameFormat, M8.Localize.Get(hotspotRegionTitleRef), M8.Localize.Get(hotspotData.nameRef));

        if(hotspotClimateNameLabel) hotspotClimateNameLabel.text = string.Format(hotspotClimateNameFormat, M8.Localize.Get(hotspotClimateTitleRef), M8.Localize.Get(hotspotData.climate.nameRef));

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
            for(int i = 0; i < atmosphereStatDisplays.Length; i++) {
                int statIndex = -1;
                for(int j = 0; j < mCurStats.Length; j++) {
                    if(mCurStats[j].atmosphere == atmosphereStatDisplays[i]) {
                        statIndex = j;
                        break;
                    }
                }

                if(statIndex != -1) {
                    var stat = mCurStats[statIndex];

                    if(mAtmosphereStatWidgetCache.Count == 0) {
                        Debug.LogWarning("Ran out of atmosphere widget for: " + hotspotData.name);
                        break;
                    }

                    var newItm = mAtmosphereStatWidgetCache.RemoveLast();

                    newItm.widget.Setup(stat.atmosphere, stat.range);
                    newItm.statIndex = statIndex;

                    newItm.transform.SetAsLastSibling();
                    newItm.active = true;

                    mAtmosphereStatWidgetActives.Add(newItm);
                }
            }
        }
        else { //update values
            hotspotData.ApplyModifiedStats(mCurStats, mCurSeason, mRegionIndex);

            for(int i = 0; i < mAtmosphereStatWidgetActives.Count; i++) {
                var itm = mAtmosphereStatWidgetActives[i];

                itm.widget.SetRange(mCurStats[itm.statIndex].range);
            }
        }

        //update criteria
        mCriteriaGroup.Evaluate(mCurStats);

        //check if we can launch, show glow if so
        mIsLaunchValid = mCriteriaGroup.criticCountBad == 0 && mCriteriaGroup.criticCountGood >= GameData.instance.overworldLaunchCriticGoodCount;

        //TODO: hint system
    }

    private void InitAttributeWidgets() {
        var capacity = atmosphereStatDisplays.Length;

        mAtmosphereStatWidgetActives = new M8.CacheList<AtmosphereStatWidgetItem>(capacity);
        mAtmosphereStatWidgetCache = new M8.CacheList<AtmosphereStatWidgetItem>(capacity);

        for(int i = 0; i < capacity; i++) {
            var newItm = Instantiate(atmosphereStatTemplate);

            newItm.transform.SetParent(atmosphereStatRoot, false);
            newItm.gameObject.SetActive(false);

            mAtmosphereStatWidgetCache.Add(new AtmosphereStatWidgetItem { widget = newItm, statIndex = -1 });
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
