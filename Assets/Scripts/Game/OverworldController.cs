using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoLExt;

public class OverworldController : GameModeController<OverworldController> {
    [Header("Setup")]
    public AtmosphereAttributeBase atmosphereDefault;
    public SeasonData seasonDefault;
    public AtmosphereAttributeBase[] atmosphereActiveOverlays;

    [Header("Overworld")]
    public OverworldView overworldView;
    public OverworldBounds overworldBounds;

    [Header("Hotspots")]
    public Transform hotspotRoot;
    public float hotspotZoom;

    [Header("Investigate")]
    public LandscapePreview landscapePreview;

    [Header("Signal Listen")]
    public SignalSeasonData signalListenSeasonToggle;
    public SignalHotspot signalListenHotspotClick;

    [Header("Signal Invoke")]
    public SignalSeasonData signalInvokeSeasonDefault;

    [Header("Debug")]
    public bool debugOverrideHotspotGroup;
    public string debugHotspotGroup;
    public int debugHotspotIndex; //if group is empty

    public HotspotGroup hotspotGroupCurrent { get; private set; }

    public bool isBusy { get { return mRout != null; } }

    private HotspotGroup[] mHotspotGroups;

    private SeasonData mCurSeasonData;

    private Coroutine mRout;

    private M8.GenericParams mModalOverworldParms = new M8.GenericParams();

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

            landscapePreview.DestroyHotspotPreviews();
        }

        if(groupIndex >= 0 && groupIndex < mHotspotGroups.Length) {
            hotspotGroupCurrent = mHotspotGroups[groupIndex];
            hotspotGroupCurrent.active = true;

            //prep landscape prefabs for preview
            for(int i = 0; i < hotspotGroupCurrent.hotspots.Length; i++) {
                var hotspot = hotspotGroupCurrent.hotspots[i];

                landscapePreview.AddHotspotPreview(hotspot.hotspot.data);
            }
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

        mCurSeasonData = seasonDefault;

        if(landscapePreview)
            landscapePreview.active = false;

        //setup signals

        if(signalListenHotspotClick) signalListenHotspotClick.callback += OnHotspotClick;
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

        yield return null;

        //set season
        if(signalInvokeSeasonDefault) signalInvokeSeasonDefault.Invoke(mCurSeasonData);

        //show overworld modal
        ModalShowOverworld();
    }

    IEnumerator DoInvestigateEnter(Hotspot hotspot) {
        //hide hotspot
        hotspot.Hide();

        //pop overworld modal
        M8.ModalManager.main.CloseUpTo(GameData.instance.modalOverworld, true);

        while(M8.ModalManager.main.isBusy || M8.ModalManager.main.IsInStack(GameData.instance.modalOverworld))
            yield return null;

        //wait for hotspot to hide completely
        while(hotspot.isBusy)
            yield return null;

        hotspotGroupCurrent.active = false;

        //zoom-in
        overworldView.ZoomIn(hotspot.position, hotspotZoom);

        //wait for zoom-in
        while(overworldView.isBusy)
            yield return null;

        //show preview
        landscapePreview.SetCurrentPreview(hotspot.data);
        landscapePreview.active = true;
        //anim

        //push investigate modal


        mRout = null;
    }

    IEnumerator DoInvestigateExit() {
        //pop investigate modal

        //hide investigate
        //anim
        landscapePreview.active = false;

        //zoom-out
        overworldView.ZoomOut();

        //wait for zoom-out
        while(overworldView.isBusy || M8.ModalManager.main.isBusy || M8.ModalManager.main.IsInStack(GameData.instance.modalHotspotInvestigate))
            yield return null;

        hotspotGroupCurrent.active = true;

        ModalShowOverworld();

        mRout = null;
    }

    void OnHotspotClick(Hotspot hotspot) {
        if(isBusy)
            return;

        mRout = StartCoroutine(DoInvestigateEnter(hotspot));
    }

    private void ModalShowOverworld() {
        mModalOverworldParms[ModalOverworld.parmAtmosphereActives] = atmosphereActiveOverlays;
        mModalOverworldParms[ModalOverworld.parmAtmosphere] = atmosphereDefault;
        mModalOverworldParms[ModalOverworld.parmSeason] = mCurSeasonData;
        mModalOverworldParms[ModalOverworld.parmCriteria] = hotspotGroupCurrent ? hotspotGroupCurrent.criteria : null;

        M8.ModalManager.main.Open(GameData.instance.modalOverworld, mModalOverworldParms);
    }
}
