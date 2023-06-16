using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoLExt;

public class OverworldController : GameModeController<OverworldController> {
    public struct HotspotItemInfo {
        public Transform root { get; private set; }

        public Hotspot hotspot { get; private set; }

        public HotspotItemInfo(Transform t) {
            root = t;
            hotspot = t.GetComponent<Hotspot>();
        }
    }

    public class HotspotGroupInfo {
        public string name { get { return rootGO ? rootGO.name : ""; } }

        public bool active {
            get { return rootGO ? rootGO.activeSelf : false; }
            set {
                if(rootGO)
                    rootGO.SetActive(value);
            }
        }

        public GameObject rootGO { get; private set; }

        public HotspotItemInfo[] hotspots { get; private set; }

        public HotspotGroupInfo(Transform t) {
            rootGO = t.gameObject;

            //assume each child contains Hotspot
            hotspots = new HotspotItemInfo[t.childCount];

            for(int i = 0; i < t.childCount; i++) {
                var hotspotTrans = t.GetChild(i);

                hotspots[i] = new HotspotItemInfo(hotspotTrans);
            }

            //initially disabled
            rootGO.SetActive(false);
        }
    }

    [Header("Overworld")]
    public OverworldView overworldView;
    public OverworldBounds overworldBounds;

    [Header("Hotspots")]
    public Transform hotspotRoot;
    public float hotspotZoom;

    public HotspotGroupInfo hotspotGroupCurrent { get; private set; }

    private HotspotGroupInfo[] mHotspotGroups;

    protected override void OnInstanceDeinit() {
        base.OnInstanceDeinit();
    }

    protected override void OnInstanceInit() {
        base.OnInstanceInit();

        if(hotspotRoot) {
            mHotspotGroups = new HotspotGroupInfo[hotspotRoot.childCount];

            for(int i = 0; i < hotspotRoot.childCount; i++) {
                var grp = new HotspotGroupInfo(hotspotRoot.GetChild(i));

                mHotspotGroups[i] = grp;

                //setup callbacks
                for(int j = 0; j < grp.hotspots.Length; j++) {
                    var hotspot = grp.hotspots[j].hotspot;

                    hotspot.clickCallback += OnHotspotClick;
                }
            }
        }
        else
            mHotspotGroups = new HotspotGroupInfo[0];
    }

    protected override IEnumerator Start() {
        yield return base.Start();

        //setup based on progress

        //show overworld

        //some intros

        //show hotspots
    }

    void OnHotspotClick(Hotspot hotspot) {

    }
}
