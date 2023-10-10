using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class ColonySequence02 : ColonySequenceBase {
    [Header("Enemy Data")]
    public UnitData mushroom;
    public UnitData fly;
    public UnitData hopper;

	[Header("Building Data")]
	public StructureData house;

	[Header("Dialogs")]
    public ModalDialogFlowIncremental dlgIntro;
    public ModalDialogFlowIncremental dlgMushroom;
    public ModalDialogFlowIncremental dlgFly;
    public ModalDialogFlowIncremental dlgHazzard;

	private bool mIsHouseSpawned = false;

	private bool mIsMushroomSpawned;
    private bool mIsFlySpawned;
    private bool mIsHopperSpawned;

    private bool mIsHazzardHappened;

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
        //Debug.Log("Dialog about tropical climate.");
        yield return dlgIntro.Play();
    }

    public override void CycleNext() {
        if(ColonyController.instance.cycleController.cycleCurWeather.isHazzard) {
            if(!mIsHazzardHappened) {
                mIsHazzardHappened = true;

                //Debug.Log("Dialog about hazzard.");
                StartCoroutine(DoHazzardDialog());
            }
        }
    }

    IEnumerator DoHazzardDialog() {
        isPauseCycle = true;
        yield return dlgHazzard.Play();
        isPauseCycle = false;
    }

    void OnStructureSpawned(Structure structure) {
		if(structure.data == house) {
			if(!mIsHouseSpawned) {
				mIsHouseSpawned = true;

				isPauseCycle = false;
			}
		}
	}

	void OnUnitSpawned(Unit unit) {
        if(unit.data == mushroom) {
            if(!mIsMushroomSpawned) {
                mIsMushroomSpawned = true;

                //Debug.Log("Dialog about mushroom.");
                StartCoroutine(DoMushroomDialog());
            }
        }
        else if(unit.data == fly) {
            if(!mIsFlySpawned) {
                mIsFlySpawned = true;

                //Debug.Log("Dialog about fly.");
                StartCoroutine(DoFlyDialog());
            }
        }
        else if(unit.data == hopper) {
            if(!mIsHopperSpawned) {
                mIsHopperSpawned = true;

                //Debug.Log("Dialog about hopper.");
            }
        }
    }

    IEnumerator DoMushroomDialog() {
        ColonyController.instance.Resume();

        GameData.instance.signalClickCategory.Invoke(-1);

        var mushrooms = ColonyController.instance.unitController.GetUnitActivesByData(mushroom);
        if(mushrooms != null && mushrooms.Count > 0) {
            var mushroom = mushrooms[0];
            while(mushroom.state == UnitState.Spawning)
                yield return null;
        }

        ColonyController.instance.Pause();

        yield return dlgMushroom.Play();

        ColonyController.instance.Resume();
    }

    IEnumerator DoFlyDialog() {
        var flies = ColonyController.instance.unitController.GetUnitActivesByData(fly);
        if(flies != null && flies.Count > 0) {
            var fly = flies[0];
            while(fly.state == UnitState.Spawning)
                yield return null;
        }

        yield return new WaitForSeconds(1f);

        ColonyController.instance.Resume();

        GameData.instance.signalClickCategory.Invoke(-1);

        ColonyController.instance.Pause();

        yield return dlgFly.Play();

        ColonyController.instance.Resume();
    }
}
