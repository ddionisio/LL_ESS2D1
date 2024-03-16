using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class OverworldSequence02Grid : OverworldSequenceBase {
	[Header("Atmosphere Toggles")]
	public AtmosphereAttributeBase none;
	public AtmosphereAttributeBase humidity;

	[Header("Overworld")]
	public ModalDialogFlowIncremental dlgIntro;
	public ModalDialogFlowIncremental humidDlg;
	public ModalDialogFlowIncremental dlgIntroPost;

	[Header("Signal Invoke")]
	public SignalAtmosphereAttribute signalInvokeAtmosphereToggle;

	public override IEnumerator StartBegin() {
		yield return dlgIntro.Play();
	}

	public override IEnumerator StartFinish() {
		OverworldControllerGrid.instance.hotspotGroup.active = false;

		if(signalInvokeAtmosphereToggle) signalInvokeAtmosphereToggle.Invoke(humidity);

		yield return humidDlg.Play();

		if(signalInvokeAtmosphereToggle) signalInvokeAtmosphereToggle.Invoke(none);

		OverworldControllerGrid.instance.hotspotGroup.active = true;

		yield return dlgIntroPost.Play();
	}
}