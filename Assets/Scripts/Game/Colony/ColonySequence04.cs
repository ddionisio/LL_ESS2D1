using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColonySequence04 : ColonySequenceBase {
    [Header("Enemy Data")]
    public UnitData burrow;

    [Header("Building Data")]
    public StructureData house;
    public StructureData waterTank;

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
        Debug.Log("Dialog about highland climate.");
        yield return null;
    }

    void OnUnitSpawned(Unit unit) {
        if(unit.data == burrow) {
            if(!mIsBurrowSpawned) {
                mIsBurrowSpawned = true;

                Debug.Log("Dialog about burrows.");
            }
        }
    }

    void OnStructureSpawned(Structure structure) {
        if(structure.data == house) {
            if(!mIsHouseSpawned) {
                mIsHouseSpawned = true;

                Debug.Log("Dialog about landscaping again.");

                LandscaperTutorialStart();
            }
        }
    }

    private void LandscaperTutorialStart() {
        StartCoroutine(DoLandScaperTutorial());
    }

    IEnumerator DoLandScaperTutorial() {
        //wait for population increase
        while(ColonyController.instance.population < 2)
            yield return null;

        Debug.Log("Dialog about doing excellent, and finally at the tail-end of this journey.");

        isPauseCycle = false;
        cyclePauseAllowProgress = false;
    }
}
