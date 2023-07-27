using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour, M8.IPoolInit, M8.IPoolSpawn, M8.IPoolSpawnComplete, M8.IPoolDespawn {
    [Header("Display")]
    public Transform root;

    [Header("Animations")]
    public M8.Animator.Animate animator;

    [M8.Animator.TakeSelector]
    public string takeSpawn;
    [M8.Animator.TakeSelector]
    public string takeIdle;
    [M8.Animator.TakeSelector]
    public string takeMove;
    [M8.Animator.TakeSelector]
    public string takeRun;
    [M8.Animator.TakeSelector]
    public string takeAct;
    [M8.Animator.TakeSelector]
    public string takeHurt;
    [M8.Animator.TakeSelector]
    public string takeDying;
    [M8.Animator.TakeSelector]
    public string takeDeath;
    [M8.Animator.TakeSelector]
    public string takeDespawn;

    public UnitData data { get; private set; }

    public UnitState state {
        get { return mState; }

        set {
            if(mState != value) {
                ClearCurrentState();

                mState = value;

                stateTimeLastChanged = Time.time;

                if(CanUpdateAI())
                    mUpdateAICurTime = 0f;

                ApplyCurrentState();

                stateChangedCallback?.Invoke(this);
            }
        }
    }

    public int hitpointsCurrent {
        get { return mCurHitpoints; }
        set {
            var val = Mathf.Clamp(value, 0, hitpointsMax);
            if(mCurHitpoints != val) {
                if(val < mCurHitpoints && !isDamageable) //prevent actual damage
                    return;

                var prevHitpoints = mCurHitpoints;
                mCurHitpoints = val;

                if(mCurHitpoints > prevHitpoints) { //healed?
                    //heal fx?

                    if(state == UnitState.Dying)
                        state = UnitState.Idle;
                }
                else if(mCurHitpoints == 0) { //dead?
                    if(data.canRevive)
                        state = UnitState.Dying;
                    else
                        state = UnitState.Death;
                }
                else if(mCurHitpoints < prevHitpoints) { //perform damage
                    state = UnitState.Hurt;
                }

                //signal
            }
        }
    }

    public int hitpointsMax { get { return data.hitpoints; } }

    public bool isDamageable { get { return data.hitpoints > 0 && (state == UnitState.Idle || state == UnitState.Move || state == UnitState.Act); } }

    public bool isMovable {
        get {
            return moveCtrl && !moveCtrl.isLocked;
        }
    }

    public Vector2 position {
        get { return transform.position; }
        set { transform.position = value; }
    }

    public Vector2 up {
        get { return transform.up; }
        set { transform.up = value; }
    }

    public BoxCollider2D boxCollider { get; private set; }

    public M8.PoolDataController poolCtrl { get; private set; }
    public MovableBase moveCtrl { get; private set; }

    public Structure ownerStructure { get; private set; }

    /// <summary>
    /// Elapsed time since this state
    /// </summary>
    public float stateTimeElapsed { get { return Time.time - stateTimeLastChanged; } }

    /// <summary>
    /// Last time since state change
    /// </summary>
    public float stateTimeLastChanged { get; private set; }

    /// <summary>
    /// Current waypoint while moving
    /// </summary>
    public Waypoint moveWaypoint { get; private set; }

    public event System.Action<Unit> stateChangedCallback;

    protected Coroutine mRout;

    private UnitState mState;

    private int mCurHitpoints;

    protected int mTakeSpawnInd = -1;
    protected int mTakeIdleInd = -1;
    protected int mTakeMoveInd = -1;
    protected int mTakeRunInd = -1;
    protected int mTakeActInd = -1;
    protected int mTakeHurtInd = -1;
    protected int mTakeDyingInd = -1;
    protected int mTakeDeathInd = -1;
    protected int mTakeDespawnInd = -1;

    private int mTakeCurMoveInd;

    private float mUpdateAICurTime;

    public bool IsTouching(Unit otherUnit) {
        if(!(boxCollider && otherUnit.boxCollider)) return false;

        return boxCollider.bounds.Intersects(otherUnit.boxCollider.bounds);
    }

    public bool IsTouchingStructure(Structure structure) {
        if(!(boxCollider && structure.boxCollider)) return false;

        return boxCollider.bounds.Intersects(structure.boxCollider.bounds);
    }

    public bool MoveTo(Vector2 toPos, bool isRun) {
        if(!isMovable) return false;

        if(moveWaypoint != null) { //remove previous waypoint
            moveWaypoint.RemoveMark();
            moveWaypoint = null;
        }

        MoveApply(toPos, isRun);

        return true;
    }

    public bool MoveTo(Waypoint toWaypoint, bool isRun) {
        if(!isMovable) return false;

        if(moveWaypoint != null) //remove previous waypoint
            moveWaypoint.RemoveMark();

        moveWaypoint = toWaypoint;
        moveWaypoint.AddMark();

        MoveApply(moveWaypoint.point, isRun);

        return true;
    }

    public bool MoveTo(Structure structure, string waypointName, bool checkMarked, bool isRun) {
        var wp = structure.GetWaypointRandom(waypointName, checkMarked);
        if(wp != null)
            return MoveTo(wp, isRun);
        else //fail-safe, just move at structure's position
            return MoveTo(structure.position, isRun);
    }

    public bool MoveToOwnerStructure(bool isRun) {
        if(!(isMovable && ownerStructure)) return false;

        if(moveWaypoint != null) //remove previous waypoint
            moveWaypoint.RemoveMark();

        moveWaypoint = ownerStructure.GetWaypointRandom(GameData.structureWaypointIdle, true);
        if(moveWaypoint != null) {
            moveWaypoint.AddMark();

            MoveApply(moveWaypoint.point, isRun);
        }
        else //just move to structure's position
            MoveApply(ownerStructure.position, isRun);

        return true;
    }

    public void Despawn() {
        if(state == UnitState.Despawning || state == UnitState.None) //already despawing, or is released
            return;

        state = UnitState.Despawning;
    }

    protected virtual void Init() { }

    protected virtual void Despawned() { }

    protected virtual void Spawned() { }
                
    protected virtual void ClearCurrentState() {
        StopCurrentRout();

        if(moveCtrl) moveCtrl.Cancel();

        switch(mState) {
            case UnitState.Move:
                mTakeCurMoveInd = -1;

                if(moveWaypoint != null) {
                    moveWaypoint.RemoveMark();
                    moveWaypoint = null;
                }
                break;
        }
    }

    protected virtual void ApplyCurrentState() {
        var physicsActive = true;

        switch(mState) {
            case UnitState.Spawning:
                AnimateToState(mTakeSpawnInd, UnitState.Idle);

                physicsActive = false;
                break;

            case UnitState.Idle:
                if(mTakeIdleInd != -1)
                    animator.Play(mTakeIdleInd);
                break;

            case UnitState.Move:
                mRout = StartCoroutine(DoMove());
                break;

            case UnitState.Act:
                if(mTakeActInd != -1)
                    animator.Play(mTakeActInd);
                break;

            case UnitState.Hurt:
                mRout = StartCoroutine(DoHurt());

                physicsActive = false;
                break;

            case UnitState.Dying:
                mRout = StartCoroutine(DoDying());

                physicsActive = false;

                GameData.instance.signalUnitDying?.Invoke(this);
                break;

            case UnitState.Death:
                AnimateToRelease(mTakeDeathInd);

                physicsActive = false;
                break;

            case UnitState.Despawning:
                AnimateToRelease(mTakeDespawnInd);

                physicsActive = false;
                break;

            case UnitState.Retreat:
                //TODO
                break;

            case UnitState.RetreatToBase:
                //TODO
                break;

            case UnitState.None:
                if(animator)
                    animator.Stop();

                mCurHitpoints = 0;

                up = Vector2.up;

                physicsActive = false;
                break;
        }

        if(!CanUpdateAI())
            ClearAIState();

        if(boxCollider)
            boxCollider.enabled = physicsActive;
    }

    protected virtual bool CanUpdateAI() {
        return mState == UnitState.Idle || mState == UnitState.Move || mState == UnitState.Act;
    }

    protected virtual void ClearAIState() { }

    protected virtual void UpdateAI() { }

    protected virtual void Update() {
        //AI update
        if(CanUpdateAI()) {
            mUpdateAICurTime += Time.deltaTime;
            if(mUpdateAICurTime >= GameData.instance.unitUpdateAIDelay) {
                mUpdateAICurTime = 0f;
                UpdateAI();
            }
        }
    }

    protected virtual void MoveToComplete() {
        state = UnitState.Idle;
    }

    protected void AnimateToState(int takeInd, UnitState toState) {
        mRout = StartCoroutine(DoAnimationToState(takeInd, toState));
    }

    protected void AnimateToRelease(int takeInd) {
        mRout = StartCoroutine(DoAnimationToRelease(takeInd));
    }
        
    void M8.IPoolInit.OnInit() {
        poolCtrl = GetComponent<M8.PoolDataController>();

        moveCtrl = GetComponent<MovableBase>();

        boxCollider = GetComponent<BoxCollider2D>();
        if(boxCollider)
            boxCollider.enabled = false;

        //initialize animations
        if(animator) {
            mTakeSpawnInd = animator.GetTakeIndex(takeSpawn);
            mTakeIdleInd = animator.GetTakeIndex(takeIdle);
            mTakeMoveInd = animator.GetTakeIndex(takeMove);
            mTakeRunInd = animator.GetTakeIndex(takeRun);
            mTakeActInd = animator.GetTakeIndex(takeAct);
            mTakeHurtInd = animator.GetTakeIndex(takeHurt);
            mTakeDyingInd = animator.GetTakeIndex(takeDying);
            mTakeDeathInd = animator.GetTakeIndex(takeDeath);
            mTakeDespawnInd = animator.GetTakeIndex(takeDespawn);
        }

        //initial states
        mState = UnitState.None;

        Init();
    }

    void M8.IPoolSpawn.OnSpawned(M8.GenericParams parms) {
        if(parms != null) {
            if(parms.ContainsKey(UnitSpawnParams.data))
                data = parms.GetValue<UnitData>(UnitSpawnParams.data);

            if(parms.ContainsKey(UnitSpawnParams.structureOwner))
                ownerStructure = parms.GetValue<Structure>(UnitSpawnParams.structureOwner);

            if(parms.ContainsKey(UnitSpawnParams.spawnPoint))
                position = parms.GetValue<Vector2>(UnitSpawnParams.spawnPoint);
        }

        //set initial states
        mCurHitpoints = hitpointsMax;
        stateTimeLastChanged = Time.time;

        Spawned();
    }

    void M8.IPoolSpawnComplete.OnSpawnComplete() {
        state = UnitState.Spawning;
    }

    void M8.IPoolDespawn.OnDespawned() {
        state = UnitState.None;
        
        Despawned();

        data = null;
        ownerStructure = null;
    }

    IEnumerator DoMove() {
        yield return null;

        if(mTakeCurMoveInd != -1)
            animator.Play(mTakeCurMoveInd);

        moveCtrl.Move();

        while(moveCtrl.isMoving)
            yield return null;

        mRout = null;

        MoveToComplete();
    }

    IEnumerator DoHurt() {
        if(mTakeHurtInd != -1)
            animator.Play(mTakeHurtInd);

        var delay = GameData.instance.unitHurtDelay;
        if(delay > 0f) {
            var curTime = 0f;
            while(curTime < delay) {
                yield return null;

                curTime += Time.deltaTime;
            }
        }
        else
            yield return null;

        mRout = null;

        state = UnitState.Idle;
    }

    IEnumerator DoDying() {
        if(mTakeDyingInd != -1)
            animator.Play(mTakeDyingInd);

        var delay = GameData.instance.unitDyingDelay;
        if(delay > 0f) {
            var curTime = 0f;
            while(curTime < delay) {
                yield return null;

                curTime += Time.deltaTime;
            }
        }
        else
            yield return null;

        mRout = null;

        state = UnitState.Death;
    }

    IEnumerator DoAnimationToState(int takeInd, UnitState toState) {
        if(takeInd != -1)
            yield return animator.PlayWait(takeInd);
        else
            yield return null;

        mRout = null;

        state = toState;
    }

    IEnumerator DoAnimationToRelease(int takeInd) {
        if(takeInd != -1)
            yield return animator.PlayWait(takeInd);
        else
            yield return null;

        mRout = null;

        poolCtrl.Release();
    }

    private void MoveApply(Vector2 toPos, bool isRun) {
        if(isRun) {
            mTakeCurMoveInd = mTakeRunInd;

            moveCtrl.moveSpeed = data.runSpeed;
        }
        else {
            mTakeCurMoveInd = mTakeMoveInd;

            moveCtrl.moveSpeed = data.moveSpeed;
        }

        moveCtrl.moveDestination = toPos;

        state = UnitState.Move;
    }

    private void StopCurrentRout() {
        if(mRout != null) {
            StopCoroutine(mRout);
            mRout = null;
        }
    }
}
