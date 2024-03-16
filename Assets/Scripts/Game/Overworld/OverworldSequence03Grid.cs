using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class OverworldSequence03Grid : OverworldSequenceBase {
	[Header("Atmosphere Toggles")]
	public AtmosphereAttributeBase none;
	public AtmosphereAttributeBase wind;
	public AtmosphereAttributeBase temperature;

	[Header("Illustrations")]
	public AnimatorEnterExit windDirsIllustration;

	[Header("Dialogs")]
	public ModalDialogFlowIncremental dlgIntro;
	public ModalDialogFlowIncremental dlgCoriolis;
	public ModalDialogFlowIncremental dlgWinds;
	public ModalDialogFlowIncremental dlgIntroEnd;

	[Header("Signal Invoke")]
	public SignalAtmosphereAttribute signalInvokeAtmosphereToggle;

	public override IEnumerator StartBegin() {

		yield return dlgIntro.Play();
	}

	public override IEnumerator StartFinish() {
		OverworldControllerGrid.instance.hotspotGroup.active = false;

		if(OverworldControllerGrid.instance.overworldWindFX)
			OverworldControllerGrid.instance.overworldWindFX.Play();

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

		signalInvokeAtmosphereToggle.Invoke(none);

		OverworldControllerGrid.instance.hotspotGroup.active = true;

		yield return dlgIntroEnd.Play();
	}
}
