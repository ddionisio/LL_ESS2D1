using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColonySequence03 : ColonySequenceBase {
    [Header("Unit Data")]
    public UnitData engineer;
    public UnitData landscaper;
    public UnitData gardener;

    [Header("Building Data")]
    public StructureData house;
    public StructureData waterTank;
    public StructureData plant;

    [Header("Land Info")]
    public ArableField[] fields;

    private bool mIsHouseSpawned;

    private bool mIsWaterTankSpawned;
    private bool mIsEngineerSpawned;

    private bool mIsLandscaperSpawned;

    private bool mIsPlantSpawned;

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
        Debug.Log("Dialog about desert climate.");
        yield return null;
    }

    void OnUnitSpawned(Unit unit) {
        if(unit.data == engineer) {
            if(!mIsEngineerSpawned) {
                mIsEngineerSpawned = true;

                if(mIsWaterTankSpawned)
                    LandscaperTutorialStart();
                else {
                    Debug.Log("Dialog about placing water tank.");
                }
            }
        }
        else if(unit.data == landscaper) {
            mIsLandscaperSpawned = true;
        }
    }

    void OnStructureSpawned(Structure structure) {
        if(structure.data == house) {
            if(!mIsHouseSpawned) {
                mIsHouseSpawned = true;

                Debug.Log("Dialog about landscaping, then instruct player to place water tank.");
            }
        }
        else if(structure.data == waterTank) {
            if(!mIsWaterTankSpawned) {
                mIsWaterTankSpawned = true;

                if(mIsEngineerSpawned)
                    LandscaperTutorialStart();
                else {
                    Debug.Log("Dialog about spawning engineer.");
                }
            }
        }
        else if(structure.data == plant) {
            mIsPlantSpawned = true;
        }
    }

    private void LandscaperTutorialStart() {
        StartCoroutine(DoLandScaperTutorial());
    }

    IEnumerator DoLandScaperTutorial() {
        //wait for water tank to be spawned
        var isWaterTankReady = false;
        while(!isWaterTankReady) {
            yield return null;

            var activeStructures = ColonyController.instance.structurePaletteController.GetStructureActives(waterTank);
            if(activeStructures != null) {
                for(int i = 0; i < activeStructures.Count; i++) {
                    var structure = activeStructures[i];
                    if(structure.state == StructureState.Active) {
                        isWaterTankReady = true;
                        break;
                    }
                }
            }
        }

        //unlock landscaper
        ColonyController.instance.unitPaletteController.ForceShowUnit(landscaper);
        ColonyController.instance.unitPaletteController.IncreaseCapacity(1);

        Debug.Log("Talk about landscaping, and wait for it to finish one land.");

        //wait for landscaper to be spawned
        while(!mIsLandscaperSpawned)
            yield return null;

        //wait for a land to be finished
        bool isFieldReady = false;
        while(!isFieldReady) {
            yield return null;

            for(int i = 0; i < fields.Length; i++) {
                var field = fields[i];
                if(!field)
                    continue;

                if(field.isHealthFull) {
                    isFieldReady = true;
                    break;
                }
            }
        }

        cyclePauseAllowProgress = false;

        Debug.Log("Tell player of their excellent effort, ask them to place a plant.");

        while(!mIsPlantSpawned)
            yield return null;

        Debug.Log("Tell player of their excellent effort, remind them to tend the garden. play now resumes.");

        //unlock gardener
        ColonyController.instance.unitPaletteController.ForceShowUnit(gardener);
        ColonyController.instance.unitPaletteController.IncreaseCapacity(1);

        isPauseCycle = false;        
    }
}
