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
    public CriteriaGroup criteriaGroup;

    [Header("Signal Listen")]
    public SignalSeasonData signalListenSeasonToggle;
    public SignalHotspot signalListenHotspotClick;
    public M8.Signal signalListenHotspotInvestigateBack;
    public M8.SignalInteger signalListenHotspotInvestigateLaunch;

    [Header("Signal Invoke")]
    public SignalAtmosphereAttribute signalInvokeAtmosphereOverlayDefault;
    public SignalSeasonData signalInvokeSeasonDefault;

    [Header("Debug")]
    public bool debugOverrideHotspotGroup;
    public string debugHotspotGroup;
    public int debugHotspotIndex; //if group is empty

    public HotspotGroup hotspotGroupCurrent { get; private set; }
    public Hotspot hotspotCurrent { get; private set; }

    public bool isBusy { get { return mRout != null; } }

    private HotspotGroup[] mHotspotGroups;

    private SeasonData mCurSeasonData;

    private Coroutine mRout;

    private M8.GenericParams mModalOverworldParms = new M8.GenericParams();
    private M8.GenericParams mModalHotspotInvestigateParms = new M8.GenericParams();

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

        hotspotCurrent = null;
    }

    protected override void OnInstanceDeinit() {
        if(signalListenSeasonToggle) signalListenSeasonToggle.callback -= OnSeasonToggle;
        if(signalListenHotspotClick) signalListenHotspotClick.callback -= OnHotspotClick;
        if(signalListenHotspotInvestigateBack) signalListenHotspotInvestigateBack.callback -= OnHotspotInvestigateBack;
        if(signalListenHotspotInvestigateLaunch) signalListenHotspotInvestigateLaunch.callback -= OnHotspotInvestigateLaunch;

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

        if(criteriaGroup)
            criteriaGroup.active = false;

        //setup signals
        if(signalListenSeasonToggle) signalListenSeasonToggle.callback += OnSeasonToggle;
        if(signalListenHotspotClick) signalListenHotspotClick.callback += OnHotspotClick;
        if(signalListenHotspotInvestigateBack) signalListenHotspotInvestigateBack.callback += OnHotspotInvestigateBack;
        if(signalListenHotspotInvestigateLaunch) signalListenHotspotInvestigateLaunch.callback += OnHotspotInvestigateLaunch;
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
        //turn off overlays
        if(signalInvokeAtmosphereOverlayDefault) signalInvokeAtmosphereOverlayDefault.Invoke(atmosphereDefault);

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
        landscapePreview.SetSeason(mCurSeasonData);
        landscapePreview.active = true;
        //anim

        //show critic group
        criteriaGroup.Setup(hotspotGroupCurrent.criteria);
        criteriaGroup.active = true;
        //anim

        //push investigate modal
        mModalHotspotInvestigateParms[ModalHotspotInvestigate.parmSeason] = mCurSeasonData;
        mModalHotspotInvestigateParms[ModalHotspotInvestigate.parmCriteriaGroup] = criteriaGroup;
        mModalHotspotInvestigateParms[ModalHotspotInvestigate.parmLandscape] = landscapePreview;

        M8.ModalManager.main.Open(GameData.instance.modalHotspotInvestigate, mModalHotspotInvestigateParms);

        mRout = null;
    }

    IEnumerator DoInvestigateExit() {
        //pop investigate modal
        M8.ModalManager.main.CloseUpTo(GameData.instance.modalHotspotInvestigate, true);

        //hide investigate
        //anim
        landscapePreview.active = false;

        //hide critic group
        //anim
        criteriaGroup.active = false;

        //zoom-out
        overworldView.ZoomOut();

        //wait for zoom-out
        while(overworldView.isBusy || M8.ModalManager.main.isBusy || M8.ModalManager.main.IsInStack(GameData.instance.modalHotspotInvestigate))
            yield return null;

        hotspotGroupCurrent.active = true;

        ModalShowOverworld();

        mRout = null;
    }

    IEnumerator DoLaunch(int regionIndex) {
        //pop investigate modal
        M8.ModalManager.main.CloseUpTo(GameData.instance.modalHotspotInvestigate, true);

        //hide investigate
        //anim
        landscapePreview.active = false;

        //hide critic group
        //anim
        criteriaGroup.active = false;

        //wait for modals
        while(M8.ModalManager.main.isBusy || M8.ModalManager.main.IsInStack(GameData.instance.modalHotspotInvestigate))
            yield return null;

        mRout = null;

        //go to colony scene
        var hotspotData = hotspotCurrent.data;

        int seasonIndex = hotspotData.climate.GetSeasonIndex(mCurSeasonData);

        if(hotspotData.colonyScene.isValid) {
            GameData.instance.ProgressNextToColony(hotspotData.colonyScene, regionIndex, seasonIndex);
        }
        else {
            Debug.LogWarning("Invalid colony scene for: " + hotspotData.name);
        }
    }

    void OnSeasonToggle(SeasonData season) {
        mCurSeasonData = season;
    }

    void OnHotspotClick(Hotspot hotspot) {
        if(isBusy)
            return;

        hotspotCurrent = hotspot;

        mRout = StartCoroutine(DoInvestigateEnter(hotspot));
    }

    void OnHotspotInvestigateBack() {
        if(isBusy)
            return;

        hotspotCurrent = null;

        mRout = StartCoroutine(DoInvestigateExit());
    }

    void OnHotspotInvestigateLaunch(int regionIndex) {
        if(isBusy)
            return;

        mRout = StartCoroutine(DoLaunch(regionIndex));
    }

    private void ModalShowOverworld() {
        mModalOverworldParms[ModalOverworld.parmAtmosphereActives] = atmosphereActiveOverlays;
        mModalOverworldParms[ModalOverworld.parmAtmosphere] = atmosphereDefault;
        mModalOverworldParms[ModalOverworld.parmSeason] = mCurSeasonData;
        mModalOverworldParms[ModalOverworld.parmCriteria] = hotspotGroupCurrent ? hotspotGroupCurrent.criteria : null;

        M8.ModalManager.main.Open(GameData.instance.modalOverworld, mModalOverworldParms);
    }
}
