using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class ColonySequence01Revised : ColonySequenceBase {
	[Header("Unit Data")]
	public UnitData gardener;
	public UnitData engineer;
	public UnitData hunter;

	[Header("Enemy Data")]
	public UnitData mole;

	[Header("Building Data")]
	public StructureData house;
	public StructureData plant;
	public StructureData waterTank;

	[Header("Illustrations")]
	public AnimatorEnterExit summonIllustrate;
	public AnimatorEnterExit gardenerIllustrate;
	public AnimatorEnterExit engineerIllustrate;
	public AnimatorEnterExit fighterIllustrate;

	[Header("Dialogs")]
	public ModalDialogFlowIncremental dlgIntro;
	public ModalDialogFlowIncremental dlgWeather;
	public ModalDialogFlowIncremental dlgPostIntro;
	public ModalDialogFlowIncremental dlgHousePlaced;
	public ModalDialogFlowIncremental dlgPlantPlaced;
	public ModalDialogFlowIncremental dlgSummonInstruction;
	public ModalDialogFlowIncremental dlgWaterTankPlaced;
	public ModalDialogFlowIncremental dlgMoleAppear;

	private bool mIsHouseSpawned = false;
	private bool mIsPlantSpawned = false;
	private bool mIsGardenerSpawned = false;
	private bool mIsWaterTankSpawned = false;
	private bool mIsEngineerSpawned = false;
	private bool mIsMoleSpawned = false;	

	public override void Init() {
		isPauseCycle = true;

		ColonyController.instance.structurePaletteController.signalInvokeStructureSpawned.callback += OnStructureSpawned;

		GameData.instance.signalUnitSpawned.callback += OnUnitSpawned;
	}

	public override void Deinit() {
		ColonyController.instance.structurePaletteController.signalInvokeStructureSpawned.callback -= OnStructureSpawned;

		GameData.instance.signalUnitSpawned.callback -= OnUnitSpawned;
	}

	public override IEnumerator Intro() {
		yield return dlgIntro.Play();
	}

	public override IEnumerator Forecast() {
		yield return dlgWeather.Play();
	}

	public override IEnumerator ColonyShipPreEnter() {
		yield return null;
	}

	public override IEnumerator ColonyShipPostEnter() {
		yield return dlgPostIntro.Play();

		isPauseCycle = false;
	}

	void OnUnitSpawned(Unit unit) {
		if(unit.data == engineer) {
			if(!mIsEngineerSpawned) {
				mIsEngineerSpawned = true;

				//resume the game
				isPauseCycle = false;
			}
		}
		else if(unit.data == gardener) {
			if(!mIsGardenerSpawned) {
				mIsGardenerSpawned = true;

				//resume the game
				isPauseCycle = false;
			}
		}
		else if(unit.data == mole) {
			if(!mIsMoleSpawned) {
				mIsMoleSpawned = true;

				StartCoroutine(DoMoleSpawned());
			}
		}
	}

	void OnStructureSpawned(Structure structure) {
		if(structure.data == house) {
			if(!mIsHouseSpawned) {
				mIsHouseSpawned = true;

				StartCoroutine(DoHouseSpawned(structure));
			}
		}
		else if(structure.data == plant) {
			if(!mIsPlantSpawned) {
				mIsPlantSpawned = true;

				StartCoroutine(DoPlantSpawned(structure));
			}
		}
		else if(structure.data == waterTank) {
			if(!mIsWaterTankSpawned) {
				mIsWaterTankSpawned = true;

				StartCoroutine(DoWaterTankSpawned());
			}
		}
	}

	IEnumerator DoHouseSpawned(Structure structure) {
		isPauseCycle = true;

		//wait for house to deploy completely
		while(structure.state == StructureState.Spawning)
			yield return null;

		yield return dlgHousePlaced.Play();

		isPauseCycle = false;
	}

	IEnumerator DoPlantSpawned(Structure structure) {
		//wait for plant to deploy completely
		while(structure.state == StructureState.Spawning)
			yield return null;

		isPauseCycle = true;

		//gardener dialog
		gardenerIllustrate.Show();
		yield return gardenerIllustrate.PlayEnterWait();

		yield return dlgPlantPlaced.Play();

		yield return gardenerIllustrate.PlayExitWait();
		gardenerIllustrate.Hide();

		//summon dialog
		summonIllustrate.Show();
		yield return summonIllustrate.PlayEnterWait();

		yield return dlgSummonInstruction.Play();

		yield return summonIllustrate.PlayExitWait();
		summonIllustrate.Hide();
	}

	IEnumerator DoWaterTankSpawned() {
		isPauseCycle = true;

		//engineer dialog
		engineerIllustrate.Show();
		yield return engineerIllustrate.PlayEnterWait();

		yield return dlgWaterTankPlaced.Play();

		yield return engineerIllustrate.PlayExitWait();
		engineerIllustrate.Hide();

		isPauseCycle = false;
	}

	IEnumerator DoMoleSpawned() {
		yield return new WaitForSeconds(1f);

		M8.SceneManager.instance.Pause();

		fighterIllustrate.Show();
		yield return fighterIllustrate.PlayEnterWait();

		yield return dlgMoleAppear.Play();

		yield return fighterIllustrate.PlayExitWait();
		fighterIllustrate.Hide();

		M8.SceneManager.instance.Resume();
	}
}
