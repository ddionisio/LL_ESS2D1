using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class ColonySequence03Revised : ColonySequenceBase {
	[Header("Enemy Data")]
	public UnitData flame;

	[Header("Illustrations")]
	public AnimatorEnterExit flameDouseIllustrate;

	[Header("Dialogs")]
	public ModalDialogFlowIncremental dlgIntro;
	public ModalDialogFlowIncremental dlgFlameSpawned;
	public ModalDialogFlowIncremental dlgFlameDouse;

	private bool mIsFlameSpawned;

	public override void Init() {
		GameData.instance.signalUnitSpawned.callback += OnUnitSpawned;
	}

	public override void Deinit() {
		GameData.instance.signalUnitSpawned.callback -= OnUnitSpawned;
	}

	public override IEnumerator Intro() {
		//Debug.Log("Dialog about desert climate.");
		yield return dlgIntro.Play();
	}

	void OnUnitSpawned(Unit unit) {
		if(unit.data == flame) {
			if(!mIsFlameSpawned) {
				mIsFlameSpawned = true;
				StartCoroutine(DoFlameSpawned(unit));
			}
		}
	}

	IEnumerator DoFlameSpawned(Unit unit) {
		yield return new WaitForSeconds(0.5f);

		M8.SceneManager.instance.Pause();

		yield return dlgFlameSpawned.Play();

		flameDouseIllustrate.Show();
		yield return flameDouseIllustrate.PlayEnterWait();

		yield return dlgFlameDouse.Play();

		yield return flameDouseIllustrate.PlayExitWait();
		flameDouseIllustrate.Hide();

		M8.SceneManager.instance.Resume();
	}
}
