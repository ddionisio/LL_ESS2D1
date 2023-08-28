using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColonySequence02 : ColonySequenceBase {
    [Header("Enemy Data")]
    public UnitData mushroom;
    public UnitData fly;
    public UnitData hopper;

    private bool mIsMushroomSpawned;
    private bool mIsFlySpawned;
    private bool mIsHopperSpawned;

    private bool mIsHazzardHappened;

    public override void Init() {
        GameData.instance.signalUnitSpawned.callback += OnUnitSpawned;
    }

    public override void Deinit() {
        GameData.instance.signalUnitSpawned.callback -= OnUnitSpawned;
    }

    public override IEnumerator Intro() {
        Debug.Log("Dialog about tropical climate.");
        yield return null;
    }

    public override void CycleNext() {
        if(ColonyController.instance.cycleController.cycleCurWeather.isHazzard) {
            if(!mIsHazzardHappened) {
                mIsHazzardHappened = true;

                Debug.Log("Dialog about hazzard.");
            }
        }
    }

    void OnUnitSpawned(Unit unit) {
        if(unit.data == mushroom) {
            if(!mIsMushroomSpawned) {
                mIsMushroomSpawned = true;

                Debug.Log("Dialog about mushroom.");
            }
        }
        else if(unit.data == fly) {
            if(!mIsFlySpawned) {
                mIsFlySpawned = true;

                Debug.Log("Dialog about fly.");
            }
        }
        else if(unit.data == hopper) {
            if(!mIsHopperSpawned) {
                mIsHopperSpawned = true;

                Debug.Log("Dialog about hopper.");
            }
        }
    }
}
