using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitMedic : Unit {
    public Unit targetUnit { 
        get { return mTargetUnit; }
        set {
            if(mTargetUnit != value) {
                mTargetUnit = value;

                if(mTargetUnit && (state == UnitState.Idle || state == UnitState.Move))
                    MoveTo(mTargetUnit.position, false);
            }
        }
    }

    private Unit mTargetUnit;

    protected override void ApplyCurrentState() {
        base.ApplyCurrentState();

        switch(state) {
            case UnitState.Act:
                mRout = StartCoroutine(DoAct());                
                break;
        }
    }

    protected override void ClearAIState() {
        mTargetUnit = null;
    }

    protected override void UpdateAI() {
        switch(state) {
            case UnitState.Idle:
                if(mTargetUnit) {
                    if(CanWorkOnUnit(mTargetUnit))
                        MoveTo(mTargetUnit.position, false);
                    else {
                        mTargetUnit = null;
                        MoveToOwnerStructure(false);
                    }
                }
                else
                    MoveToOwnerStructure(false);
                break;

            case UnitState.Move:
                if(mTargetUnit) {
                    if(CanWorkOnUnit(mTargetUnit)) {
                        if(IsTouching(mTargetUnit)) //already in contact, start proceedure
                            state = UnitState.Act;
                    }
                    else { //no longer in need of medic, return to base
                        mTargetUnit = null;
                        MoveToOwnerStructure(false);
                    }
                }
                break;
        }
    }

    protected override void MoveToComplete() {
        //check to see if target is still dying
        if(mTargetUnit) {
            if(CanWorkOnUnit(mTargetUnit)) {
                //make sure we are in contact
                if(IsTouching(mTargetUnit)) {
                    state = UnitState.Act;
                    return;
                }
            }
            else
                mTargetUnit = null;
        }
        else { //despawn if we are at our base
            if(IsTouchingStructure(ownerStructure)) {
                Despawn();
                return;
            }
        }

        base.MoveToComplete();
    }

    IEnumerator DoAct() {
        if(mTakeActInd != -1) {
            while(animator.isPlaying)
                yield return null;
        }
        else
            yield return null;

        if(mTargetUnit) {
            if(CanWorkOnUnit(mTargetUnit))
                mTargetUnit.hitpointsCurrent = mTargetUnit.hitpointsMax;

            mTargetUnit = null;
        }

        mRout = null;

        MoveToOwnerStructure(false);
    }

    private bool CanWorkOnUnit(Unit unit) {
        return unit.state == UnitState.Dying;
    }
}
