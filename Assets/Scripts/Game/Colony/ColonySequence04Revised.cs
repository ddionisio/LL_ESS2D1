using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class ColonySequence04Revised : ColonySequenceBase {
	[Header("Enemy Data")]
	public UnitData burrow;
	public UnitData debris;

	[Header("Dialog")]
	public ModalDialogFlowIncremental dlgIntro;
	public ModalDialogFlowIncremental dlgCave;
	public ModalDialogFlowIncremental dlgDebris;

	private bool mIsBurrowSpawned;
	private bool mIsDebrisSpawned;

	public override void Init() {
		GameData.instance.signalUnitSpawned.callback += OnUnitSpawned;
	}

	public override void Deinit() {
		GameData.instance.signalUnitSpawned.callback -= OnUnitSpawned;
	}

	public override IEnumerator Intro() {
		//Debug.Log("Dialog about highland climate.");
		yield return dlgIntro.Play();
	}

	void OnUnitSpawned(Unit unit) {
		if(unit.data == burrow) {
			if(!mIsBurrowSpawned) {
				mIsBurrowSpawned = true;
				StartCoroutine(DoBurrowDialog(unit));
			}
		}
		else if(unit.data == debris) {
			if(!mIsDebrisSpawned) {
				mIsDebrisSpawned = true;
				StartCoroutine(DoDebrisDialog(unit));
			}
		}
	}

	IEnumerator DoBurrowDialog(Unit unit) {
		//ColonyController.instance.Resume();

		//GameData.instance.signalClickCategory.Invoke(-1);

		while(unit.state == UnitState.Spawning)
			yield return null;

		ColonyController.instance.Pause();

		yield return dlgCave.Play();

		ColonyController.instance.Resume();
	}

	IEnumerator DoDebrisDialog(Unit unit) {
		yield return new WaitForSeconds(0.5f);

		ColonyController.instance.Pause();

		yield return dlgDebris.Play();

		ColonyController.instance.Resume();
	}
}
