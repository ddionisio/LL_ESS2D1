using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoLExt;

public class Hotspot : MonoBehaviour {
    public enum PingResult {
        None,
        Near,
        Reveal
    }

    [Header("Data")]
    public HotspotData data;
    [SerializeField]
    bool _isHidden = true;

    [Header("Display")]
    public GameObject rootGO;

    [Header("Ping Info")]
    public Transform pingRoot;
    public float revealRadius;
    public float pingRadius;

    [Header("Animation")]
    public M8.Animator.Animate animator;
    [M8.Animator.TakeSelector]
    public string takeActive;
    [M8.Animator.TakeSelector]
    public string takeReveal;
    [M8.Animator.TakeSelector]
    public string takePing;

    [Header("Signal Invoke")]
    public SignalHotspot signalInvokeClick;

    public Vector2 position { get { return transform.position; } }

    public bool isBusy { get { return mRout != null; } } //wait for animation

    public bool isHidden { get; private set; }

    private Coroutine mRout;

    private int mTakeActiveInd = -1;
    private int mTakeRevealInd = -1;
    private int mTakePingInd = -1;

    private bool[] mIsSeasonAnalyzed; //corresponds to atmosphere infos in hotspot data

    public bool IsSeasonAnalyzed(SeasonData season) {
        if(!data) //fail-safe
            return false;

        if(mIsSeasonAnalyzed == null) {
            mIsSeasonAnalyzed = new bool[data.atmosphereInfos.Length];
            return false; //we know it hasn't been analyzed
        }

        var seasonInd = data.GetAtmosphereInfoIndex(season);
        if(seasonInd == -1)
            return false;

        return mIsSeasonAnalyzed[seasonInd];
    }

    public void SetSeasonAnalyzed(SeasonData season, bool isAnalyzed) {
        if(!data) //fail-safe
            return;

        if(mIsSeasonAnalyzed == null)
            mIsSeasonAnalyzed = new bool[data.atmosphereInfos.Length];

        var seasonInd = data.GetAtmosphereInfoIndex(season);
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

    void OnEnable() {
        if(pingRoot) pingRoot.gameObject.SetActive(false);

        if(isHidden) {
            if(rootGO) rootGO.SetActive(false);
        }
        else {
            if(rootGO) rootGO.SetActive(true);

            if(mTakeActiveInd != -1)
                animator.Play(mTakeActiveInd);
        }
    }

    void Awake() {
        if(animator) {
            mTakeActiveInd = animator.GetTakeIndex(takeActive);
            mTakeRevealInd = animator.GetTakeIndex(takeReveal);
            mTakePingInd = animator.GetTakeIndex(takePing);
        }

        isHidden = _isHidden;
    }

    IEnumerator DoPing() {
        pingRoot.gameObject.SetActive(true);

        if(mTakePingInd != -1)
            yield return animator.PlayWait(mTakePingInd);

        pingRoot.gameObject.SetActive(false);

        mRout = null;
    }

    IEnumerator DoReveal() {
        if(mTakeRevealInd != -1)
            yield return animator.PlayWait(mTakeRevealInd);

        if(mTakeActiveInd != -1)
            animator.Play(takeActive);

        mRout = null;
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(position, revealRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(position, pingRadius);
    }
}
