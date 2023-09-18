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
	public GameObject analysisCompleteGO;
	public TMP_Text analysisLabel;
    public Color analysisColorMatch = Color.white;
	public Color analysisColorMismatch = Color.white;
	public GameObject analysisIconMatchGO;
	public GameObject analysisIconLessGO;
	public GameObject analysisIconGreaterGO;

	[Header("Analyze")]
	public GameObject analyzeGO;
	public GameObject analyzeProgressGO;
    public float analyzeDelay = 0.5f;

	[Header("Analyze Animation")]
	public M8.Animator.Animate analyzeAnimator;
	[M8.Animator.TakeSelector(animatorField = "analyzeAnimator")]
	public int analyzeTakeMatch = -1;
	[M8.Animator.TakeSelector(animatorField = "analyzeAnimator")]
	public int analyzeTakeMismatch = -1;

    [Header("Analyze SFX")]
    [M8.SoundPlaylist]
    public string analyzeSFXMatch;
	[M8.SoundPlaylist]
	public string analyzeSFXMismatch;

	[Header("Animation")]
    public M8.Animator.Animate animator;
    [M8.Animator.TakeSelector]
    public int takeActive = -1;
    [M8.Animator.TakeSelector]
    public int takeReveal = -1;
    [M8.Animator.TakeSelector]
    public int takePing = -1;

	[Header("Signal Invoke")]
    public SignalHotspot signalInvokeClick;
    public SignalHotspot signalInvokeAnalysisComplete;

    [Header("Signal Listen")]
    public SignalAtmosphereAttribute signalListenAtmosphereToggle;
    public SignalSeasonData signalListenSeasonToggle;

    public Vector2 position { get { return transform.position; } }

    public bool isBusy { get { return mRout != null || mAnalyzeRout != null; } } //wait for animation

    public bool isHidden { get; private set; }

    public bool isSelected { 
        get { return mIsSelected; }
        set {
            if(mIsSelected != value) {
                mIsSelected = value;

				ClearAnalyzeProgress();

				ApplySelectDisplay();
                ApplyAnalysisDisplay();
			}
        }
    }

    private Coroutine mRout;
    private Coroutine mAnalyzeRout;

    private bool[] mIsSeasonAnalyzed; //corresponds to atmosphere infos in hotspot data

    private Dictionary<AtmosphereAttributeBase, AnalyzeResult[]> mSeasonAtmosphereAnalyzeResults = new Dictionary<AtmosphereAttributeBase, AnalyzeResult[]>(); //each result corresponds to season index via GameData

    private AtmosphereAttributeBase mAtmosphere = null;
    private int mSeasonIndex = -1;

    private bool mIsSelected;
    private bool mIsHover;

    public bool GetStat(SeasonData season, AtmosphereAttributeBase atmosphere, out M8.RangeFloat outputStat) {
		var stats = data.GetAtmosphereStats(season);

        for(int i = 0; i < stats.Length; i++) {
            if(stats[i].atmosphere == atmosphere) {
                outputStat = stats[i].range;
                return true;
            }
        }

        outputStat = new M8.RangeFloat();
        return false;
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

    public bool IsFullyAnalyzed(CriteriaData criteria, SeasonData season) {
        var seasonInd = GameData.instance.GetSeasonIndex(season);
        if(seasonInd == -1)
            return false;

        var analyzedCount = 0;

        for(int i = 0; i < criteria.attributes.Length; i++) {
            var attr = criteria.attributes[i];

            AnalyzeResult[] analyzes;
            if(mSeasonAtmosphereAnalyzeResults.TryGetValue(attr.atmosphere, out analyzes)) {
                if(analyzes[seasonInd] != AnalyzeResult.None)
                    analyzedCount++;
            }
        }

        return analyzedCount == criteria.attributes.Length;
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

    public void AnalyzeClick() {
        if(mAnalyzeRout != null)
            StopCoroutine(mAnalyzeRout);

        mAnalyzeRout = StartCoroutine(DoAnalyzeProgress());
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

        if(mRout != null) {
            StopCoroutine(mRout);
            mRout = null;
        }

        ClearAnalyzeProgress();
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
        if(mAtmosphere != atmosphere) {
            mAtmosphere = atmosphere;

			ClearAnalyzeProgress();

			ApplyAnalysisDisplay();
		}
    }

    void OnSeasonToggle(SeasonData season) {
        var seasonInd = GameData.instance.GetSeasonIndex(season);
        if(mSeasonIndex != seasonInd) {
            mSeasonIndex = seasonInd;

            ClearAnalyzeProgress();

			ApplyAnalysisDisplay();
        }
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
        		
		ApplyAnalysisDisplay();

		mRout = null;
	}

    IEnumerator DoAnalyzeProgress() {
        //animation
		if(analyzeGO) analyzeGO.SetActive(false);
		if(analysisCompleteGO) analysisCompleteGO.SetActive(false);

		if(analyzeProgressGO) analyzeProgressGO.SetActive(true);

		yield return new WaitForSeconds(analyzeDelay);

        mAnalyzeRout = null;

        var criteria = OverworldController.instance.hotspotGroup.criteria;

        //apply analysis
        var analyzeResult = SeasonAtmosphereAnalyze(criteria, GameData.instance.seasons[mSeasonIndex], mAtmosphere);
        if(analyzeResult != AnalyzeResult.None) {
            if(analyzeResult == AnalyzeResult.Equal) {
                if(analyzeTakeMatch != -1)
                    analyzeAnimator.Play(analyzeTakeMatch);

                if(!string.IsNullOrEmpty(analyzeSFXMatch))
                    M8.SoundPlaylist.instance.Play(analyzeSFXMatch, false);
            }
            else {
				if(analyzeTakeMismatch != -1)
					analyzeAnimator.Play(analyzeTakeMismatch);

				if(!string.IsNullOrEmpty(analyzeSFXMismatch))
					M8.SoundPlaylist.instance.Play(analyzeSFXMismatch, false);
			}
        }

        //update display
        ApplyAnalysisDisplay();

        signalInvokeAnalysisComplete?.Invoke(this);
	}

	private AnalyzeResult SeasonAtmosphereAnalyze(CriteriaData criteria, SeasonData season, AtmosphereAttributeBase atmosphere) {
		var atmosAttrs = data.GetAtmosphereStats(season);
		if(atmosAttrs == null)
			return AnalyzeResult.None;

		int atmosInd = -1;
		for(int i = 0; i < atmosAttrs.Length; i++) {
			if(atmosAttrs[i].atmosphere == atmosphere) {
				atmosInd = i;
				break;
			}
		}

		if(atmosInd == -1)
			return AnalyzeResult.None;

		var val = atmosAttrs[atmosInd].median;

		int result;
		if(!criteria.AtmosphereValueCompare(atmosphere, val, out result))
			return AnalyzeResult.None;

		AnalyzeResult[] analyzeResults;
		if(!mSeasonAtmosphereAnalyzeResults.TryGetValue(atmosphere, out analyzeResults)) {
			analyzeResults = new AnalyzeResult[GameData.instance.seasons.Length];
			mSeasonAtmosphereAnalyzeResults[atmosphere] = analyzeResults;
		}

		var seasonInd = GameData.instance.GetSeasonIndex(season);
		if(seasonInd == -1)
			return AnalyzeResult.None;

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

        return analyzeResults[seasonInd];
	}

	private void ApplyAnalysisDisplay() {
        var isVisible = !isHidden && mAtmosphere && mAtmosphere != GameData.instance.atmosphereNone && mSeasonIndex != -1 && mIsSelected;

		if(analysisRootGO)
			analysisRootGO.SetActive(isVisible);

		if(isVisible) {
            var season = GameData.instance.seasons[mSeasonIndex];
            			
            var analyzeResult = GetSeasonAtmosphereAnalyze(season, mAtmosphere);
            if(analyzeResult != AnalyzeResult.None) {
                if(analyzeGO) analyzeGO.SetActive(false);
                if(analysisCompleteGO) analysisCompleteGO.SetActive(true);

				var stats = data.GetAtmosphereStats(season);

				int statInd = -1;
				for(int i = 0; i < stats.Length; i++) {
					if(stats[i].atmosphere == mAtmosphere) {
						statInd = i;
						break;
					}
				}

				//apply value label
				if(statInd != -1) {
                    if(analysisLabel) {
                        analysisLabel.text = stats[statInd].GetValueString();
                        analysisLabel.color = analyzeResult == AnalyzeResult.Equal ? analysisColorMatch : analysisColorMismatch;
					}
                }

				//apply icon display
				if(analysisIconMatchGO) analysisIconMatchGO.SetActive(analyzeResult == AnalyzeResult.Equal);
				if(analysisIconLessGO) analysisIconLessGO.SetActive(analyzeResult == AnalyzeResult.Less);
				if(analysisIconGreaterGO) analysisIconGreaterGO.SetActive(analyzeResult == AnalyzeResult.Greater);
			}
            else {
				if(analyzeGO) analyzeGO.SetActive(true);
				if(analysisCompleteGO) analysisCompleteGO.SetActive(false);
			}
		}

		if(analyzeProgressGO) analyzeProgressGO.SetActive(false);
	}

    private void ApplySelectDisplay() {
        if(selectGO) selectGO.SetActive(mIsSelected || mIsHover);
    }

    private void ClearAnalyzeProgress() {
        if(mAnalyzeRout != null) {
            StopCoroutine(mAnalyzeRout);
            mAnalyzeRout = null;
		}

		if(analyzeProgressGO) analyzeProgressGO.SetActive(false);
	}

	void OnDrawGizmos() {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(position, revealRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(position, pingRadius);
    }
}
