using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using LoLExt;

using TMPro;

public class Hotspot : MonoBehaviour {
    public enum PingResult {
        None,
        Near,
        Reveal
    }

    public enum AnalyzeResult {
        None,
        Less,
        Equal,
        Greater
    }

    [Header("Data")]
    public HotspotData data;
    [SerializeField]
    bool _isHidden = true;

    [Header("Display")]
    public GameObject rootGO;
    public GameObject selectGO;

    [Header("Ping Info")]
    public Transform pingRoot;
    public float revealRadius;
    public float pingRadius;

    [Header("Analysis Info")]
    public GameObject analysisRootGO;
    public GameObject analysisProgressGO;
	public GameObject analysisCompleteGO;
	public TMP_Text analysisLabel;
	public GameObject analysisIconProgressGO;
	public GameObject analysisIconMatchGO;
	public GameObject analysisIconMismatchGO;

	[Header("Animation")]
    public M8.Animator.Animate animator;
    [M8.Animator.TakeSelector]
    public int takeActive = -1;
    [M8.Animator.TakeSelector]
    public int takeReveal = -1;
    [M8.Animator.TakeSelector]
    public int takePing = -1;
	[M8.Animator.TakeSelector]
	public int takeAnalysisEnter = -1;

	[Header("Signal Invoke")]
    public SignalHotspot signalInvokeClick;
    public SignalHotspot signalInvokeAnalysisComplete;

    [Header("Signal Listen")]
    public SignalAtmosphereAttribute signalListenAtmosphereToggle;
    public SignalSeasonData signalListenSeasonToggle;

    public Vector2 position { get { return transform.position; } }

    public bool isBusy { get { return mRout != null; } } //wait for animation

    public bool isHidden { get; private set; }

    public bool isSelected { 
        get { return mIsSelected; }
        set {
            if(mIsSelected != value) {
                mIsSelected = value;
                ApplySelectDisplay();
			}
        }
    }

    private Coroutine mRout;

    private bool[] mIsSeasonAnalyzed; //corresponds to atmosphere infos in hotspot data

    private Dictionary<AtmosphereAttributeBase, AnalyzeResult[]> mSeasonAtmosphereAnalyzeResults = new Dictionary<AtmosphereAttributeBase, AnalyzeResult[]>(); //each result corresponds to season index via GameData

    private AtmosphereAttributeBase mAtmosphere = null;
    private int mSeasonIndex = -1;

    private bool mIsSelected;
    private bool mIsHover;

    public void SeasonAtmosphereAnalyze(CriteriaData criteria, SeasonData season, AtmosphereAttributeBase atmosphere) {
        var atmosAttrs = data.GetAtmosphereStats(season);
        if(atmosAttrs == null)
            return;

        int atmosInd = -1;
        for(int i = 0; i < atmosAttrs.Length; i++) {
            if(atmosAttrs[i].atmosphere == atmosphere) {
                atmosInd = i;
                break;
            }
        }

        if(atmosInd == -1)
            return;

        var val = atmosAttrs[atmosInd].median;

        int result;
        if(!criteria.AtmosphereValueCompare(atmosphere, val, out result))
            return;

        AnalyzeResult[] analyzeResults;
        if(!mSeasonAtmosphereAnalyzeResults.TryGetValue(atmosphere, out analyzeResults)) {
            analyzeResults = new AnalyzeResult[GameData.instance.seasons.Length];
            mSeasonAtmosphereAnalyzeResults[atmosphere] = analyzeResults;
		}

        var seasonInd = GameData.instance.GetSeasonIndex(season);
        if(seasonInd == -1)
            return;

        switch(result) {
            case 0:
                analyzeResults[seasonInd] = AnalyzeResult.Equal; 
                break;
            case 1:
                analyzeResults[seasonInd] = AnalyzeResult.Greater;
                break;
            case -1:
                analyzeResults[seasonInd] = AnalyzeResult.Less;
                break;
            default:
                analyzeResults[seasonInd] = AnalyzeResult.None;
				break;
        }
    }

    public AnalyzeResult GetSeasonAtmosphereAnalyze(SeasonData season, AtmosphereAttributeBase atmosphere) {
		AnalyzeResult[] analyzeResults;
        if(mSeasonAtmosphereAnalyzeResults.TryGetValue(atmosphere, out analyzeResults)) {
			var seasonInd = GameData.instance.GetSeasonIndex(season);
			if(seasonInd != -1)
                return analyzeResults[seasonInd];
		}
            
        return AnalyzeResult.None;
    }

    public bool IsSeasonAnalyzed(SeasonData season) {
        if(!data) //fail-safe
            return false;

        if(mIsSeasonAnalyzed == null) {
            mIsSeasonAnalyzed = new bool[GameData.instance.seasons.Length];
            return false; //we know it hasn't been analyzed
        }

        var seasonInd = GameData.instance.GetSeasonIndex(season);
        if(seasonInd == -1)
            return false;

        return mIsSeasonAnalyzed[seasonInd];
    }

    public void SetSeasonAnalyzed(SeasonData season, bool isAnalyzed) {
        if(!data) //fail-safe
            return;

        if(mIsSeasonAnalyzed == null)
            mIsSeasonAnalyzed = new bool[GameData.instance.seasons.Length];

        var seasonInd = GameData.instance.GetSeasonIndex(season);
        if(seasonInd != -1)
            mIsSeasonAnalyzed[seasonInd] = isAnalyzed;
    }

    public PingResult CheckPing(Vector2 pos) {
        var dpos = pos - position;

        var distSq = dpos.sqrMagnitude;

        if(distSq <= revealRadius * revealRadius)
            return PingResult.Reveal;

        if(distSq <= pingRadius * pingRadius)
            return PingResult.Near;

        return PingResult.None;
    }

    public void Ping(Vector2 pos) {
        if(pingRoot) {
            if(mRout != null)
                StopCoroutine(mRout);

            //orient ping towards pos, position next to it
            var dir = (pos - position).normalized;

            pingRoot.position = pos;
            pingRoot.up = dir;

            mRout = StartCoroutine(DoPing());
        }
    }

    public void Reveal() {
        if(!isHidden)
            return;

        isHidden = false;

        if(rootGO) rootGO.SetActive(true);

        if(mRout != null)
            StopCoroutine(mRout);

        mRout = StartCoroutine(DoReveal());
    }

    /// <summary>
    /// Call when clicked to enter investigation mode
    /// </summary>
    public void Click() {
        signalInvokeClick?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        mIsHover = true;

        if(isHidden) return;

        ApplySelectDisplay();
    }

	public void OnPointerExit(PointerEventData eventData) {
		mIsHover = false;

		if(isHidden) return;

		ApplySelectDisplay();
	}

	void OnDisable() {
		if(signalListenAtmosphereToggle) signalListenAtmosphereToggle.callback -= OnAtmosphereToggle;
		if(signalListenSeasonToggle) signalListenSeasonToggle.callback -= OnSeasonToggle;

		mIsHover = false;
	}

	void OnEnable() {
        if(signalListenAtmosphereToggle) signalListenAtmosphereToggle.callback += OnAtmosphereToggle;
        if(signalListenSeasonToggle) signalListenSeasonToggle.callback += OnSeasonToggle;

		if(OverworldController.isInstantiated) {
            mAtmosphere = OverworldController.instance.currentAtmosphere;
            mSeasonIndex = GameData.instance.GetSeasonIndex(OverworldController.instance.currentSeason);
		}

		if(pingRoot) pingRoot.gameObject.SetActive(false);
		if(analysisRootGO) analysisRootGO.SetActive(false);
        if(selectGO) selectGO.SetActive(false);

		if(isHidden) {
            if(rootGO) rootGO.SetActive(false);

            mIsSelected = false;
            mIsHover = false;
		}
        else {
            if(rootGO) rootGO.SetActive(true);

            mRout = StartCoroutine(DoShow());
        }
	}

    void Awake() {
        isHidden = _isHidden;
    }

    void OnAtmosphereToggle(AtmosphereAttributeBase atmosphere) {

    }

    void OnSeasonToggle(SeasonData season) {

    }

    IEnumerator DoPing() {
        pingRoot.gameObject.SetActive(true);

        if(takePing != -1)
            yield return animator.PlayWait(takePing);

        pingRoot.gameObject.SetActive(false);

        mRout = null;
    }

    IEnumerator DoReveal() {
        if(takeReveal != -1)
            yield return animator.PlayWait(takeReveal);

        if(takeActive != -1)
            animator.Play(takeActive);

        mRout = null;
    }

    IEnumerator DoShow() {
		if(takeActive != -1)
			yield return animator.PlayWait(takeActive);

		ApplySelectDisplay();

		if(IsAnalysisShow()) {
            if(analysisRootGO) analysisRootGO.SetActive(true);
            ApplyAnalysisDisplay();

            if(takeAnalysisEnter != -1)
                animator.Play(takeAnalysisEnter);
        }

        mRout = null;
	}

    private bool IsAnalysisShow() {
        if(!mAtmosphere || mSeasonIndex == -1)
            return false;

        AnalyzeResult[] results;
        if(!mSeasonAtmosphereAnalyzeResults.TryGetValue(mAtmosphere, out results))
            return false;

        return results[mSeasonIndex] != AnalyzeResult.None;
	}

    private void ApplyAnalysisDisplay() {
		//analysisLabel
	}

    private void ApplySelectDisplay() {
        if(selectGO) selectGO.SetActive(mIsSelected || mIsHover);
    }

	void OnDrawGizmos() {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(position, revealRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(position, pingRadius);
    }
}
