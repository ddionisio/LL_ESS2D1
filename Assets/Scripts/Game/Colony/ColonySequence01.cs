using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColonySequence01 : ColonySequenceBase {
    [Header("Unit Data")]
    public UnitData gardener;
    public UnitData engineer;
    public UnitData hunter;

    [Header("Enemy Data")]
    public UnitData vine;
    public UnitData mole;

    [Header("Building Data")]
    public StructureData house;
    public StructureData plant;
    public StructureData waterTank;
    public StructureData solarPower;

    private bool mIsGardenerSpawned = false;

    private bool mIsEngineerSpawned = false;

    private bool mIsHouseSpawned = false;
    private bool mIsHouseSecondSpawned = false;

    private bool mIsPopulationIncreased = false;

    private bool mIsPlantSpawned = false;

    private bool mIsWaterTankSpawned = false;
    private bool mIsSolarPowerSpawned = false;

    private bool mIsVineSpawned = false;
    private bool mIsMoleSpawned = false;

    public override void Init() {
        isPauseCycle = true;
        cyclePauseAllowProgress = true;

        ColonyController.instance.signalInvokePopulationUpdate.callback += OnPopulationUpdate;

        ColonyController.instance.structurePaletteController.signalInvokeStructureSpawned.callback += OnStructureSpawned;

        GameData.instance.signalUnitSpawned.callback += OnUnitSpawned;
    }

    public override void Deinit() {
        ColonyController.instance.signalInvokePopulationUpdate.callback -= OnPopulationUpdate;

        ColonyController.instance.structurePaletteController.signalInvokeStructureSpawned.callback -= OnStructureSpawned;

        GameData.instance.signalUnitSpawned.callback -= OnUnitSpawned;
    }

    public override IEnumerator Intro() {
        Debug.Log("Dialog about temperate climate.");
        yield return null;
    }

    public override IEnumerator Forecast() {
        Debug.Log("Dialog about climate vs. weather.");
        yield return null;
    }

    public override IEnumerator ColonyShipPreEnter() {
        yield return null;
    }

    public override IEnumerator ColonyShipPostEnter() {
        Debug.Log("Dialog about placing houses and game premise.");
        yield return null;
    }

    public override void CycleBegin() {

    }

    public override void CycleNext() {

    }

    public override IEnumerator CycleEnd() {
        yield return null;
    }

    void OnPopulationUpdate() {
        var colonyCtrl = ColonyController.instance;

        if(colonyCtrl.population >= 2) {
            if(!mIsPopulationIncreased) {
                mIsPopulationIncreased = true;

                cyclePauseAllowProgress = false;

                Debug.Log("Dialog about population increase, new house available.");
            }
        }
    }

    void OnUnitSpawned(Unit unit) {
        if(unit.data == gardener) {
            if(!mIsGardenerSpawned) {
                mIsGardenerSpawned = true;

                if(mIsPlantSpawned) {
                    //Debug.Log("Dialog about population increase, and new house to place.");
                }
                else {
                    Debug.Log("Dialog about spawning plant.");
                }
            }
        }
        else if(unit.data == engineer) {
            if(!mIsEngineerSpawned) {                
                mIsEngineerSpawned = true;

                Debug.Log("Dialog about engineers as builders, etc. etc.");

                //finally proceed with game
                isPauseCycle = false;
                cyclePauseAllowProgress = false;
            }
        }
        else if(unit.data == vine) {
            if(!mIsVineSpawned) {
                mIsVineSpawned = true;

                Debug.Log("Dialog about vines and the need for gardener.");
            }
        }
        else if(unit.data == mole) {
            if(!mIsMoleSpawned) {
                mIsMoleSpawned = true;

                ColonyController.instance.unitPaletteController.ForceShowUnit(hunter);
                ColonyController.instance.unitPaletteController.IncreaseCapacity(1);

                Debug.Log("Dialog about mole, and the use of hunters.");
            }
        }
    }

    void OnStructureSpawned(Structure structure) {
        if(structure.data == house) {
            if(!mIsHouseSpawned) {
                mIsHouseSpawned = true;

                Debug.Log("Dialog about needing plant and gardener.");
            }
            else if(!mIsHouseSecondSpawned) {
                mIsHouseSecondSpawned = true;                

                Debug.Log("Dialog about needing water tank and solar power.");
                                
                //unlock water tank and solar panel
                ColonyController.instance.structurePaletteController.ForceShowStructure(solarPower);
                ColonyController.instance.structurePaletteController.ForceShowStructure(waterTank);
            }
        }
        else if(structure.data == plant) {
            if(!mIsPlantSpawned) {
                mIsPlantSpawned = true;

                if(mIsGardenerSpawned) {
                    Debug.Log("Dialog about population increase, and new house to place.");
                }
                else {
                    Debug.Log("Dialog about spawning gardener.");
                }
            }
        }
        else if(structure.data == waterTank) {
            if(!mIsWaterTankSpawned) {
                mIsWaterTankSpawned = true;

                if(mIsSolarPowerSpawned) {
                    Debug.Log("Dialog about needing engineers to construct them.");

                    //unlock engineer
                    ColonyController.instance.unitPaletteController.ForceShowUnit(engineer);
                }
                else {
                    Debug.Log("Dialog about spawning power plant.");
                }
            }
        }
        else if(structure.data == solarPower) {
            if(!mIsSolarPowerSpawned) {
                mIsSolarPowerSpawned = true;

                if(mIsWaterTankSpawned) {
                    Debug.Log("Dialog about needing engineers to construct them.");

                    //unlock engineer
                    ColonyController.instance.unitPaletteController.ForceShowUnit(engineer);
                }
                else {
                    Debug.Log("Dialog about spawning water tank.");
                }
            }
        }
    }
}
