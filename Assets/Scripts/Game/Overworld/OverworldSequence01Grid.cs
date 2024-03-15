using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class OverworldSequence01Grid : OverworldSequenceBase {
	[Header("Atmosphere Toggles")]
	public AtmosphereAttributeBase none;
	public AtmosphereAttributeBase temperature;

	[Header("Intro")]
	public ModalDialogFlowIncremental introDlg;
	public AnimatorEnterExit criteriaIllustrate;
	public ModalDialogFlowIncremental criteriaDlg;

	[Header("Overworld")]
	public ModalDialogFlowIncremental latitudeDlg;
	public AnimatorEnterExit latitudeIllustrate;
	public ModalDialogFlowIncremental tempIntroDlg;
	public ModalDialogFlowIncremental tempDlg;
	//public AnimatorEnterExit sunIllustrate;
	//public ModalDialogFlowIncremental sunDlg;
	public ModalDialogFlowIncremental hotspotDlg;

	[Header("Investigate")]
	public ModalDialogFlowIncremental investigateIntroDlg;
	public ModalDialogFlowIncremental investigateMapDlg;
	public ModalDialogFlowIncremental investigateDlg;

	[Header("Signal Invoke")]
	public SignalAtmosphereAttribute signalInvokeAtmosphereToggle;

	//private bool mIsAnalyzeShown;
	private bool mIsInvestigateShown;

	public override IEnumerator StartBegin() {
		var modalOverworld = M8.ModalManager.main.GetBehaviour<ModalOverworld>(GameData.instance.modalOverworld);
		modalOverworld.atmosphereToggle.active = false;

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
		yield return latitudeDlg.Play();

		if(latitudeIllustrate) latitudeIllustrate.Show();

		yield return new WaitForSeconds(4f);

		if(latitudeIllustrate) latitudeIllustrate.Hide();

		yield return tempIntroDlg.Play();

		if(signalInvokeAtmosphereToggle) signalInvokeAtmosphereToggle.Invoke(temperature);

		yield return tempDlg.Play();

		if(signalInvokeAtmosphereToggle) signalInvokeAtmosphereToggle.Invoke(none);

		var modalOverworld = M8.ModalManager.main.GetBehaviour<ModalOverworld>(GameData.instance.modalOverworld);
		modalOverworld.atmosphereToggle.active = true;
		modalOverworld.atmosphereToggleHighlightGO.SetActive(true);

		yield return hotspotDlg.Play();

		modalOverworld.atmosphereToggleHighlightGO.SetActive(false);
	}

	public override IEnumerator InvestigationEnterEnd() {
		if(!mIsInvestigateShown) {
			mIsInvestigateShown = true;

			yield return investigateIntroDlg.Play();

			var modalInvestigate = M8.ModalManager.main.GetBehaviour<ModalHotspotInvestigateGrid>(GameData.instance.modalHotspotInvestigateGrid);
			if(modalInvestigate && modalInvestigate.mapHighlightGO)
				modalInvestigate.mapHighlightGO.SetActive(true);

			//describe topography
			yield return investigateMapDlg.Play();

			if(modalInvestigate && modalInvestigate.mapHighlightGO)
				modalInvestigate.mapHighlightGO.SetActive(false);

			yield return investigateDlg.Play();
		}
	}
}