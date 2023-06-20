using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandscapePreview : MonoBehaviour {
    [Header("View")]
    public Transform root;
    //move info
    
    public bool active { 
        get { return gameObject.activeSelf; }
        set { gameObject.SetActive(value); }
    }

    public LandscapePreviewTelemetry landscapePreviewTelemetry { get; private set; }

    public int curRegionIndex {
        get { return mCurRegionInd; }
        set {
            if(mCurRegionInd != value) {
                mCurRegionInd = value;

                //move towards region view
            }
        }
    }

    private Dictionary<HotspotData, LandscapePreviewTelemetry> mHotspotPreviews = new Dictionary<HotspotData, LandscapePreviewTelemetry>();

    private int mCurRegionInd;

    public void AddHotspotPreview(HotspotData hotspotData) {
        if(mHotspotPreviews.ContainsKey(hotspotData)) //already added
            return;

        if(!hotspotData.landscapePrefab) {
            Debug.LogWarning("No landscape prefab found for: " + hotspotData.name);
            return;
        }

        var newLandscape = Instantiate(hotspotData.landscapePrefab);

        var landscapeTrans = newLandscape.transform;
        landscapeTrans.SetParent(root, false);

        newLandscape.gameObject.SetActive(false);

        mHotspotPreviews.Add(hotspotData, newLandscape);
    }

    public void SetCurrentPreview(HotspotData hotspotData) {
        if(landscapePreviewTelemetry) {
            landscapePreviewTelemetry.gameObject.SetActive(false);
        }

        LandscapePreviewTelemetry newLandscapePreviewTelemetry;
        mHotspotPreviews.TryGetValue(hotspotData, out newLandscapePreviewTelemetry);

        if(newLandscapePreviewTelemetry) {
            landscapePreviewTelemetry = newLandscapePreviewTelemetry;

            var landscapeTrans = landscapePreviewTelemetry.transform;

            landscapeTrans.localPosition = Vector3.zero;
            landscapeTrans.localRotation = Quaternion.identity;
            landscapeTrans.localScale = Vector3.one;

            landscapePreviewTelemetry.gameObject.SetActive(true);

            //reset view to first region
            mCurRegionInd = 0;
        }
    }
}
