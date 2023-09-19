using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class OverworldSequence02 : OverworldSequenceBase {
    [Header("Atmosphere Toggles")]
    public AtmosphereAttributeBase none;
    public AtmosphereAttributeBase wind;
    public AtmosphereAttributeBase temperature;

    [Header("Illustrations")]
    public AnimatorEnterExit windDirsIllustration;
	public AnimatorEnterExit gulfStreamIllustration;

	[Header("Dialogs")]
    public ModalDialogFlowIncremental dlgIntro;
    public ModalDialogFlowIncremental dlgCoriolis;
    public ModalDialogFlowIncremental dlgWinds;
	public ModalDialogFlowIncremental dlgOcean;
	public ModalDialogFlowIncremental dlgGulfStream;
	public ModalDialogFlowIncremental dlgIntroPost;

    [Header("Signal Invoke")]
    public SignalAtmosphereAttribute signalInvokeAtmosphereToggle;
    public SignalSeasonData signalInvokeSeasonToggle;

    private Coroutine mSeasonCycleRout;

    public override IEnumerator StartBegin() {
        yield return dlgIntro.Play();

		signalInvokeSeasonToggle?.Invoke(GameData.instance.seasons[0]);

		if(OverworldController.instance.overworldWindFX)
			OverworldController.instance.overworldWindFX.Play();

		signalInvokeAtmosphereToggle.Invoke(wind);

        yield return new WaitForSeconds(2f);

        yield return dlgCoriolis.Play();

        if(windDirsIllustration) {
            windDirsIllustration.Show();
            windDirsIllustration.PlayEnter();
		}

		yield return new WaitForSeconds(2f);

        yield return dlgWinds.Play();

		if(windDirsIllustration) {
            yield return windDirsIllustration.PlayExitWait();
			windDirsIllustration.Hide();
		}

		signalInvokeAtmosphereToggle.Invoke(temperature);

		yield return dlgOcean.Play();

        if(gulfStreamIllustration) {
            gulfStreamIllustration.Show();
            gulfStreamIllustration.PlayEnter();
		}

        StartSeasonCycle();

		yield return dlgGulfStream.Play();

        StopSeasonCycle();

		if(gulfStreamIllustration) {
            yield return gulfStreamIllustration.PlayExitWait();
			gulfStreamIllustration.Hide();			
		}

		signalInvokeAtmosphereToggle.Invoke(none);
	}

    public override IEnumerator StartFinish() {
        yield return dlgIntroPost.Play();
    }

    private void StartSeasonCycle() {
        if(mSeasonCycleRout != null)
            StopCoroutine(mSeasonCycleRout);

        mSeasonCycleRout = StartCoroutine(DoSeasonCycle());
	}

    private void StopSeasonCycle() {
        if(mSeasonCycleRout != null) {
            StopCoroutine(mSeasonCycleRout);
            mSeasonCycleRout = null;
		}

        signalInvokeSeasonToggle?.Invoke(GameData.instance.seasons[0]);
	}

    IEnumerator DoSeasonCycle() {
        var wait = new WaitForSeconds(4f);

        int curInd = 1;

        while(true) {
            signalInvokeSeasonToggle?.Invoke(GameData.instance.seasons[curInd]);

            curInd++;
            if(curInd == GameData.instance.seasons.Length)
                curInd = 0;

            yield return wait;
        }
    }
}