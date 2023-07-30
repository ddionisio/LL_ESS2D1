using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitEngineer : Unit {
    private Structure mTargetStructure;
    private bool mTargetIsWorkAdded;

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
                if(mTargetStructure) {
                    mTargetStructure.WorkAdd();

                    if(mTargetStructure.state == StructureState.Active || mTargetStructure.state == StructureState.Destroyed)
                        mTargetStructure.state = StructureState.Repair;

                    mTargetIsWorkAdded = true;
                }
                break;
        }
    }

    protected override void ClearAIState() {
        ClearTarget();
    }

    protected override void UpdateAI() {
        switch(state) {
            case UnitState.Idle:
                if(mTargetStructure) { //have a target?
                    //check if it's still valid
                    if(CanGotoAndWorkOnStructure(mTargetStructure)) {
                        var wp = mTargetStructure.GetWaypointUnmarkedClosest(GameData.structureWaypointWork, position.x);
                        MoveTo(wp, false); //move to it
                    }
                    else
                        ClearTarget();
                }
                else { //look for work
                    if(!RefreshAndMoveToNewTarget()) {
                        if(stateTimeElapsed >= GameData.instance.unitIdleWanderDelay) //wander
                            MoveToOwnerStructure(false);
                    }
                }
                break;

            case UnitState.Move:
                //check if structure is still workable
                if(mTargetStructure) {
                    if(!CanWorkOnStructure(mTargetStructure)) {
                        ClearTarget();

                        //find a new one
                        if(!RefreshAndMoveToNewTarget()) //return to base
                            MoveToOwnerStructure(false);
                    }
                }
                else {
                    //look for work
                    RefreshAndMoveToNewTarget();
                }
                //other things
                break;

            case UnitState.Act:
                //simply check if it's still workable, if not, then we are done
                if(mTargetStructure) {
                    if(CanWorkOnStructure(mTargetStructure)) {
                        if(mTargetStructure.state == StructureState.Destroyed) //it got destroyed while we're fixing it
                            mTargetStructure.state = StructureState.Repair;
                    }
                    else
                        MoveToOwnerStructure(false);
                }
                else
                    MoveToOwnerStructure(false);
                break;
        }
    }

    protected override void MoveToComplete() {
        //check if we can still work on the target
        if(mTargetStructure) {
            if(CanWorkOnStructure(mTargetStructure)) {
                //are we at the structure?
                if(IsTouchingStructure(mTargetStructure)) {
                    state = UnitState.Act;
                    return;
                } //move to structure again
            } //can no longer work on structure
        } //no target

        //back to idle to re-evaluate our decisions
        base.MoveToComplete();
    }

    private void ClearTarget() {
        if(mTargetStructure) {
            if(mTargetStructure.state != StructureState.None) {
                //remove from work
                if(mTargetIsWorkAdded)
                    mTargetStructure.WorkRemove();
            }

            mTargetStructure = null;
        }

        mTargetIsWorkAdded = false;
    }

    private bool RefreshAndMoveToNewTarget() {
        if(mTargetStructure) //fail-safe, shouldn't exist when calling this
            ClearTarget();

        var structureCtrl = ColonyController.instance.structurePaletteController;

        Structure construct = null;
        float constructDist = 0f;
        Structure destroyed = null;
        float destroyedDist = 0f;
        Structure repair = null;
        float repairDist = 0f;

        var structureActives = structureCtrl.structureActives;
        for(int i = 0; i < structureActives.Count; i++) {
            var structure = structureActives[i];

            //check if it's viable
            if(CanGotoAndWorkOnStructure(structure)) {
                var dist = Mathf.Abs(structure.position.x - position.x);

                //priorities: construction, destroyed, repairable
                if(structure.state == StructureState.Construction) {
                    if(!construct || dist < constructDist) {
                        construct = structure;
                        constructDist = dist;
                    }
                }
                else if(structure.state == StructureState.Destroyed) {
                    if(!destroyed || dist < destroyedDist) {
                        destroyed = structure;
                        destroyedDist = dist;
                    }
                }
                else if(!repair || dist < repairDist) {
                    repair = structure;
                    repairDist = dist;
                }
            }
        }

        //setup actual target
        if(construct)
            mTargetStructure = construct;
        else if(destroyed)
            mTargetStructure = destroyed;
        else
            mTargetStructure = repair;

        if(mTargetStructure) {
            var wp = mTargetStructure.GetWaypointUnmarkedClosest(GameData.structureWaypointWork, position.x);
            MoveTo(wp, false); //move to it

            return true;
        }

        return false;
    }

    private bool CanWorkOnStructure(Structure structure) {
        return structure.canEngineer && !structure.workIsFull;
    }

    private bool CanGotoAndWorkOnStructure(Structure structure) {
        if(CanWorkOnStructure(structure)) {
            //check if all work waypoint is marked (this means someone else is on the way)
            var unmarkedWorkWp = structure.GetWaypointUnmarkedClosest(GameData.structureWaypointWork, position.x);

            return unmarkedWorkWp != null;
        }

        return false;
    }
}
