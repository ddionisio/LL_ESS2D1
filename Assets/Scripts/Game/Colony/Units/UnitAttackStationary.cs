using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitAttackStationary : Unit {
    [Header("Attack Info")]
    public Bounds attackBounds;
    public int attackCheckCapacity = 4;

    public Vector2 attackAreaCenter { get { return transform.position + attackBounds.center; } }
    public Vector2 attackAreaSize { get { return attackBounds.size; } }

    private Collider2D[] mAttackCheckColls;

    protected override void ApplyCurrentState() {
        base.ApplyCurrentState();

        switch(state) {
            case UnitState.Idle:
                mRout = StartCoroutine(DoIdle());
                break;

            case UnitState.Act:
                mRout = StartCoroutine(DoAttack());
                break;
        }
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

        state = UnitState.Act;
    }

    IEnumerator DoAttack() {
        var attackDat = data as UnitAttackData;
        if(!attackDat) {
            mRout = null;
            yield break;
        }

        yield return null;

        //wait for attack animation to end
        if(mTakeActInd != -1) {
            while(animator.isPlaying)
                yield return null;
        }

        var gameDat = GameData.instance;

        LayerMask lookupLayerMask = gameDat.unitLayerMask;

        if(attackDat.canAttackStructure)
            lookupLayerMask.value |= gameDat.structureLayerMask.value;

        var hitCount = Physics2D.OverlapBoxNonAlloc(attackAreaCenter, attackAreaSize, 0f, mAttackCheckColls, lookupLayerMask);
        for(int i = 0; i < hitCount; i++) {
            var coll = mAttackCheckColls[i];
            if(coll == boxCollider)
                continue;

            var go = coll.gameObject;

            if((1<< go.layer) == gameDat.unitLayerMask) { //is unit?
                if(attackDat.CheckUnitTag(go)) { //check tag
                    var unit = coll.GetComponent<Unit>();
                    if(unit && attackDat.CanAttackUnit(unit)) //check damage eligibility and immunity
                        unit.hitpointsCurrent -= attackDat.attackDamage;
                }
            }
            else if((1 << go.layer) == gameDat.structureLayerMask) {
                var structure = coll.GetComponent<Structure>();
                if(structure && structure.isDamageable)
                    structure.hitpointsCurrent -= attackDat.attackDamage;
            }
        }

        mRout = null;

        state = UnitState.Idle;
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.red;

        Gizmos.DrawWireCube(attackAreaCenter, attackAreaSize);
    }
}
