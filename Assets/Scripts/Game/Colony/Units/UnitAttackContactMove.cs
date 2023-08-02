using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Move and attack on contact, despawn after reaching end of screen.
/// </summary>
public class UnitAttackContactMove : Unit {    
    [Header("Attack Info")]
    public int attackCheckCapacity = 4;

    private Vector2 mMoveDest;
    private Collider2D[] mAttackCheckColls;

    private Structure mLastStructureAttacked; //prevent continuous attack on one structure

    protected override void ApplyCurrentState() {
        base.ApplyCurrentState();

        switch(state) {
            case UnitState.Idle:
                mRout = StartCoroutine(DoIdle());
                break;

            case UnitState.Act:
                mRout = StartCoroutine(DoAttack());
                break;

            case UnitState.None:
                mLastStructureAttacked = null;
                break;
        }
    }

    protected override void UpdateAI() {
        switch(state) {
            case UnitState.Move:
                if(CheckContactAndAttack())
                    state = UnitState.Act;
                break;
        }
    }

    protected override void MoveToComplete() {
        Despawn();
    }

    protected override void Spawned(M8.GenericParams parms) {
        var moveDir = DirType.None;

        if(parms != null) {
            if(parms.ContainsKey(UnitSpawnParams.moveDirType)) {
                moveDir = parms.GetValue<DirType>(UnitSpawnParams.moveDirType);                
            }
        }

        mMoveDest = GetScreenOutsidePosition(moveDir);
    }

    protected override void Init() {
        mAttackCheckColls = new Collider2D[attackCheckCapacity];
    }

    IEnumerator DoIdle() {
        var attackDat = data as UnitAttackData;
        if(!attackDat) {
            mRout = null;
            yield break;
        }

        do {
            yield return null;
        } while(stateTimeElapsed < attackDat.attackIdleDelay);

        mRout = null;

        MoveTo(mMoveDest, false);
    }

    IEnumerator DoAttack() {
        yield return null;

        //wait for attack animation to end
        if(mTakeActInd != -1) {
            while(animator.isPlaying)
                yield return null;
        }

        mRout = null;

        state = UnitState.Idle;
    }

    private bool CheckContactAndAttack() {
        var attackDat = data as UnitAttackData;
        if(!attackDat)
            return false;

        var gameDat = GameData.instance;

        LayerMask lookupLayerMask = gameDat.unitLayerMask;

        if(attackDat.canAttackStructure)
            lookupLayerMask.value |= gameDat.structureLayerMask.value;

        Vector2 checkPos, checkSize;

        if(boxCollider) {
            checkPos = position + boxCollider.offset;
            checkSize = boxCollider.size;
        }
        else { //fail-safe
            checkPos = position;
            checkSize = Vector2.one;
        }

        var attackCount = 0;

        var hitCount = Physics2D.OverlapBoxNonAlloc(checkPos, checkSize, 0f, mAttackCheckColls, lookupLayerMask);
        for(int i = 0; i < hitCount; i++) {
            var coll = mAttackCheckColls[i];
            if(coll == boxCollider)
                continue;

            var go = coll.gameObject;

            if((1 << go.layer) == gameDat.unitLayerMask) { //is unit?
                if(attackDat.CheckUnitTag(go)) { //check tag
                    var unit = coll.GetComponent<Unit>();
                    if(unit && attackDat.CanAttackUnit(unit)) { //check damage eligibility and immunity
                        unit.hitpointsCurrent -= attackDat.attackDamage;
                        attackCount++;
                    }
                }
            }
            else if((1 << go.layer) == gameDat.structureLayerMask) {
                if(!mLastStructureAttacked || mLastStructureAttacked.boxCollider != coll) { //prevent continuous attack on a structure
                    var structure = coll.GetComponent<Structure>();
                    if(structure && structure.isDamageable) {
                        structure.hitpointsCurrent -= attackDat.attackDamage;

                        mLastStructureAttacked = structure;

                        attackCount++;
                    }
                }
            }
        }

        return attackCount > 0;
    }
}
