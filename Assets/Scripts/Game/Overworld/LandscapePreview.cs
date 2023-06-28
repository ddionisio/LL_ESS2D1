using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandscapePreview : MonoBehaviour {
    [Header("View")]
    public Transform root;

    [Header("Region Move Info")]
    public float regionMoveDelay = 0.5f;

    [Header("Signal Listen")]
    public SignalSeasonData signalListenSeasonChange;

    public bool active { 
        get { return gameObject.activeSelf; }
        set { gameObject.SetActive(value); }
    }

    public HotspotData hotspotData { get; private set; }
    public LandscapePreviewTelemetry landscapePreviewTelemetry { get; private set; }

    public int curRegionIndex {
        get { return mCurRegionInd; }
        set {
            if(mCurRegionInd != value) {
                mCurRegionInd = value;

                //move towards region view                
                mMoveEnd = -landscapePreviewTelemetry.regions[mCurRegionInd].center;

                if(mMoveRout == null) {
                    //mMoveStart = landscapePreviewTelemetry.transform.localPosition;

                    mMoveRout = StartCoroutine(DoRegionMove());
                }

                //update altitude
                altitude = landscapePreviewTelemetry.GetAltitude(mCurRegionInd);
            }
        }
    }

    public float altitude { get; private set; }

    public float altitudeScale {
        get {
            return landscapePreviewTelemetry.GetAltitudeScale(altitude);
        }
    }

    public bool isMoving { get { return mMoveRout != null; } }
        
    private Dictionary<HotspotData, LandscapePreviewTelemetry> mHotspotPreviews = new Dictionary<HotspotData, LandscapePreviewTelemetry>();

    private int mCurRegionInd;

    private Coroutine mMoveRout;
    private Vector2 mMoveEnd;

    public void DestroyHotspotPreviews() {
        foreach(var pair in mHotspotPreviews) {
            if(pair.Value)
                Destroy(pair.Value);
        }

        mHotspotPreviews.Clear();
    }

    public void AddHotspotPreview(HotspotData aHotspotData) {
        if(mHotspotPreviews.ContainsKey(aHotspotData)) //already added
            return;

        if(!aHotspotData.landscapePrefab) {
            Debug.LogWarning("No landscape prefab found for: " + aHotspotData.name);
            return;
        }

        var newLandscape = Instantiate(aHotspotData.landscapePrefab);

        var landscapeTrans = newLandscape.transform;
        landscapeTrans.SetParent(root, false);

        newLandscape.gameObject.SetActive(false);

        mHotspotPreviews.Add(aHotspotData, newLandscape);
    }

    public void SetCurrentPreview(HotspotData aHotspotData) {
        if(landscapePreviewTelemetry) {
            landscapePreviewTelemetry.gameObject.SetActive(false);
        }

        LandscapePreviewTelemetry newLandscapePreviewTelemetry;
        mHotspotPreviews.TryGetValue(aHotspotData, out newLandscapePreviewTelemetry);

        if(newLandscapePreviewTelemetry) {
            hotspotData = aHotspotData;

            landscapePreviewTelemetry = newLandscapePreviewTelemetry;

            var landscapeTrans = landscapePreviewTelemetry.transform;

            landscapeTrans.localPosition = Vector3.zero;
            landscapeTrans.localRotation = Quaternion.identity;
            landscapeTrans.localScale = Vector3.one;

            landscapePreviewTelemetry.gameObject.SetActive(true);

            //reset view to first region
            mCurRegionInd = 0;

            landscapeTrans.localPosition = -landscapePreviewTelemetry.regions[mCurRegionInd].center;

            altitude = landscapePreviewTelemetry.GetAltitude(mCurRegionInd);
        }
    }

    public void SetSeason(SeasonData seasonData) {

    }

    void OnDisable() {
        if(signalListenSeasonChange) signalListenSeasonChange.callback -= SetSeason;

        if(mMoveRout != null) {
            StopCoroutine(mMoveRout);
            mMoveRout = null;
        }
    }

    void OnEnable() {
        if(signalListenSeasonChange) signalListenSeasonChange.callback += SetSeason;
    }

    IEnumerator DoRegionMove() {
        var landscapeTrans = landscapePreviewTelemetry.transform;

        Vector2 vel = Vector2.zero;
        Vector2 pos = landscapeTrans.localPosition;

        while(pos != mMoveEnd) {
            pos = Vector2.SmoothDamp(pos, mMoveEnd, ref vel, regionMoveDelay);
            landscapeTrans.localPosition = pos;

            yield return null;
        }

        mMoveRout = null;
    }
}
