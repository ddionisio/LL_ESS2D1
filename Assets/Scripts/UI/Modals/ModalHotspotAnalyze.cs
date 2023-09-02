using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class ModalHotspotAnalyze : M8.ModalController, M8.IModalActive, M8.IModalPush, M8.IModalPop {
    public const string parmHotspot = "hotspot"; //Hotspot
    public const string parmSeason = "overworldSeason"; //SeasonData
    public const string parmCriteria = "overworldCriteria"; //CriteriaData

    [Header("Hotspot Display")]
    public Image hotspotIconImage;
    public bool hotspotIconUseNativeSize;

    public TMP_Text hotspotRegionNameLabel;
    public TMP_Text hotspotClimateNameLabel;

    [Header("Season Display")]
    public Image seasonIconImage;
    public bool seasonIconUseNativeSize;
    public TMP_Text seasonNameLabel;

    [Header("Analyze Display")]
    public Transform analyzeRoot;
    public HotspotAnalyzeItemWidget analyzeItemTemplate; //not prefab
    public int analyzeItemCapacity = 3;

    [Header("Bottom Display")]
    public GameObject analyzingRootGO;
    public GameObject mismatchRootGO;
    public GameObject investigateRootGO;

    [Header("Animation")]
    public M8.Animator.Animate animator;
    [M8.Animator.TakeSelector]
    public string takeMatch;
    [M8.Animator.TakeSelector]
    public string takeMismatch;

    [Header("SFX")]
    [M8.SoundPlaylist]
    public string sfxMatch;
    [M8.SoundPlaylist]
    public string sfxMismatch;

    [Header("Signal Invoke")]
    public SignalHotspot signalInvokeHotspotInvestigate;

    public bool isAnalyzing { get { return mAnalyzeRout != null; } }

    private bool mIsInit;

    private M8.CacheList<HotspotAnalyzeItemWidget> mAnalyzeActives;
    private M8.CacheList<HotspotAnalyzeItemWidget> mAnalyzeCache;

    private bool mIsAnalyzeComplete;
    
    private Hotspot mHotspot;
    private SeasonData mSeasonData;

    private Coroutine mAnalyzeRout;

    private int mTakeMatchInd = -1;
    private int mTakeMismatchInd = -1;

    public void HotspotInvestigate() {
        signalInvokeHotspotInvestigate?.Invoke(mHotspot);
    }

    void M8.IModalActive.SetActive(bool aActive) {
        if(aActive) {
            if(mIsAnalyzeComplete) {
                //setup immediate result
                int matchCount = 0;
                for(int i = 0; i < mAnalyzeActives.Count; i++) {
                    var itm = mAnalyzeActives[i];

                    itm.AnalyzeImmediate();
                    if(itm.compareResult == 0)
                        matchCount++;

                    itm.active = true;
                }

                if(analyzingRootGO) analyzingRootGO.SetActive(false);
                if(mismatchRootGO) mismatchRootGO.SetActive(matchCount < mAnalyzeActives.Count);
                if(investigateRootGO) investigateRootGO.SetActive(matchCount == mAnalyzeActives.Count);
            }
            else {
                if(mAnalyzeRout == null)
                    mAnalyzeRout = StartCoroutine(DoAnalyze());
            }
        }
    }

    void M8.IModalPop.Pop() {
        ClearAnalyzeItems();

        if(mAnalyzeRout != null) {
            StopCoroutine(mAnalyzeRout);
            mAnalyzeRout = null;
        }

        mHotspot = null;
        mSeasonData = null;
    }

    void M8.IModalPush.Push(M8.GenericParams parms) {
        if(!mIsInit) {
            //setup analyze items
            mAnalyzeActives = new M8.CacheList<HotspotAnalyzeItemWidget>(analyzeItemCapacity);
            mAnalyzeCache = new M8.CacheList<HotspotAnalyzeItemWidget>(analyzeItemCapacity);

            for(int i = 0; i < analyzeItemCapacity; i++) {
                var newItm = Instantiate(analyzeItemTemplate, analyzeRoot);
                newItm.active = false;
                mAnalyzeCache.Add(newItm);
            }

            analyzeItemTemplate.active = false;

            //setup animation
            if(animator) {
                mTakeMatchInd = animator.GetTakeIndex(takeMatch);
                mTakeMismatchInd = animator.GetTakeIndex(takeMismatch);
            }

            mIsInit = true;
        }

        mHotspot = null;
        mSeasonData = null;

        CriteriaData criteriaData = null;

        if(parms != null) {
            if(parms.ContainsKey(parmHotspot))
                mHotspot = parms.GetValue<Hotspot>(parmHotspot);
            if(parms.ContainsKey(parmSeason))
                mSeasonData = parms.GetValue<SeasonData>(parmSeason);
            if(parms.ContainsKey(parmCriteria))
                criteriaData = parms.GetValue<CriteriaData>(parmCriteria);
        }

        if(analyzingRootGO) analyzingRootGO.SetActive(false);
        if(mismatchRootGO) mismatchRootGO.SetActive(false);
        if(investigateRootGO) investigateRootGO.SetActive(false);

        //setup info display
        if(mHotspot) {
            var hotspotData = mHotspot.data;

            if(hotspotIconImage) {
                hotspotIconImage.sprite = hotspotData.climate.icon;
                if(hotspotIconUseNativeSize)
                    hotspotIconImage.SetNativeSize();
            }

            if(hotspotRegionNameLabel) hotspotRegionNameLabel.text = M8.Localize.Get(hotspotData.nameRef);

            if(hotspotClimateNameLabel) hotspotClimateNameLabel.text = M8.Localize.Get(hotspotData.climate.nameRef);
        }

        if(mSeasonData) {
            if(seasonIconImage) {
                seasonIconImage.sprite = mSeasonData.icon;
                if(seasonIconUseNativeSize)
                    seasonIconImage.SetNativeSize();
            }

            if(seasonNameLabel)
                seasonNameLabel.text = M8.Localize.Get(mSeasonData.nameRef);
        }

        //setup analyze display
        mIsAnalyzeComplete = false;

        if(mHotspot && mSeasonData) {
            mIsAnalyzeComplete = mHotspot.IsSeasonAnalyzed(mSeasonData);

            var hotspotData = mHotspot.data;

            var regionStats = hotspotData.GetAtmosphereStats(mSeasonData);
            var criteriaStats = criteriaData.GenerateAtmosphereStats();

            for(int i = 0; i < criteriaStats.Length; i++) {
                var criteriaStat = criteriaStats[i];

                //grab corresponding region stat
                var regionStatInd = -1;
                for(int j = 0; j < regionStats.Length; j++) {
                    if(regionStats[j].atmosphere == criteriaStat.atmosphere) {
                        regionStatInd = j;
                        break;
                    }
                }

                if(regionStatInd == -1)
                    continue;

                var analyzeItm = mAnalyzeCache.RemoveLast();
                if(!analyzeItm) //fail-safe
                    break;

                analyzeItm.transform.SetAsLastSibling();

                analyzeItm.Setup(criteriaStat.atmosphere, regionStats[regionStatInd].range, criteriaStat.range);

                mAnalyzeActives.Add(analyzeItm);
            }
        }
    }

    IEnumerator DoAnalyze() {
        if(analyzingRootGO) analyzingRootGO.SetActive(true);

        var matchCount = 0;

        for(int i = 0; i < mAnalyzeActives.Count; i++) {
            var itm = mAnalyzeActives[i];

            itm.active = true;
            itm.AnalyzeStart();

            while(itm.isBusy)
                yield return null;

            if(itm.compareResult == 0) {
                if(!string.IsNullOrEmpty(sfxMatch))
                    M8.SoundPlaylist.instance.Play(sfxMatch, false);

                if(mTakeMatchInd != -1)
                    yield return animator.PlayWait(mTakeMatchInd);

                matchCount++;
            }
            else {
                if(!string.IsNullOrEmpty(sfxMismatch))
                    M8.SoundPlaylist.instance.Play(sfxMismatch, false);

                if(mTakeMismatchInd != -1)
                    yield return animator.PlayWait(mTakeMismatchInd);
            }
        }

        if(analyzingRootGO) analyzingRootGO.SetActive(false);

        if(matchCount == mAnalyzeActives.Count) {
            if(investigateRootGO) investigateRootGO.SetActive(true);
        }
        else {
            if(mismatchRootGO) mismatchRootGO.SetActive(true);
        }

        mAnalyzeRout = null;

        mIsAnalyzeComplete = true;

        mHotspot.SetSeasonAnalyzed(mSeasonData, true);
    }

    private void ClearAnalyzeItems() {
        for(int i = 0; i < mAnalyzeActives.Count; i++) {
            var itm = mAnalyzeActives[i];
            itm.active = false;
            mAnalyzeCache.Add(itm);
        }

        mAnalyzeActives.Clear();
    }
}
