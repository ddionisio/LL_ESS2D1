using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class ModalHotspotInvestigateGrid : M8.ModalController, M8.IModalPush, M8.IModalPop {
	public const string parmSeason = "inspectSeason"; //SeasonData
	public const string parmCriteriaGroup = "inspectCriteriaGrp"; //CriteriaGroup
	public const string parmGridCtrl = "inspectGridCtrl"; //LandscapeGridController

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

	[Header("Altitude Display")]
	public AtmosphereAttributeBase altitudeAttributeData; //for display
	public TMP_Text altitudeValueLabel;
	public float altitudeChangeDelay = 0.3f;

	[Header("Topography Display")]	
	public TMP_Text topographyItemTemplate; //not prefab
	public int topographyItemCapacity = 6;
	public Transform topographyItemRoot;

	[Header("Atmosphere Attributes Display")]
	public AtmosphereAttributeRangeWidget atmosphereStatTemplate; //not a prefab
	public int atmosphereStatCapacity = 8;
	public Transform atmosphereStatRoot;

	//TODO: altitude display

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
	private LandscapeGridController mLandscapeGridCtrl;

	private const int mRegionIndex = -1;

	private AtmosphereStat[] mCurStats;

	private M8.CacheList<TMP_Text> mTopographyItemActives;
	private M8.CacheList<TMP_Text> mTopographyItemCache;
	private GridData.TopographyType[] mTopographyTypeCache;

	private M8.CacheList<AtmosphereStatWidgetItem> mAtmosphereStatWidgetActives;
	private M8.CacheList<AtmosphereStatWidgetItem> mAtmosphereStatWidgetCache;

	private bool mIsInit;

	private bool mIsLaunchValid;

	private float mAltitudeValue;
	private float mAltitudeValueEnd;
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

	void M8.IModalPop.Pop() {
		if(mAltitudeChangeRout != null) {
			StopCoroutine(mAltitudeChangeRout);
			mAltitudeChangeRout = null;
		}

		if(signalListenSeasonChange) signalListenSeasonChange.callback -= OnSeasonToggle;

		ClearTopographyWidgets();
		ClearAttributeWidgets();

		mCurSeason = null;
		mCriteriaGroup = null;

		if(mLandscapeGridCtrl)
			mLandscapeGridCtrl.clickCallback -= OnLandscapeGridClickUpdate;

		mLandscapeGridCtrl = null;

		mCurStats = null;
	}

	void M8.IModalPush.Push(M8.GenericParams parms) {
		if(!mIsInit) {
			InitWidgets();

			mIsInit = true;
		}

		if(parms != null) {
			if(parms.ContainsKey(parmSeason))
				mCurSeason = parms.GetValue<SeasonData>(parmSeason);

			if(parms.ContainsKey(parmCriteriaGroup))
				mCriteriaGroup = parms.GetValue<CriteriaGroup>(parmCriteriaGroup);

			if(parms.ContainsKey(parmGridCtrl)) {
				mLandscapeGridCtrl = parms.GetValue<LandscapeGridController>(parmGridCtrl);

				if(mLandscapeGridCtrl) {
					mLandscapeGridCtrl.clickCallback += OnLandscapeGridClickUpdate;
				}
			}
		}

		if(signalListenSeasonChange) signalListenSeasonChange.callback += OnSeasonToggle;

		//hook up with grid controller for atmosphere update

		//setup hotspot info display
		var hotspotData = mLandscapeGridCtrl.hotspotData;
		if(hotspotData) {
			if(hotspotIconImage) {
				hotspotIconImage.sprite = hotspotData.climate.icon;
				if(hotspotIconUseNativeSize)
					hotspotIconImage.SetNativeSize();
			}

			if(hotspotRegionNameLabel) hotspotRegionNameLabel.text = M8.Localize.Get(hotspotData.nameRef);

			if(hotspotClimateNameLabel) hotspotClimateNameLabel.text = M8.Localize.Get(hotspotData.climate.nameRef);
		}

		UpdateAtmosphereStats();
	}

	void OnSeasonToggle(SeasonData season) {
		mCurSeason = season;

		//update attributes
		UpdateAtmosphereStats();
	}

	void OnLandscapeGridClickUpdate(LandscapeGridController ctrl) {
		//update topography
		ClearTopographyWidgets();

		var topographyCount = mLandscapeGridCtrl.GetShipTopographies(mTopographyTypeCache);
		for(int i = 0; i < topographyCount; i++) {
			var newItm = mTopographyItemCache.RemoveLast();
			newItm.text = GridData.instance.GetTopographyText(mTopographyTypeCache[i]);

			newItm.transform.SetAsLastSibling();
			newItm.gameObject.SetActive(true);

			mTopographyItemActives.Add(newItm);
		}
		//

		UpdateAtmosphereStats();
	}

	IEnumerator DoAltitudeChange() {
		float altitudeChangeVel = 0f;

		while(Mathf.RoundToInt(mAltitudeValue) != Mathf.RoundToInt(mAltitudeValueEnd)) {
			mAltitudeValue = Mathf.SmoothDamp(mAltitudeValue, mAltitudeValueEnd, ref altitudeChangeVel, altitudeChangeDelay);

			var iVal = Mathf.RoundToInt(mAltitudeValue);

			altitudeValueLabel.text = altitudeAttributeData.GetValueString(iVal);

			yield return null;
		}

		mAltitudeChangeRout = null;
	}

	private void UpdateAtmosphereStats() {
		var hotspotData = mLandscapeGridCtrl.hotspotData;

		if(mCurStats == null) { //first time
			mCurStats = hotspotData.GenerateModifiedStats(mCurSeason, mRegionIndex);

			//modify stats from grid
			if(mLandscapeGridCtrl.shipActive) {
				GridData.instance.ApplyMod(mCurStats, mLandscapeGridCtrl.atmosphereMod);
			}

			//setup active items (only show the ones needed for criteria)
			for(int i = 0; i < mCurStats.Length; i++) {
				var stat = mCurStats[i];

				if(mAtmosphereStatWidgetCache.Count == 0) {
					Debug.LogWarning("Ran out of atmosphere widget for: " + hotspotData.name);
					break;
				}

				M8.RangeFloat criteriaRange;
				if(!mCriteriaGroup.data.GetRange(stat.atmosphere, out criteriaRange))
					continue;

				var newItm = mAtmosphereStatWidgetCache.RemoveLast();

				newItm.widget.Setup(stat.atmosphere, stat.range);

				newItm.widget.SetupRangeValid(criteriaRange);

				newItm.transform.SetAsLastSibling();
				newItm.active = true;

				mAtmosphereStatWidgetActives.Add(newItm);
			}

			//initial value for altitude
			mAltitudeValue = mAltitudeValueEnd = mLandscapeGridCtrl.altitude;

			altitudeValueLabel.text = altitudeAttributeData.GetValueString(Mathf.RoundToInt(mAltitudeValue));
		}
		else { //update values
			hotspotData.ApplyModifiedStats(mCurStats, mCurSeason, mRegionIndex);

			//modify stats from grid
			if(mLandscapeGridCtrl.shipActive) {
				GridData.instance.ApplyMod(mCurStats, mLandscapeGridCtrl.atmosphereMod);
			}

			for(int i = 0; i < mAtmosphereStatWidgetActives.Count; i++) {
				var itm = mAtmosphereStatWidgetActives[i];

				var statInd = GetStatIndex(itm.widget.atmosphere);
				if(statInd != -1)
					itm.widget.SetRange(mCurStats[statInd].range);
			}

			mAltitudeValueEnd = mLandscapeGridCtrl.altitude;

			if(mAltitudeChangeRout == null)
				mAltitudeChangeRout = StartCoroutine(DoAltitudeChange());
		}

		//update criteria
		mCriteriaGroup.Evaluate(mCurStats, true);

		//check if we can launch, show glow if so
		mIsLaunchValid = mCriteriaGroup.criticCountBad == 0 && mCriteriaGroup.criticCountGood >= 1;

		if(launchAvailableGO) launchAvailableGO.SetActive(mIsLaunchValid);
		if(launchSelectable) launchSelectable.interactable = mIsLaunchValid;

		//TODO: hint system
	}

	private int GetStatIndex(AtmosphereAttributeBase atmosphere) {
		if(mCurStats != null) {
			for(int i = 0; i < mCurStats.Length; i++) {
				var stat = mCurStats[i];
				if(stat.atmosphere == atmosphere)
					return i;
			}
		}

		return -1;
	}

	private void InitWidgets() {
		//initialize atmosphere items
		mAtmosphereStatWidgetActives = new M8.CacheList<AtmosphereStatWidgetItem>(atmosphereStatCapacity);
		mAtmosphereStatWidgetCache = new M8.CacheList<AtmosphereStatWidgetItem>(atmosphereStatCapacity);

		for(int i = 0; i < atmosphereStatCapacity; i++) {
			var newItm = Instantiate(atmosphereStatTemplate);

			newItm.transform.SetParent(atmosphereStatRoot, false);
			newItm.gameObject.SetActive(false);

			mAtmosphereStatWidgetCache.Add(new AtmosphereStatWidgetItem { widget = newItm });
		}

		atmosphereStatTemplate.gameObject.SetActive(false);

		//initialize topography items
		mTopographyItemActives = new M8.CacheList<TMP_Text>(topographyItemCapacity);
		mTopographyItemCache = new M8.CacheList<TMP_Text>(topographyItemCapacity);

		for(int i = 0; i < topographyItemCapacity; i++) {
			var newItm = Instantiate(topographyItemTemplate);

			newItm.transform.SetParent(topographyItemRoot, false);
			newItm.gameObject.SetActive(false);

			mTopographyItemCache.Add(newItm);
		}

		topographyItemTemplate.gameObject.SetActive(false);

		mTopographyTypeCache = new GridData.TopographyType[topographyItemCapacity];
	}

	private void ClearTopographyWidgets() {
		for(int i = 0; i < mTopographyItemActives.Count; i++) {
			var itm = mTopographyItemActives[i];

			itm.gameObject.SetActive(false);

			mTopographyItemCache.Add(itm);
		}

		mTopographyItemActives.Clear();
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