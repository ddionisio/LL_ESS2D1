using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModalOverworld : M8.ModalController, M8.IModalPush, M8.IModalPop {
    public const string parmAtmosphereActives = "overworldAtmos"; //AtmosphereAttributeBase[], determines toggles to activate
    public const string parmAtmosphere = "overworldAtmosphere"; //AtmosphereAttributeBase (current selected/default)
    public const string parmSeason = "overworldSeason"; //SeasonData
    public const string parmCriteria = "overworldCriteria"; //CriteriaData
    public const string parmHintDialogTextRef = "overworldHintTextRef"; //string

    [Header("Controls")]
    public AtmosphereAttributeSelectWidget atmosphereToggle;

    public SeasonSelectWidget seasonToggle;

    [Header("Display")]
    public CriteriaWidget criteriaDisplay;
    public AtmosphereAttributeWidget atmosphereLegend; //used for measurement legend
    public GameObject atmosphereToggleHighlightGO;
	public GameObject seasonToggleHighlightGO;

    [Header("Hint")]
    public GameObject hintRootGO;
    public LoLExt.AnimatorEnterExit hintDialog;
	public GameObject hintPortraitActiveGO;
	public M8.TextMeshPro.TextMeshProTypewriter hintDialogText;

	[Header("Signal Listen")]
    public SignalAtmosphereAttribute signalListenAtmosphereToggle;
	public SignalHotspot signalListenHotspotAnalyzeComplete;

	public bool hintActive { get { return hintRootGO ? hintRootGO.activeSelf : false; } set { if(hintRootGO) hintRootGO.SetActive(value); } }

    private string mHintDialogTextRef;
    private float mHintDialogTextDelay;

    private Coroutine mHintRout;
    private int mHotSpotCounterMistake;

    public void ShowHintDialog() {
        if(mHintRout != null) return;

		if(hintDialog && !string.IsNullOrEmpty(mHintDialogTextRef))
			mHintRout = StartCoroutine(DoHintShow());
    }

    void M8.IModalPop.Pop() {
        if(signalListenAtmosphereToggle) signalListenAtmosphereToggle.callback -= OnAtmosphereToggle;
        if(signalListenHotspotAnalyzeComplete) signalListenHotspotAnalyzeComplete.callback -= OnHotspotAnalyzeComplete;

        hintActive = false;
		if(hintDialog) hintDialog.Hide();

		if(mHintRout != null) {
            StopCoroutine(mHintRout);
            mHintRout = null;
		}

		mHintDialogTextRef = null;

		mHotSpotCounterMistake = 0;
	}

    void M8.IModalPush.Push(M8.GenericParams parms) {

        AtmosphereAttributeBase atmosphereSelected = null;

        if(parms != null) {            
            if(parms.ContainsKey(parmAtmosphere))
                atmosphereSelected = parms.GetValue<AtmosphereAttributeBase>(parmAtmosphere);

            //setup atmosphere overlay toggle controls
            if(parms.ContainsKey(parmAtmosphereActives)) {
                var atmosphereActives = parms.GetValue<AtmosphereAttributeBase[]>(parmAtmosphereActives);

                //index 0 is the 'none' overlay
                atmosphereToggle.Setup(atmosphereSelected, atmosphereActives);
            }

            //setup season toggle controls
            if(parms.ContainsKey(parmSeason)) {
                var curSeasonDat = parms.GetValue<SeasonData>(parmSeason);

                seasonToggle.Setup(curSeasonDat);
            }

            //setup criteria display
            if(parms.ContainsKey(parmCriteria)) {
                var criteriaDat = parms.GetValue<CriteriaData>(parmCriteria);

                criteriaDisplay.Setup(criteriaDat);
            }

            //setup hint (ensure there is a voice duration in the localization spreadsheet)
            if(parms.ContainsKey(parmHintDialogTextRef)) {                
                var lolLocalize = M8.Localize.instance as LoLExt.LoLLocalize;
                if(lolLocalize) {
                    var txtRef = parms.GetValue<string>(parmHintDialogTextRef);
                    var txtInf = lolLocalize.GetExtraInfo(txtRef);
                    if(txtInf != null) {
                        mHintDialogTextRef = txtRef;
                        mHintDialogTextDelay = txtInf.voiceDuration;
					}
				}
            }
        }

        if(atmosphereLegend) {
            atmosphereLegend.gameObject.SetActive(false);

            if(signalListenAtmosphereToggle) signalListenAtmosphereToggle.callback += OnAtmosphereToggle;
			if(signalListenHotspotAnalyzeComplete) signalListenHotspotAnalyzeComplete.callback += OnHotspotAnalyzeComplete;
		}

		hintActive = false;
		if(hintDialog) hintDialog.Hide();
		mHotSpotCounterMistake = 0;
	}

    void OnAtmosphereToggle(AtmosphereAttributeBase atmosphereAttribute) {
        if(atmosphereAttribute.legendRange) {
            atmosphereLegend.data = atmosphereAttribute;
            atmosphereLegend.gameObject.SetActive(true);
        }
        else
            atmosphereLegend.gameObject.SetActive(false);
    }

    void OnHotspotAnalyzeComplete(Hotspot hotspot) {
        if(mHotSpotCounterMistake == GameData.instance.overworldHotspotHintCounter)
            return;

		var season = OverworldController.instance.currentSeason;
		var curAtmos = OverworldController.instance.currentAtmosphere;

        var result = hotspot.GetSeasonAtmosphereAnalyze(season, curAtmos);
        if(result == Hotspot.AnalyzeResult.Less || result == Hotspot.AnalyzeResult.Greater) {
            mHotSpotCounterMistake++;
            if(mHotSpotCounterMistake == GameData.instance.overworldHotspotHintCounter) {
				hintActive = true;
			}
        }
    }

    IEnumerator DoHintShow() {
        if(hintDialogText) hintDialogText.gameObject.SetActive(false);
		if(hintPortraitActiveGO) hintPortraitActiveGO.SetActive(false);

		hintDialog.Show();

        yield return hintDialog.PlayEnterWait();

		if(hintPortraitActiveGO) hintPortraitActiveGO.SetActive(true);

		if(hintDialogText) {
			hintDialogText.Clear();
			hintDialogText.text = M8.Localize.Get(mHintDialogTextRef);
			hintDialogText.gameObject.SetActive(true);
		}

        yield return null;

		LoLExt.LoLManager.instance.SpeakText(mHintDialogTextRef);

        var lastTime = Time.time;

        while(hintDialogText.isBusy)
		    yield return null;

		if(hintPortraitActiveGO) hintPortraitActiveGO.SetActive(false);

        while(Time.time - lastTime < mHintDialogTextDelay)
            yield return null;

        yield return hintDialog.PlayExitWait();

        hintDialog.Hide();

		mHintRout = null;
    }
}
