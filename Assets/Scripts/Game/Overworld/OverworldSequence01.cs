using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class OverworldSequence01 : OverworldSequenceBase {
    [Header("Atmosphere Toggles")]
    public AtmosphereAttributeBase none;
    public AtmosphereAttributeBase temperature;
    public AtmosphereAttributeBase humidity;

    [Header("Intro")]
    public ModalDialogFlowIncremental introDlg;
    public AnimatorEnterExit criteriaIllustrate;
    public ModalDialogFlowIncremental criteriaDlg;

    [Header("Overworld")]
    public ModalDialogFlowIncremental hudDlg;
    public ModalDialogFlowIncremental tempDlg;
    public AnimatorEnterExit sunIllustrate;
    public ModalDialogFlowIncremental sunDlg;
    public ModalDialogFlowIncremental humidDlg;
    public ModalDialogFlowIncremental hotspotDlg;

    [Header("Analyze")]
    public ModalDialogFlowIncremental analyzeDlg;

    [Header("Investigate")]
    public ModalDialogFlowIncremental investigateDlg;

    [Header("Signal Invoke")]
    public SignalAtmosphereAttribute signalInvokeAtmosphereToggle;

    private bool mIsAnalyzeShown;
    private bool mIsInvestigateShown;

    public override IEnumerator StartBegin() {
        var modalOverworld = M8.ModalManager.main.GetBehaviour<ModalOverworld>(GameData.instance.modalOverworld);
        modalOverworld.atmosphereToggle.active = false;
        modalOverworld.seasonToggle.active = false;

        yield return introDlg.Play();

        if(criteriaIllustrate) {
            criteriaIllustrate.Show();
            criteriaIllustrate.PlayEnter();
        }

        yield return criteriaDlg.Play();

        if(criteriaIllustrate) {
            yield return criteriaIllustrate.PlayExitWait();
            criteriaIllustrate.Hide();
        }
    }

    public override IEnumerator StartFinish() {
        yield return hudDlg.Play();

        if(signalInvokeAtmosphereToggle) signalInvokeAtmosphereToggle.Invoke(temperature);

        yield return tempDlg.Play();

        /*if(sunIllustrate) {
            sunIllustrate.Show();
            sunIllustrate.PlayEnter();
        }

        yield return sunDlg.Play();

        if(sunIllustrate) {
            yield return sunIllustrate.PlayExitWait();
            sunIllustrate.Hide();
        }*/

        if(signalInvokeAtmosphereToggle) signalInvokeAtmosphereToggle.Invoke(humidity);

        yield return humidDlg.Play();

        if(signalInvokeAtmosphereToggle) signalInvokeAtmosphereToggle.Invoke(none);

        var modalOverworld = M8.ModalManager.main.GetBehaviour<ModalOverworld>(GameData.instance.modalOverworld);
        modalOverworld.atmosphereToggle.active = true;

        yield return hotspotDlg.Play();
    }

    public override void HotspotClick(Hotspot hotspot) {
        if(!mIsAnalyzeShown) {
            mIsAnalyzeShown = true;
            StartCoroutine(DoAnalyzeSequence());
        }
    }

    IEnumerator DoAnalyzeSequence() {
        while(M8.ModalManager.main.IsInStack(GameData.instance.modalHotspotAnalyze) || M8.ModalManager.main.isBusy)
            yield return null;

        yield return analyzeDlg.Play();

        var modalOverworld = M8.ModalManager.main.GetBehaviour<ModalOverworld>(GameData.instance.modalOverworld);
        modalOverworld.seasonToggle.active = true;
    }

    public override IEnumerator InvestigationEnterEnd() {
        if(!mIsInvestigateShown) {
            mIsInvestigateShown = true;
            yield return investigateDlg.Play();
        }
    }
}
