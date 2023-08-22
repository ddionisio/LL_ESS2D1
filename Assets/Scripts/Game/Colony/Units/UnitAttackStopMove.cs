using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Move to a distant, wait, and then attack; despawn after reaching end of screen.
/// </summary>
public class UnitAttackStopMove : Unit {
    [Header("Move Info")]
    public bool moveIsGrounded;
    public M8.RangeFloat moveDistanceRange;

    [Header("Attack Info")]
    public Bounds attackBounds;
    public int attackCheckCapacity = 4;

    public Vector2 attackAreaCenter { get { return transform.position + attackBounds.center; } }
    public Vector2 attackAreaSize { get { return attackBounds.size; } }

    private DirType mMoveDir;

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

    protected override void Spawned(M8.GenericParams parms) {
        mMoveDir = DirType.None;

        if(parms != null) {
            if(parms.ContainsKey(UnitSpawnParams.moveDirType))
                mMoveDir = parms.GetValue<DirType>(UnitSpawnParams.moveDirType);
        }
    }

    protected override void Init() {
        mAttackCheckColls = new Collider2D[attackCheckCapacity];
    }

    protected override void UpdateAI() {
        switch(state) {
            case UnitState.Move:
                if(isOffscreen)
                    Despawn();
                break;
        }
    }

    protected override void MoveToComplete() {
        //offscreen?
        if(isOffscreen)
            Despawn();
        else
            state = UnitState.Act;
    }

    IEnumerator DoIdle() {
        var attackDat = data as UnitAttackData;
        if(!attackDat) {
            mRout = null;
            yield break;
        }

        var isGrounded = true; //for jumping units

        do {
            yield return null;

            if(moveIsGrounded)
                isGrounded = FallDown();

        } while(!isGrounded || stateTimeElapsed < attackDat.attackIdleDelay);

        mRout = null;

        //move
        var toPos = position;
        var dist = moveDistanceRange.random;

        switch(mMoveDir) {
            case DirType.Up:
                toPos.y += dist;
                break;
            case DirType.Down:
                toPos.y -= dist;
                break;
            case DirType.Left:
                toPos.x -= dist;
                break;
            case DirType.Right:
                toPos.x += dist;
                break;
        }

        if(moveIsGrounded) {
            GroundPoint groundPos;
            if(GroundPoint.GetGroundPoint(toPos.x, out groundPos))
                toPos = groundPos.position;
        }

        MoveTo(toPos, false);
    }

    IEnumerator DoAttack() {
        var attackDat = data as UnitAttackData;
        if(!attackDat) {
            mRout = null;
            yield break;
        }

        yield return null;

        //attack within the area
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

            if((1 << go.layer) == gameDat.unitLayerMask) { //is unit?
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

        //wait for attack animation to end
        if(takeAct != -1) {
            while(animator.isPlaying)
                yield return null;
        }

        mRout = null;

        state = UnitState.Idle;
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.red;

        Gizmos.DrawWireCube(attackAreaCenter, attackAreaSize);
    }
}
