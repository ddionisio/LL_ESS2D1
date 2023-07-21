using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitGardener : Unit {
    [Header("Gardener Info")]
    public StructureData[] targetPlantStructures;

    private StructurePlant mTargetPlant;
    private bool mTargetPlantIsWorkAdded;

    protected override void Despawned() {
        ClearAIState();
    }

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
                    if(CanWorkOnPlant(mTargetPlant, true)) {
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
                    if(!CanWorkOnPlant(mTargetPlant, false)) {
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
            if(mTargetPlant.growthState == StructurePlant.GrowthState.Growing && !mTargetPlant.workIsFull)
                state = UnitState.Act;
            else //back to idle, find a new plant from there
                base.MoveToComplete();
        }
        else //nothing
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

        for(int i = 0; i < targetPlantStructures.Length; i++) {
            mTargetPlant = FindNearestPlantGrowing(targetPlantStructures[i]);
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

    private StructurePlant FindNearestPlantGrowing(StructureData structureData) {
        StructurePlant ret = null;
        float dist = 0f;

        var structureCtrl = ColonyController.instance.structurePaletteController;

        var activeList = structureCtrl.GetStructureActives(structureData);
        if(activeList != null) {
            var x = position.x;

            for(int i = 0; i < activeList.Count; i++) {
                var plant = activeList[i] as StructurePlant;
                if(CanWorkOnPlant(plant, true)) {
                    //see if it's closer to our current available plant, or it's the first one
                    var plantDist = Mathf.Abs(plant.position.x - x);
                    if(!ret || plantDist < dist) {
                        ret = plant;
                        dist = plantDist;
                    }
                }
            }
        }

        return ret;
    }

    private bool CanWorkOnPlant(StructurePlant plant, bool checkWorkWaypoint) {
        if(plant && plant.growthState == StructurePlant.GrowthState.Growing && !plant.workIsFull) {
            if(checkWorkWaypoint) {
                //check if all work waypoint is marked (this means someone else is on the way)
                var unmarkedWorkWp = plant.GetWaypointUnmarked(GameData.structureWaypointWork);

                return unmarkedWorkWp != null;
            }
            else
                return true;
        }

        return false;
    }
}
