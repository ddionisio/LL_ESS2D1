using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitGardener : Unit {
    [Header("Gardener Animations")]
    [Tooltip("Ensure this is not in loop.")]
    [M8.Animator.TakeSelector]
    public int takeAttack = -1;

    private Unit mTargetEnemy;

    private StructurePlant mTargetPlant;
    private bool mTargetPlantIsWorkAdded;

    public override bool canSwim { get { return true; } }

    public bool canWork { get { return ColonyController.instance.cycleAllowProgress && !ColonyController.instance.cycleController.isHazzard && !isSwimming; } }

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
                mRout = StartCoroutine(DoAct());
                break;
        }
    }

    protected override int GetActTakeIndex() {
        return mTargetEnemy ? -1 : base.GetActTakeIndex();
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

    IEnumerator DoAct() {
        yield return null;

        if(mTargetEnemy) { //target still alive?            
            while(mTargetEnemy.hitpointsCurrent > 0) {
                if(takeAttack != -1)
                    yield return animator.PlayWait(takeAttack);

                mTargetEnemy.hitpointsCurrent--;

                yield return null;
            }
        }
        else if(mTargetPlant) {
            mTargetPlant.WorkAdd();
            mTargetPlantIsWorkAdded = true;

            while(canWork && mTargetPlant.growthState == StructurePlant.GrowthState.Growing) //wait for plant to stop growing
                yield return null;

            yield return null;
        }

        mRout = null;

        MoveToOwnerStructure(false);
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

        if(isSwimming || ColonyController.instance.cycleController.isHazzard) //can't do anything if we are swimming or hazzard
            return false;

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
        if(!canWork)
            return false;

        var structureCtrl = colonyCtrl.structurePaletteController;
        
        var targetPlantStructures = GameData.instance.structureFoodSources;

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
        return canWork && plant.growthState == StructurePlant.GrowthState.Growing && !plant.workIsFull;
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
