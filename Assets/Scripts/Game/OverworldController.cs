using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoLExt;

public class OverworldController : GameModeController<OverworldController> {

    [Header("Overworld")]
    public OverworldView overworldView;
    public OverworldBounds overworldBounds;

    [Header("Hotspots")]
    public Transform hotspotRoot;
    public float hotspotZoom;

    [Header("Investigate")]
    public LandscapePreview landscapePreview;

    [Header("Signal Listen")]
    public SignalHotspot signalListenHotspotClick;

    [Header("Debug")]
    public bool debugOverrideHotspotGroup;
    public string debugHotspotGroup;
    public int debugHotspotIndex; //if group is empty

    public HotspotGroup hotspotGroupCurrent { get; private set; }

    public bool isBusy { get { return mRout != null; } }

    private HotspotGroup[] mHotspotGroups;

    private Coroutine mRout;

    public int GetHotspotGroup(string groupName) {
        for(int i = 0; i < mHotspotGroups.Length; i++) {
            if(mHotspotGroups[i].name == groupName)
                return i;
        }

        return -1;
    }

    public void SetHotspotGroupCurrent(int groupIndex) {
        if(hotspotGroupCurrent != null) {
            hotspotGroupCurrent.active = false;
            hotspotGroupCurrent = null;
        }

        if(groupIndex >= 0 && groupIndex < mHotspotGroups.Length) {
            hotspotGroupCurrent = mHotspotGroups[groupIndex];
            hotspotGroupCurrent.active = true;
        }
    }

    protected override void OnInstanceDeinit() {
        if(signalListenHotspotClick) signalListenHotspotClick.callback -= OnHotspotClick;

        if(mRout != null) {
            StopCoroutine(mRout);
            mRout = null;
        }

        base.OnInstanceDeinit();
    }

    protected override void OnInstanceInit() {
        base.OnInstanceInit();

        if(hotspotRoot) {
            mHotspotGroups = new HotspotGroup[hotspotRoot.childCount];

            for(int i = 0; i < hotspotRoot.childCount; i++) {
                var t = hotspotRoot.GetChild(i);
                var grp = t.GetComponent<HotspotGroup>();
                if(grp)
                    grp.active = false; //hide initially

                mHotspotGroups[i] = grp;
            }
        }
        else
            mHotspotGroups = new HotspotGroup[0];

        if(landscapePreview)
            landscapePreview.active = false;

        //setup signals

        if(signalListenHotspotClick) signalListenHotspotClick.callback -= OnHotspotClick;
    }

    protected override IEnumerator Start() {
        yield return base.Start();

        //show overworld

        //some intros

        //show hotspots
        int hotspotIndex = -1;

        if(!debugOverrideHotspotGroup) {

        }
        else {
            if(!string.IsNullOrEmpty(debugHotspotGroup))
                hotspotIndex = GetHotspotGroup(debugHotspotGroup);
            else
                hotspotIndex = debugHotspotIndex;
        }

        SetHotspotGroupCurrent(hotspotIndex);
    }

    IEnumerator DoInvestigateEnter(Hotspot hotspot) {
        //pop overworld modal
                
        //hide hotspot
        hotspot.Hide();

        hotspotGroupCurrent.active = false;

        //wait for hotspot to hide completely

        //zoom-in
        overworldView.ZoomIn(hotspot.position, hotspotZoom);

        //wait for zoom-in
        while(overworldView.isBusy)
            yield return null;
                
        //push investigate modal

        mRout = null;
    }

    IEnumerator DoInvestigateExit(Hotspot hotspot) {
        yield return null;

        overworldView.ZoomOut();

        hotspotGroupCurrent.active = true;

        mRout = null;
    }

    void OnHotspotClick(Hotspot hotspot) {
        if(isBusy)
            return;

        mRout = StartCoroutine(DoInvestigateEnter(hotspot));
    }
}
