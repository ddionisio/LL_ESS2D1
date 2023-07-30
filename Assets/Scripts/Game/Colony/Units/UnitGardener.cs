using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitGardener : Unit {
    [Header("Gardener Animations")]
    public string takeAttack;

    private Unit mTargetEnemy;

    private StructurePlant mTargetPlant;
    private bool mTargetPlantIsWorkAdded;

    private int mTakeAttackInd = -1;

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

    protected override int GetActTakeIndex() {
        return mTargetEnemy ? mTakeAttackInd : base.GetActTakeIndex();
    }

    protected override void ClearAIState() {
        ClearTargetPlant();
        ClearTargetEnemy();
    }

    protected override void UpdateAI() {
        switch(state) {
            case UnitState.Idle:
                if(mTargetEnemy) { //have target enemy?
                    if(mTargetEnemy.hitpointsCurrent == 0) //no longer valid, wait for new target
                        ClearTargetEnemy();
                    else if(IsTouching(mTargetEnemy))
                        state = UnitState.Act;
                    else //keep moving towards it
                        MoveTo(mTargetEnemy.position, false);
                }
                else if(mTargetPlant) { //have a target plant?
                    //check if it's still valid
                    if(CanGotoAndWorkOnPlant(mTargetPlant)) {
                        var wp = mTargetPlant.GetWaypointUnmarkedClosest(GameData.structureWaypointWork, position.x);
                        MoveTo(wp, false); //move to it
                    }
                    else
                        ClearTargetPlant();
                }
                else if(!RefreshAndMoveToNewTarget()) { //look for new target
                    if(stateTimeElapsed >= GameData.instance.unitIdleWanderDelay) //wander
                        MoveToOwnerStructure(false);
                }
                break;

            case UnitState.Move:
                if(mTargetEnemy) { //check if target is still killable
                    if(mTargetEnemy.hitpointsCurrent == 0) {
                        ClearTargetEnemy();

                        //find a new target
                        if(!RefreshAndMoveToNewTarget()) //return to base
                            MoveToOwnerStructure(false);
                    }
                    else if(IsTouching(mTargetEnemy))
                        state = UnitState.Act;
                }
                else if(mTargetPlant) { //check if the plant is still valid
                    if(!CanWorkOnPlant(mTargetPlant)) {
                        ClearTargetPlant();

                        //find a new one
                        if(!RefreshAndMoveToNewTarget()) //return to base
                            MoveToOwnerStructure(false);
                    }
                }
                else //look for plant to help grow
                    RefreshAndMoveToNewTarget();
                break;

            case UnitState.Act:                
                if(mTargetEnemy) { //target still alive?
                    if(mTargetEnemy.hitpointsCurrent > 0) {
                        mTargetEnemy.hitpointsCurrent--;
                        return;
                    }
                }                
                else if(mTargetPlant) { //plant still growing?
                    if(mTargetPlant.growthState == StructurePlant.GrowthState.Growing)
                        return;
                }

                MoveToOwnerStructure(false);
                break;
        }
    }

    protected override void MoveToComplete() {
        //check if we can still kill target
        if(mTargetEnemy) {
            if(mTargetEnemy.hitpointsCurrent > 0) {
                //are we in contact?
                if(IsTouching(mTargetEnemy)) {
                    state = UnitState.Act;
                    return;
                }
            }
        }
        //check if we can still work on the plant
        else if(mTargetPlant) {
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

    protected override void Init() {
        if(animator)
            mTakeAttackInd = animator.GetTakeIndex(takeAttack);
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

    private void ClearTargetEnemy() {
        if(mTargetEnemy) {
            mTargetEnemy.RemoveMark();
            mTargetEnemy = null;
        }
    }

    private bool RefreshAndMoveToNewTarget() {
        if(mTargetPlant) //fail-safe, shouldn't exist when calling this
            ClearTargetPlant();
        if(mTargetEnemy)
            ClearTargetEnemy();

        var gardenerDat = data as UnitGardenerData;

        var colonyCtrl = ColonyController.instance;

        //check for enemies
        var unitCtrl = colonyCtrl.unitController;

        var targetEnemies = gardenerDat.targetDestroy;

        for(int i = 0; i < targetEnemies.Length; i++) {
            mTargetEnemy = unitCtrl.GetUnitNearestActiveByData<Unit>(position.x, targetEnemies[i], CanTargetEnemy);
            if(mTargetEnemy)
                break;
        }

        if(mTargetEnemy) {
            mTargetEnemy.AddMark();
            MoveTo(mTargetEnemy.position, false);

            return true;
        }

        //check for plants
        var structureCtrl = colonyCtrl.structurePaletteController;
        
        var targetPlantStructures = gardenerDat.targetPlantStructures;

        for(int i = 0; i < targetPlantStructures.Length; i++) {
            mTargetPlant = structureCtrl.GetStructureNearestActive<StructurePlant>(position.x, targetPlantStructures[i], CanGotoAndWorkOnPlant);
            if(mTargetPlant)
                break;
        }

        if(mTargetPlant) {
            var wp = mTargetPlant.GetWaypointUnmarkedClosest(GameData.structureWaypointWork, position.x);
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
            var unmarkedWorkWp = plant.GetWaypointUnmarkedClosest(GameData.structureWaypointWork, position.x);

            return unmarkedWorkWp != null;
        }

        return false;
    }

    private bool CanTargetEnemy(Unit unit) {
        return unit.markCount < 1 && unit.hitpointsCurrent > 0;
    }
}
