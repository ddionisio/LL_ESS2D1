using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoLExt;
using UnityEngine.Events;

public class OverworldController : GameModeController<OverworldController> {
    [Header("Setup")]
    public AtmosphereAttributeBase atmosphereDefault;
    public SeasonData seasonDefault;
    public AtmosphereAttributeBase[] atmosphereActiveOverlays;

    [Header("Overworld")]
    public OverworldView overworldView;
    public ParticleSystem overworldWindFX;
    public bool overworldWindFXPlayOnStart = true;

    [Header("Hotspots")]
    public HotspotGroup hotspotGroup;
    public float hotspotZoom;

    [Header("Investigate")]
    public LandscapePreview landscapePreview;
    public CriteriaGroup criteriaGroup;

    [Header("Audio")]
    [M8.MusicPlaylist]
    public string music;
    [M8.SoundPlaylist]
    public string sfxInvestigateEnter;
    [M8.SoundPlaylist]
    public string sfxInvestigateExit;

    [Header("Sequence")]
    public OverworldSequenceBase sequence;

    [Header("Signal Listen")]
    public SignalAtmosphereAttribute signalListenAtmosphereToggle;
    public SignalSeasonData signalListenSeasonToggle;
    public SignalHotspot signalListenHotspotClick;
    public SignalHotspot signalListenHotspotInvestigate;
    public M8.Signal signalListenHotspotInvestigateBack;
    public M8.SignalInteger signalListenHotspotInvestigateLaunch;

    [Header("Signal Invoke")]
    public SignalAtmosphereAttribute signalInvokeAtmosphereOverlayDefault;
    public SignalSeasonData signalInvokeSeasonDefault;
    public SignalHotspot signalInvokeHotspotSelectChanged;

    public Hotspot hotspotCurrent { get; private set; }

    public bool isBusy { get { return mRout != null; } }

    public SeasonData currentSeason { get; private set; }
    public AtmosphereAttributeBase currentAtmosphere {  get; private set; }

    private Coroutine mRout;

    private M8.GenericParams mModalOverworldParms = new M8.GenericParams();
    private M8.GenericParams mModalHotspotAnalyzeParms = new M8.GenericParams();
    private M8.GenericParams mModalHotspotInvestigateParms = new M8.GenericParams();

    public void ClearCurrentHotspot() {
        if(hotspotCurrent) {
            hotspotCurrent.isSelected = false;
            hotspotCurrent = null;

            signalInvokeHotspotSelectChanged?.Invoke(hotspotCurrent);
		}
    }

    public void SetCurrentHotspot(Hotspot hotspot) {
		if(hotspotCurrent != hotspot) {
			if(hotspotCurrent) hotspotCurrent.isSelected = false;

			hotspotCurrent = hotspot;
			hotspotCurrent.isSelected = true;

			signalInvokeHotspotSelectChanged?.Invoke(hotspotCurrent);
		}
	}

    protected override void OnInstanceDeinit() {
        hotspotCurrent = null;

		if(sequence) sequence.Deinit();

		if(signalListenAtmosphereToggle) signalListenAtmosphereToggle.callback -= OnAtmosphereToggle;
		if(signalListenSeasonToggle) signalListenSeasonToggle.callback -= OnSeasonToggle;
        if(signalListenHotspotClick) signalListenHotspotClick.callback -= OnHotspotClick;
        if(signalListenHotspotInvestigate) signalListenHotspotInvestigate.callback -= OnHotspotInvestigate;
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

        if(GameData.instance.disableSequence)
            sequence = null;

        currentSeason = seasonDefault;

        if(hotspotGroup)
            hotspotGroup.active = false;

        if(landscapePreview)
            landscapePreview.active = false;

        if(criteriaGroup)
            criteriaGroup.active = false;

        //setup signals
        if(signalListenAtmosphereToggle) signalListenAtmosphereToggle.callback += OnAtmosphereToggle;
		if(signalListenSeasonToggle) signalListenSeasonToggle.callback += OnSeasonToggle;
        if(signalListenHotspotClick) signalListenHotspotClick.callback += OnHotspotClick;
        if(signalListenHotspotInvestigate) signalListenHotspotInvestigate.callback += OnHotspotInvestigate;
        if(signalListenHotspotInvestigateBack) signalListenHotspotInvestigateBack.callback += OnHotspotInvestigateBack;
        if(signalListenHotspotInvestigateLaunch) signalListenHotspotInvestigateLaunch.callback += OnHotspotInvestigateLaunch;

        if(sequence) sequence.Init();
    }

    protected override IEnumerator Start() {
        yield return base.Start();

        if(!string.IsNullOrEmpty(music))
            M8.MusicPlaylist.instance.Play(music, true, true);

        //show overworld

        //some intros
        if(sequence)
            yield return sequence.StartBegin();

        //wind FX
        if(overworldWindFX && overworldWindFXPlayOnStart)
            overworldWindFX.Play();
                
        //setup hotspot
        if(hotspotGroup) {
            //prep landscape prefabs for preview
            for(int i = 0; i < hotspotGroup.hotspots.Length; i++) {
                var hotspot = hotspotGroup.hotspots[i];

                landscapePreview.AddHotspotPreview(hotspot.hotspot.data);
            }

            //show
            hotspotGroup.active = true;
        }

        yield return null;

        //set season
        if(signalInvokeSeasonDefault) signalInvokeSeasonDefault.Invoke(currentSeason);

        //show overworld modal
        ModalShowOverworld();

        while(M8.ModalManager.main.isBusy)
            yield return null;

        if(sequence)
            yield return sequence.StartFinish();
    }

    IEnumerator DoInvestigateEnter(Hotspot hotspot) {
        if(sequence)
            yield return sequence.InvestigationEnterBegin();

        //turn off overlays
        if(signalInvokeAtmosphereOverlayDefault) signalInvokeAtmosphereOverlayDefault.Invoke(atmosphereDefault);

        //pop overworld modal
        M8.ModalManager.main.CloseUpTo(GameData.instance.modalOverworld, true);

        while(M8.ModalManager.main.isBusy || M8.ModalManager.main.IsInStack(GameData.instance.modalOverworld))
            yield return null;

        hotspotGroup.active = false;

        if(!string.IsNullOrEmpty(sfxInvestigateEnter))
            M8.SoundPlaylist.instance.Play(sfxInvestigateEnter, false);

        //zoom-in
        overworldView.ZoomIn(hotspot.position, hotspotZoom);

        //wait for zoom-in
        while(overworldView.isBusy)
            yield return null;

        //show preview
        landscapePreview.SetCurrentPreview(hotspot.data);
        landscapePreview.SetSeason(currentSeason);
        landscapePreview.active = true;
        //anim

        criteriaGroup.ApplyCriteria(hotspotGroup.criteria);

        //show critic group
        criteriaGroup.active = true;
        criteriaGroup.Show();

        //push investigate modal
        mModalHotspotInvestigateParms[ModalHotspotInvestigate.parmSeason] = currentSeason;
        mModalHotspotInvestigateParms[ModalHotspotInvestigate.parmCriteriaGroup] = criteriaGroup;
        mModalHotspotInvestigateParms[ModalHotspotInvestigate.parmLandscape] = landscapePreview;

        M8.ModalManager.main.Open(GameData.instance.modalHotspotInvestigate, mModalHotspotInvestigateParms);

        while(M8.ModalManager.main.isBusy)
            yield return null;

        if(sequence)
            yield return sequence.InvestigationEnterEnd();

        mRout = null;
    }

    IEnumerator DoInvestigateExit() {
        //pop investigate modal
        M8.ModalManager.main.CloseUpTo(GameData.instance.modalHotspotInvestigate, true);

        if(!string.IsNullOrEmpty(sfxInvestigateExit))
            M8.SoundPlaylist.instance.Play(sfxInvestigateExit, false);

        //hide investigate
        //anim
        landscapePreview.active = false;

        //hide critic group
        criteriaGroup.Hide();
        
        //zoom-out
        overworldView.ZoomOut();

        //wait for zoom-out
        while(overworldView.isBusy || M8.ModalManager.main.isBusy || M8.ModalManager.main.IsInStack(GameData.instance.modalHotspotInvestigate))
            yield return null;

        hotspotGroup.active = true;

        criteriaGroup.active = false;

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

        int seasonIndex = GameData.instance.GetSeasonIndex(currentSeason);

        if(hotspotData.colonyScene.isValid) {
            GameData.instance.ProgressNextToColony(hotspotData.colonyScene, regionIndex, seasonIndex);
        }
        else {
            Debug.LogWarning("Invalid colony scene for: " + hotspotData.name);
        }
    }

    void OnAtmosphereToggle(AtmosphereAttributeBase atmosphere) {
        currentAtmosphere = atmosphere;
	}

    void OnSeasonToggle(SeasonData season) {
        currentSeason = season;

        if(sequence) sequence.SeasonToggle(season);
    }

    void OnHotspotClick(Hotspot hotspot) {
        if(isBusy)
            return;

        SetCurrentHotspot(hotspot);

		//show analysis
		/*mModalHotspotAnalyzeParms[ModalHotspotAnalyze.parmHotspot] = hotspot;
        mModalHotspotAnalyzeParms[ModalHotspotAnalyze.parmSeason] = currentSeason;
        mModalHotspotAnalyzeParms[ModalHotspotAnalyze.parmCriteria] = hotspotGroup.criteria;

        M8.ModalManager.main.Open(GameData.instance.modalHotspotAnalyze, mModalHotspotAnalyzeParms);*/

        if(sequence) sequence.HotspotClick(hotspot);
    }

    void OnHotspotInvestigate(Hotspot hotspot) {
        if(isBusy)
            return;

        //hotspotCurrent = hotspot;

        mRout = StartCoroutine(DoInvestigateEnter(hotspot));
    }

    void OnHotspotInvestigateBack() {
        if(isBusy)
            return;

        //hotspotCurrent = null;

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
        mModalOverworldParms[ModalOverworld.parmSeason] = currentSeason;
        mModalOverworldParms[ModalOverworld.parmCriteria] = hotspotGroup ? hotspotGroup.criteria : null;

        M8.ModalManager.main.Open(GameData.instance.modalOverworld, mModalOverworldParms);
    }
}
