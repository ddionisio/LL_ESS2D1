using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitEngineer : Unit {
    [Header("Engineer Animations")]
    [Tooltip("Ensure this is not in loop.")]
    [M8.Animator.TakeSelector]
    public int takeAttack = -1;

    private Unit mTargetEnemySpawner;

    private Structure mTargetStructure;
    private bool mTargetIsWorkAdded;

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
        return mTargetEnemySpawner ? -1 : base.GetActTakeIndex();
    }

    protected override void ClearAIState() {
        ClearTargetStructure();
        ClearTargetEnemy();
    }

    protected override void UpdateAI() {
        switch(state) {
            case UnitState.Idle:
                if(mTargetEnemySpawner) { //have target enemy?
                    if(mTargetEnemySpawner.hitpointsCurrent == 0) //no longer valid, wait for new target
                        ClearTargetEnemy();
                    else if(IsTouching(mTargetEnemySpawner))
                        state = UnitState.Act;
                    else //keep moving towards it
                        MoveTo(mTargetEnemySpawner.position, false);
                }
                else if(mTargetStructure) { //have a target?
                    //check if it's still valid
                    if(CanGotoAndWorkOnStructure(mTargetStructure)) {
                        var wp = mTargetStructure.GetWaypointUnmarkedClosest(GameData.structureWaypointWork, position.x);
                        MoveTo(wp, false); //move to it
                    }
                    else
                        ClearTargetStructure();
                }
                else { //look for work
                    if(!RefreshAndMoveToNewTarget()) {
                        if(stateTimeElapsed >= GameData.instance.unitIdleWanderDelay) //wander
                            MoveToOwnerStructure(false);
                    }
                }
                break;

            case UnitState.Move:
                if(mTargetEnemySpawner) { //check if target is still killable
                    if(mTargetEnemySpawner.hitpointsCurrent == 0) {
                        ClearTargetEnemy();

                        //find a new target
                        if(!RefreshAndMoveToNewTarget()) //return to base
                            MoveToOwnerStructure(false);
                    }
                    else if(IsTouching(mTargetEnemySpawner))
                        state = UnitState.Act;
                }
                else if(mTargetStructure) { //check if structure is still workable
                    if(!CanWorkOnStructure(mTargetStructure)) {
                        ClearTargetStructure();

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
        }
    }

    protected override void MoveToComplete() {
        //check if we can still kill target
        if(mTargetEnemySpawner) {
            if(mTargetEnemySpawner.hitpointsCurrent > 0) {
                //are we in contact?
                if(IsTouching(mTargetEnemySpawner)) {
                    state = UnitState.Act;
                    return;
                }
            }
        }
        //check if we can still work on the target
        else if(mTargetStructure) {
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

    IEnumerator DoAct() {
        if(mTargetEnemySpawner) {
            yield return null;

            while(mTargetEnemySpawner.hitpointsCurrent > 0) {
                if(takeAttack != -1)
                    yield return animator.PlayWait(takeAttack);

                if(mTargetEnemySpawner.isDamageable)
                    mTargetEnemySpawner.hitpointsCurrent--;

                yield return null;
            }
        }
        else if(mTargetStructure) { //simply check if it's still workable, if not, then we are done
            mTargetStructure.WorkAdd();
            mTargetIsWorkAdded = true;

            if(mTargetStructure.state == StructureState.Active || mTargetStructure.state == StructureState.Destroyed)
                mTargetStructure.state = StructureState.Repair;

            yield return null;

            while(canWork && mTargetStructure.canEngineer) {
                if(mTargetStructure.state == StructureState.Destroyed) //it got destroyed while we're fixing it
                    mTargetStructure.state = StructureState.Repair;

                yield return null;
            }
        }

        mRout = null;
        MoveToOwnerStructure(false);
    }

    private void ClearTargetStructure() {
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

    private void ClearTargetEnemy() {
        if(mTargetEnemySpawner) {
            mTargetEnemySpawner.RemoveMark();
            mTargetEnemySpawner = null;
        }
    }

    private bool RefreshAndMoveToNewTarget() {
        if(mTargetStructure) //fail-safe, shouldn't exist when calling this
            ClearTargetStructure();
        if(mTargetEnemySpawner)
            ClearTargetEnemy();

        if(isSwimming || ColonyController.instance.cycleController.isHazzard) //can't do anything if we are swimming or there's hazzard
            return false;

        var colonyCtrl = ColonyController.instance;

        //check for enemy unit spawners
        var unitCtrl = colonyCtrl.unitController;

        mTargetEnemySpawner = unitCtrl.GetUnitEnemyNearestActive<Unit>(position.x, CanTargetEnemy);
        if(mTargetEnemySpawner) {
            mTargetEnemySpawner.AddMark();
            MoveTo(mTargetEnemySpawner.position, false);

            return true;
        }

        //check for structures to repair
        if(!canWork)
            return false;

        var structureCtrl = colonyCtrl.structurePaletteController;

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
        return canWork && structure.canEngineer && !structure.workIsFull;
    }

    private bool CanGotoAndWorkOnStructure(Structure structure) {
        if(CanWorkOnStructure(structure)) {
            //check if there is enough unmarked waypoints to work
            int markWpCount = structure.GetWaypointMarkCount(GameData.structureWaypointWork);
            return markWpCount < structure.workCapacity;
        }

        return false;
    }

    private bool CanTargetEnemy(Unit unit) {
        //we only target spawners
        return unit.data is UnitSpawnerData && unit.markCount < 1 && unit.hitpointsCurrent > 0;
    }
}
