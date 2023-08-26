using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitHunter : Unit {
    [Header("Hunter Attack Info")]
    public float attackRange = 3f;
    public float attackJumpOffMinRange = 2f;
    public float attackJumpDelay = 0.5f;
    public float attackJumpHeight = 1.5f;

    [Header("Hunter Animations")]
    [M8.Animator.TakeSelector]
    public int takeAttackJump = -1;
    [M8.Animator.TakeSelector]
    public int takeAttackHit = -1;

    public override bool canSwim { get { return true; } }

    private Unit mTargetUnit;

    protected override void SpawnComplete() {
        if(moveCtrl) moveCtrl.isLocked = false;

        MoveRoam();
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
                if(boxCollider) boxCollider.enabled = false;

                mRout = StartCoroutine(DoAttack());
                break;
        }
    }

    protected override void UpdateAI() {
        switch(state) {
            case UnitState.Idle:
                if(mTargetUnit) { //have target enemy?
                    if(mTargetUnit.hitpointsCurrent == 0) //no longer valid, wait for new target
                        ClearTargetUnit();
                    else if(IsUnitInAttackRange(mTargetUnit))
                        state = UnitState.Act;
                    else //keep moving towards it
                        MoveTo(mTargetUnit.position, false);
                }
                else if(!RefreshAndMoveToNewTarget()) { //find target
                    if(stateTimeElapsed >= GameData.instance.unitIdleWanderDelay)
                        MoveRoam();
                }
                break;

            case UnitState.Move:
                if(mTargetUnit) { //check if target is still killable
                    if(mTargetUnit.hitpointsCurrent == 0) {
                        ClearTargetUnit();

                        //find a new target
                        if(!RefreshAndMoveToNewTarget()) //return to base
                            MoveRoam();
                    }
                    else if(IsUnitInAttackRange(mTargetUnit))
                        state = UnitState.Act;
                }
                else //find target
                    RefreshAndMoveToNewTarget();
                break;
        }
    }

    protected override void ClearAIState() {
        ClearTargetUnit();
    }

    protected override void MoveToComplete() {
        //check if we can still kill target
        if(mTargetUnit) {
            if(mTargetUnit.hitpointsCurrent > 0) {
                //are we in range?
                if(IsUnitInAttackRange(mTargetUnit)) {
                    state = UnitState.Act;
                    return;
                }
            }
        }

        base.MoveToComplete();
    }

    IEnumerator DoAttack() {
        var attackDat = data as UnitAttackData;
        if(!attackDat) {
            mRout = null;
            yield break;
        }

        //face enemy
        facing = mTargetUnit.position.x - position.x < 0f ? MovableBase.Facing.Left : MovableBase.Facing.Right;

        while((stateTimeElapsed < attackDat.attackIdleDelay || !mTargetUnit.isDamageable) && mTargetUnit.hitpointsCurrent > 0)
            yield return null;
    
        yield return null;
                
        var lastPosition = position;

        up = Vector2.up;

        //check if target is still valid
        if(mTargetUnit.hitpointsCurrent > 0) {// && IsUnitInAttackRange(mTargetUnit)) {            
            RestartStateTime();
                        
            //face enemy
            facing = mTargetUnit.position.x - position.x < 0f ? MovableBase.Facing.Left : MovableBase.Facing.Right;

            //do jump
            if(takeAttackJump != -1)
                animator.Play(takeAttackJump);

            while(stateTimeElapsed < attackJumpDelay) {
                yield return null;

                var t = Mathf.Clamp01(stateTimeElapsed / attackJumpDelay);

                Vector2 targetPos;
                if(mTargetUnit.boxCollider) {
                    var targetBounds = mTargetUnit.boxCollider.bounds;
                    targetPos = new Vector2(targetBounds.center.x, targetBounds.max.y);
                }
                else
                    targetPos = mTargetUnit.position;

                position = new Vector2(Mathf.Lerp(lastPosition.x, targetPos.x, t), Mathf.Lerp(lastPosition.y, targetPos.y, t) + attackJumpHeight * Mathf.Sin(t * Mathf.PI));
            }

            //check again if still valid
            if(mTargetUnit.hitpointsCurrent > 0) {
                //do actual attack
                mTargetUnit.hitpointsCurrent--;

                if(takeAttackHit != -1)
                    yield return animator.PlayWait(takeAttackHit);
            }
        }
                
        //drop back down
        if(lastPosition != position) {
            RestartStateTime();

            if(takeMidAir != -1)
                animator.Play(takeMidAir);
            else if(takeAttackJump != -1)
                animator.Play(takeAttackJump);
                                
            var dist = Mathf.Abs(lastPosition.x - position.x);
            if(dist < attackJumpOffMinRange)
                dist = attackJumpOffMinRange;

            GroundPoint toGroundPt;
            switch(facing) {
                case MovableBase.Facing.Left:
                    GroundPoint.GetGroundPoint(position.x - dist, out toGroundPt);
                    break;
                default:
                    GroundPoint.GetGroundPoint(position.x + dist, out toGroundPt);
                    break;
            }

            lastPosition = position;
            var toPos = toGroundPt.position;
                
            while(stateTimeElapsed < attackJumpDelay) {
                yield return null;

                var t = Mathf.Clamp01(stateTimeElapsed / attackJumpDelay);

                position = new Vector2(Mathf.Lerp(lastPosition.x, toPos.x, t), Mathf.Lerp(lastPosition.y, toPos.y, t) + attackJumpHeight * Mathf.Sin(t * Mathf.PI));
            }
        }

        MoveRoam();
    }

    private void MoveRoam() {
        var activeStructures = ColonyController.instance.structurePaletteController.structureActives;
        var ind = Random.Range(0, activeStructures.Count);

        MoveTo(activeStructures[ind].position, false);
    }

    private void ClearTargetUnit() {
        if(mTargetUnit) {
            mTargetUnit.RemoveMark();
            mTargetUnit = null;
        }
    }

    private bool RefreshAndMoveToNewTarget() {
        if(mTargetUnit) //fail-safe, shouldn't exist when calling this
            ClearTargetUnit();

        if(isSwimming || ColonyController.instance.cycleController.isHazzard) //can't do anything if we are swimming or there's hazzard
            return false;

        var colonyCtrl = ColonyController.instance;

        //check for enemies
        var unitCtrl = colonyCtrl.unitController;

        mTargetUnit = unitCtrl.GetUnitEnemyNearestActive<Unit>(position.x, CanTargetEnemy);
        if(mTargetUnit) {
            mTargetUnit.AddMark();
            MoveTo(mTargetUnit.position, false);

            return true;
        }

        return false;
    }

    private bool IsUnitInAttackRange(Unit unit) {
        if(unit) {
            var distX = Mathf.Abs(position.x - unit.position.x);
            return distX <= attackRange;
        }

        return false;
    }

    private bool CanTargetEnemy(Unit unit) {
        var attackDat = data as UnitAttackData;
        if(!attackDat)
            return false;

        return attackDat.CanAttackUnit(unit) && unit.markCount < 1;
    }
}
