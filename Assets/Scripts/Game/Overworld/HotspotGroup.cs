using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HotspotGroup : MonoBehaviour {
    public struct HotspotItemInfo {
        public Transform root { get; private set; }

        public Hotspot hotspot { get; private set; }

        public HotspotItemInfo(Transform t) {
            root = t;
            hotspot = t.GetComponent<Hotspot>();
        }
    }

    [Header("Data")]
    public CriteriaData criteria;

    public bool active {
        get { return gameObject.activeSelf; }
        set { gameObject.SetActive(value); }
    }

    public HotspotItemInfo[] hotspots { 
        get {
            if(!mIsInit) Init();

            return mHotspots; 
        }
    }

    private HotspotItemInfo[] mHotspots;
    private bool mIsInit;

    public int GetHotspotIndex(Hotspot hotspot) {
        for(int i = 0; i < mHotspots.Length; i++) {
            var inf = mHotspots[i];
            if(inf.hotspot == hotspot)
                return i;
        }

        return -1;
    }

    void Awake() {
        if(!mIsInit) Init();
    }

    private void Init() {
        var t = transform;

        //assume each child contains Hotspot
        mHotspots = new HotspotItemInfo[t.childCount];

        for(int i = 0; i < t.childCount; i++) {
            var hotspotTrans = t.GetChild(i);

            mHotspots[i] = new HotspotItemInfo(hotspotTrans);
        }

        mIsInit = true;
    }
}
