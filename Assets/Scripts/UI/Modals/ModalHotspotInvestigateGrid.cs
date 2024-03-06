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

	private int mRegionIndex = -1;

	private AtmosphereStat[] mCurStats;

	private M8.CacheList<AtmosphereStatWidgetItem> mAtmosphereStatWidgetActives;
	private M8.CacheList<AtmosphereStatWidgetItem> mAtmosphereStatWidgetCache;

	private bool mIsInit;

	private bool mIsLaunchValid;

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
	}

	void M8.IModalPush.Push(M8.GenericParams parms) {
	}

	private void UpdateAtmosphereStats() {
		var hotspotData = mLandscapeGridCtrl.hotspotData;

		if(mCurStats == null) { //first time
			mCurStats = hotspotData.GenerateModifiedStats(mCurSeason, mRegionIndex);

			//modify stats from grid

			//setup active items
			for(int i = 0; i < mCurStats.Length; i++) {
				var stat = mCurStats[i];

				if(mAtmosphereStatWidgetCache.Count == 0) {
					Debug.LogWarning("Ran out of atmosphere widget for: " + hotspotData.name);
					break;
				}

				var newItm = mAtmosphereStatWidgetCache.RemoveLast();

				newItm.widget.Setup(stat.atmosphere, stat.range);

				M8.RangeFloat criteriaRange;
				if(mCriteriaGroup.data.GetRange(stat.atmosphere, out criteriaRange)) {
					newItm.widget.SetupRangeValid(criteriaRange);
				}
				
				newItm.transform.SetAsLastSibling();
				newItm.active = true;

				mAtmosphereStatWidgetActives.Add(newItm);
			}
		}
		else { //update values
			hotspotData.ApplyModifiedStats(mCurStats, mCurSeason, mRegionIndex);

			//modify stats from grid

			for(int i = 0; i < mAtmosphereStatWidgetActives.Count; i++) {
				var itm = mAtmosphereStatWidgetActives[i];

				itm.widget.SetRange(mCurStats[i].range);
			}
		}

		//update criteria
		mCriteriaGroup.Evaluate(mCurStats, true);

		//check if we can launch, show glow if so
		mIsLaunchValid = mCriteriaGroup.criticCountBad == 0 && mCriteriaGroup.criticCountGood >= 1;

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