using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class ModalHotspotInvestigate : M8.ModalController, M8.IModalPush, M8.IModalPop {
    public const string parmSeason = "inspectSeason"; //SeasonData
    public const string parmCriteriaGroup = "inspectCriteriaGrp"; //CriteriaGroup
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
    public AtmosphereAttributeBase altitudeAttributeData; //for display
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

    private SeasonData mCurSeason;
    private CriteriaGroup mCriteriaGroup;
    private LandscapePreview mLandscapePreview;

    private int mRegionIndex; //updated by slider

    private AtmosphereStat[] mCurStats;

    private M8.CacheList<AtmosphereAttributeRangeWidget> mAtmosphereStatWidgetActives;
    private M8.CacheList<AtmosphereAttributeRangeWidget> mAtmosphereStatWidgetCache;

    private bool mIsInit;

    private bool mIsLaunchValid;

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

        var landscapePreviewTelemetry = mLandscapePreview.landscapePreviewTelemetry;

        //setup landscape slider        
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
            if(mAltitudeChangeRout == null)
                StartCoroutine(DoAltitudeChange());
        }
    }

    IEnumerator DoAltitudeChange() {
        do {
            yield return null;

            var altitude = mLandscapePreview.altitude;

            altitudeSlider.value = altitude;
            altitudeValueLabel.text = altitudeAttributeData.GetValueString(Mathf.RoundToInt(altitude));

        } while(mLandscapePreview.isMoving);

        mAltitudeChangeRout = null;
    }

    private void UpdateAtmosphereStats() {
        var hotspotData = mLandscapePreview.hotspotData;

        if(mCurStats == null) { //first time
            mCurStats = hotspotData.GenerateModifiedStats(mCurSeason, mRegionIndex);

            for(int i = 0; i < mCurStats.Length; i++) {
                var stat = mCurStats[i];

                if(mAtmosphereStatWidgetCache.Count == 0) {
                    Debug.LogWarning("Ran out of atmosphere widget for: " + hotspotData.name);
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
            hotspotData.ApplyModifiedStats(mCurStats, mCurSeason, mRegionIndex);

            //assume 1-1 correspondence between stats and stat widgets
            for(int i = 0; i < mCurStats.Length; i++) {
                var widget = mAtmosphereStatWidgetActives[i];

                widget.SetRange(mCurStats[i].range);
            }
        }

        //update criteria
        mCriteriaGroup.Evaluate(mCurStats);

        //check if we can launch, show glow if so
        mIsLaunchValid = mCriteriaGroup.criticCountBad == 0 && mCriteriaGroup.criticCountGood >= GameData.instance.overworldLaunchCriticGoodCount;

        //TODO: hint system
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
