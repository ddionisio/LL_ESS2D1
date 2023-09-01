using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class ColonySequence04 : ColonySequenceBase {
    [Header("Enemy Data")]
    public UnitData burrow;

    [Header("Building Data")]
    public StructureData house;
    public StructureData waterTank;

    [Header("Dialog")]
    public ModalDialogFlowIncremental dlgIntro;
    public ModalDialogFlowIncremental dlgLandscape;
    public ModalDialogFlowIncremental dlgCave;

    private bool mIsHouseSpawned;

    private bool mIsBurrowSpawned;

    public override void Init() {
        isPauseCycle = true;
        cyclePauseAllowProgress = true;

        ColonyController.instance.structurePaletteController.signalInvokeStructureSpawned.callback += OnStructureSpawned;

        GameData.instance.signalUnitSpawned.callback += OnUnitSpawned;
    }

    public override void Deinit() {
        ColonyController.instance.structurePaletteController.signalInvokeStructureSpawned.callback -= OnStructureSpawned;

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

                //Debug.Log("Dialog about burrows.");
                StartCoroutine(DoBurrowDialog());
            }
        }
    }

    IEnumerator DoBurrowDialog() {
        ColonyController.instance.Resume();

        GameData.instance.signalClickCategory.Invoke(-1);

        var burrows = ColonyController.instance.unitController.GetUnitActivesByData(burrow);
        if(burrows != null && burrows.Count > 0) {
            var burrow = burrows[0];
            while(burrow.state == UnitState.Spawning)
                yield return null;
        }

        ColonyController.instance.Pause();

        yield return dlgCave.Play();

        ColonyController.instance.Resume();
    }

    void OnStructureSpawned(Structure structure) {
        if(structure.data == house) {
            if(!mIsHouseSpawned) {
                mIsHouseSpawned = true;

                //Debug.Log("Dialog about landscaping again.");

                LandscaperTutorialStart();
            }
        }
    }

    private void LandscaperTutorialStart() {
        StartCoroutine(DoLandScaperTutorial());
    }

    IEnumerator DoLandScaperTutorial() {
        yield return dlgLandscape.Play();

        //wait for population increase
        while(ColonyController.instance.population < 2)
            yield return null;

        cyclePauseAllowProgress = false;
        isPauseCycle = false;
    }
}
