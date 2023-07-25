using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitGardener : Unit {

    private StructurePlant mTargetPlant;
    private bool mTargetPlantIsWorkAdded;

    protected override void ClearCurrentState() {
        base.ClearCurrentState();

        switch(state) {
            case UnitState.Act:
                ClearAIState();
                break;
        }
    }

    protected override void ApplyCurrentState() {
        base.ApplyCurrentState();

        switch(state) {
            case UnitState.Act:
                //work on the plant
                if(mTargetPlant) {
                    mTargetPlant.WorkAdd();
                    mTargetPlantIsWorkAdded = true;
                }
                break;
        }
    }

    protected override void ClearAIState() {
        ClearTargetPlant();
    }

    protected override void UpdateAI() {
        switch(state) {
            case UnitState.Idle:
                if(mTargetPlant) { //have a target plant?
                    //check if it's still valid
                    if(CanGotoAndWorkOnPlant(mTargetPlant)) {
                        var wp = mTargetPlant.GetWaypointUnmarked(GameData.structureWaypointWork);
                        MoveTo(wp, false); //move to it
                    }
                    else
                        ClearTargetPlant();
                }
                else { //look for plant to help grow
                    if(!RefreshAndMoveToNewTarget()) {
                        if(stateTimeElapsed >= GameData.instance.unitIdleWanderDelay) //wander
                            MoveToOwnerStructure(false);
                    }
                }
                break;

            case UnitState.Move:
                //check if the plant is still valid
                if(mTargetPlant) {
                    if(!CanWorkOnPlant(mTargetPlant)) {
                        ClearTargetPlant();

                        //find a new one
                        if(!RefreshAndMoveToNewTarget()) //return to base
                            MoveToOwnerStructure(false);
                    }
                }
                else {
                    //look for plant to help grow
                    RefreshAndMoveToNewTarget();
                }
                //other things
                break;

            case UnitState.Act:
                //finished growing? move back to base
                if(!mTargetPlant || mTargetPlant.growthState != StructurePlant.GrowthState.Growing)
                    MoveToOwnerStructure(false);
                break;
        }
    }

    protected override void MoveToComplete() {
        //check if we can still work on the plant
        if(mTargetPlant) {
            if(CanWorkOnPlant(mTargetPlant)) {
                //are we at the plant?
                if(IsTouchingStructure(mTargetPlant)) {
                    state = UnitState.Act;
                    return;
                } //move to plant again
            } //can no longer work on plant
        } //no target

        //back to idle to re-evaluate our decisions
        base.MoveToComplete();
    }
        
    private void ClearTargetPlant() {
        if(mTargetPlant) {
            if(mTargetPlant.state != StructureState.None) {
                //remove from work
                if(mTargetPlantIsWorkAdded)
                    mTargetPlant.WorkRemove();
            }

            mTargetPlant = null;
        }

        mTargetPlantIsWorkAdded = false;
    }

    private bool RefreshAndMoveToNewTarget() {
        if(mTargetPlant) //fail-safe, shouldn't exist when calling this
            ClearTargetPlant();

        var structureCtrl = ColonyController.instance.structurePaletteController;

        var gardenerDat = data as UnitGardenerData;
        var targetPlantStructures = gardenerDat.targetPlantStructures;

        for(int i = 0; i < targetPlantStructures.Length; i++) {
            mTargetPlant = structureCtrl.GetStructureNearestActive<StructurePlant>(position.x, targetPlantStructures[i], CanGotoAndWorkOnPlant);
            if(mTargetPlant)
                break;
        }

        if(mTargetPlant) {
            var wp = mTargetPlant.GetWaypointUnmarked(GameData.structureWaypointWork);
            MoveTo(wp, false); //move to it

            return true;
        }

        return false;
    }

    private bool CanWorkOnPlant(StructurePlant plant) {
        return plant.growthState == StructurePlant.GrowthState.Growing && !plant.workIsFull;
    }

    private bool CanGotoAndWorkOnPlant(StructurePlant plant) {
        if(CanWorkOnPlant(plant)) {
            //check if all work waypoint is marked (this means someone else is on the way)
            var unmarkedWorkWp = plant.GetWaypointUnmarked(GameData.structureWaypointWork);

            return unmarkedWorkWp != null;
        }

        return false;
    }
}
