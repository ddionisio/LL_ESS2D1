using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

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

    [Header("Dialogs")]
    public ModalDialogFlowIncremental dlgIntro;
    public ModalDialogFlowIncremental dlgWeather;
    public ModalDialogFlowIncremental dlgPostIntro;
    public ModalDialogFlowIncremental dlgHousePlaced;
    public ModalDialogFlowIncremental dlgHouseSecondPlaced;
    public ModalDialogFlowIncremental dlgWaterSolarPlaced;
    public ModalDialogFlowIncremental dlgEngineerPlaced;
    public ModalDialogFlowIncremental dlgVineAppear;
    public ModalDialogFlowIncremental dlgMoleAppear;

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
            }
        }
    }

    void OnUnitSpawned(Unit unit) {
        if(unit.data == gardener) {
            if(!mIsGardenerSpawned) {
                mIsGardenerSpawned = true;

                if(mIsPlantSpawned) {
                }
                else {
                }
            }
        }
        else if(unit.data == engineer) {
            if(!mIsEngineerSpawned) {                
                mIsEngineerSpawned = true;

                //finally proceed with game
                StartCoroutine(DoEngineerSummoned());
            }
        }
        else if(unit.data == vine) {
            if(!mIsVineSpawned) {
                mIsVineSpawned = true;

                StartCoroutine(DoVineAppear());
            }
        }
        else if(unit.data == mole) {
            if(!mIsMoleSpawned) {
                mIsMoleSpawned = true;

                ColonyController.instance.unitPaletteController.ForceShowUnit(hunter);
                ColonyController.instance.unitPaletteController.IncreaseCapacity(1);

                StartCoroutine(DoMoleAppear());
            }
        }
    }

    IEnumerator DoVineAppear() {
        ColonyController.instance.Resume();

        GameData.instance.signalClickCategory.Invoke(-1);

        var vines = ColonyController.instance.unitController.GetUnitActivesByData(vine);
        if(vines != null && vines.Count > 0) {
            var vine = vines[0];
            while(vine.state == UnitState.Spawning)
                yield return null;
        }

        ColonyController.instance.Pause();

        yield return dlgVineAppear.Play();

        ColonyController.instance.Resume();
    }

    IEnumerator DoMoleAppear() {
        ColonyController.instance.Resume();

        GameData.instance.signalClickCategory.Invoke(-1);

        var moles = ColonyController.instance.unitController.GetUnitActivesByData(mole);
        if(moles != null && moles.Count > 0) {
            var mole = moles[0];
            while(mole.state == UnitState.Spawning)
                yield return null;
        }

        ColonyController.instance.Pause();

        yield return dlgMoleAppear.Play();

        ColonyController.instance.Resume();
    }

    IEnumerator DoEngineerSummoned() {
        cyclePauseAllowProgress = false;

        yield return dlgEngineerPlaced.Play();

        isPauseCycle = false;
    }

    void OnStructureSpawned(Structure structure) {
        if(structure.data == house) {
            if(!mIsHouseSpawned) {
                mIsHouseSpawned = true;

                StartCoroutine(dlgHousePlaced.Play());
            }
            else if(!mIsHouseSecondSpawned) {
                mIsHouseSecondSpawned = true;
                                
                //unlock water tank and solar panel
                ColonyController.instance.structurePaletteController.ForceShowStructure(solarPower);
                ColonyController.instance.structurePaletteController.ForceShowStructure(waterTank);

                StartCoroutine(dlgHouseSecondPlaced.Play());
            }
        }
        else if(structure.data == plant) {
            if(!mIsPlantSpawned) {
                mIsPlantSpawned = true;

                if(mIsGardenerSpawned) {
                }
                else {
                }
            }
        }
        else if(structure.data == waterTank) {
            if(!mIsWaterTankSpawned) {
                mIsWaterTankSpawned = true;

                if(mIsSolarPowerSpawned) {
                    //unlock engineer
                    ColonyController.instance.unitPaletteController.ForceShowUnit(engineer);
                    ColonyController.instance.unitPaletteController.IncreaseCapacity(2);

                    StartCoroutine(dlgWaterSolarPlaced.Play());
                }
                else {
                    //StartCoroutine(dlgWaterTankPlaced.Play());
                }
            }
        }
        else if(structure.data == solarPower) {
            if(!mIsSolarPowerSpawned) {
                mIsSolarPowerSpawned = true;

                if(mIsWaterTankSpawned) {
                    //unlock engineer
                    ColonyController.instance.unitPaletteController.ForceShowUnit(engineer);
                    ColonyController.instance.unitPaletteController.IncreaseCapacity(2);

                    StartCoroutine(dlgWaterSolarPlaced.Play());
                }
                else {
                    //StartCoroutine(dlgSolarPanelPlaced.Play());
                }
            }
        }
    }
}
