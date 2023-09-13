using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class HotspotSelectInfoWidget : MonoBehaviour {
    [Header("Display")]
    public GameObject rootGO;

    [Header("Region Info Display")]
    public TMP_Text regionNameLabel;
    public TMP_Text climateNameLabel;
    public Image climateIcon;
    public bool climateIconUseNative;

    [Header("Atmosphere Stats Display")]
    public RectTransform atmosphereStatsRoot;
    public HotspotSelectInfoStatItemWidget atmosphereStatItemTemplate; //not prefab
    public int atmosphereStatItemCapacity = 3;

    [Header("Analysis State Display")]
    public GameObject analysisWaitGO;
	public GameObject analysisIncompatibleGO;
	public GameObject analysisInvestigateGO;

	[Header("Animation")]
	public M8.Animator.Animate animator;
	[M8.Animator.TakeSelector]
	public int takeEnter = -1;
	[M8.Animator.TakeSelector]
	public int takeExit = -1;

	[Header("Signal Listen")]
	public SignalHotspot signalListenHotspotChanged;
	public SignalHotspot signalListenHotspotAnalyzeComplete;
	public SignalSeasonData signalListenSeasonToggle;

	[Header("Signal Invoke")]
	public SignalHotspot signalInvokeInvestigate;

	public bool rootActive { get { return rootGO ? rootGO.activeSelf : false; } set { if(rootGO) rootGO.SetActive(value); } }

    private Hotspot mHotspot;

    private M8.CacheList<HotspotSelectInfoStatItemWidget> mAtmosphereStatActives;
	private M8.CacheList<HotspotSelectInfoStatItemWidget> mAtmosphereStatCache;

	private bool mIsAtmosphereStatInit;

	public void Investigate() {
		signalInvokeInvestigate?.Invoke(mHotspot);
	}

	void OnDisable() {
		if(signalListenHotspotChanged) signalListenHotspotChanged.callback -= OnHotspotChanged;
		if(signalListenHotspotAnalyzeComplete) signalListenHotspotAnalyzeComplete.callback -= OnHotspotAnalyzeComplete;
		if(signalListenSeasonToggle) signalListenSeasonToggle.callback -= OnSeasonToggle;
	}

	void OnEnable() {
		if(signalListenHotspotChanged) signalListenHotspotChanged.callback += OnHotspotChanged;
		if(signalListenHotspotAnalyzeComplete) signalListenHotspotAnalyzeComplete.callback += OnHotspotAnalyzeComplete;
		if(signalListenSeasonToggle) signalListenSeasonToggle.callback += OnSeasonToggle;

		rootActive = false;

		if(OverworldController.isInstantiated) {
			var curHotspot = OverworldController.instance.hotspotCurrent;
			OnHotspotChanged(curHotspot);
		}
		else
			OnHotspotChanged(null);
	}

	void Awake() {
		if(animator) animator.takeCompleteCallback += OnAnimatorTakeComplete;
	}

	void OnHotspotChanged(Hotspot hotspot) {
		mHotspot = hotspot;

		if(mHotspot) {
			//update displays
			var hotspotData = mHotspot.data;

			if(regionNameLabel) regionNameLabel.text = M8.Localize.Get(hotspotData.nameRef);

			if(climateNameLabel) climateNameLabel.text = M8.Localize.Get(hotspotData.climate.nameRef);

			if(climateIcon) {
				climateIcon.sprite = hotspotData.climate.icon;
				if(climateIconUseNative)
					climateIcon.SetNativeSize();
			}

			RefreshAtmosphereStats();

			//show
			if(!rootActive) {
				rootActive = true;

				if(takeEnter != -1)
					animator.Play(takeEnter);
			}
		}
		else {
			//hide
			if(rootActive) {
				if(takeExit != -1)
					animator.Play(takeExit);
				else
					rootActive = false;
			}
		}
	}

	void OnHotspotAnalyzeComplete(Hotspot hotspot) {
		if(mHotspot != hotspot) //sanity check
			OnHotspotChanged(hotspot);
		else if(mHotspot)
			RefreshAtmosphereStats();
	}

	void OnSeasonToggle(SeasonData season) {
		if(mHotspot)
			RefreshAtmosphereStats();
	}

	void OnAnimatorTakeComplete(M8.Animator.Animate _anim, M8.Animator.Take take) {
		if(_anim.GetTakeIndex(take.name) == takeExit)
			rootActive = false;
	}

	private void RefreshAtmosphereStats() {
		if(mIsAtmosphereStatInit) {
			ClearAtmosphereStats();
		}
		else {
			mAtmosphereStatActives = new M8.CacheList<HotspotSelectInfoStatItemWidget>(atmosphereStatItemCapacity);
			mAtmosphereStatCache = new M8.CacheList<HotspotSelectInfoStatItemWidget>(atmosphereStatItemCapacity);

			for(int i = 0; i < atmosphereStatItemCapacity; i++) {
				var statItm = Instantiate(atmosphereStatItemTemplate, atmosphereStatsRoot);

				statItm.active = false;

				mAtmosphereStatCache.Add(statItm);
			}

			atmosphereStatItemTemplate.active = false;

			mIsAtmosphereStatInit = true;
		}

		var season = OverworldController.instance.currentSeason;
		var criteria = OverworldController.instance.hotspotGroup.criteria;

		int equalCount = 0;
		int emptyCount = 0;

		for(int i = 0; i < criteria.attributes.Length; i++) {
			var statItm = mAtmosphereStatCache.RemoveLast();
			
			var attrib = criteria.attributes[i];
			var atmosphere = attrib.atmosphere;

			var analyzeResult = mHotspot.GetSeasonAtmosphereAnalyze(season, atmosphere);
			if(analyzeResult == Hotspot.AnalyzeResult.None) {
				statItm.SetupEmpty(atmosphere);
				emptyCount++;
			}
			else {
				M8.RangeFloat valRange;
				if(mHotspot.GetStat(season, atmosphere, out valRange)) {
					statItm.Setup(atmosphere, valRange.Lerp(0.5f), analyzeResult);

					if(analyzeResult == Hotspot.AnalyzeResult.Equal)
						equalCount++;
				}
				else {
					statItm.SetupEmpty(atmosphere);
					emptyCount++;
				}
			}

			statItm.transform.SetAsLastSibling();
			statItm.active = true;

			mAtmosphereStatActives.Add(statItm);
		}

		if(analysisWaitGO) analysisWaitGO.SetActive(emptyCount > 0);
		if(analysisIncompatibleGO) analysisIncompatibleGO.SetActive(emptyCount == 0 && equalCount < criteria.attributes.Length);
		if(analysisInvestigateGO) analysisInvestigateGO.SetActive(emptyCount == 0 && equalCount == criteria.attributes.Length);
	}

    private void ClearAtmosphereStats() {
        for(int i = 0; i < mAtmosphereStatActives.Count; i++) {
            var statItm = mAtmosphereStatActives[i];
            statItm.active = false;
			mAtmosphereStatCache.Add(statItm);
		}

        mAtmosphereStatActives.Clear();
	}
}
