using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour, M8.IPoolInit, M8.IPoolSpawn, M8.IPoolSpawnComplete, M8.IPoolDespawn {
    [Header("Display")]
    public Transform root;
    public SpriteRenderer spriteRender;
    public SpriteRenderer spriteOverlayRender;

    [Header("Animations")]
    public M8.Animator.Animate animator;

    [M8.Animator.TakeSelector]
    public int takeSpawn = -1;
    [M8.Animator.TakeSelector]
    public int takeIdle = -1;
    [M8.Animator.TakeSelector]
    public int takeMove = -1;
    [M8.Animator.TakeSelector]
    public int takeRun = -1;
    [M8.Animator.TakeSelector]
    public int takeMidAir = -1;
    [M8.Animator.TakeSelector]
    public int takeSwim = -1;
    [M8.Animator.TakeSelector]
    public int takeAct = -1;
    [M8.Animator.TakeSelector]
    public int takeHurt = -1;
    [M8.Animator.TakeSelector]
    public int takeDying = -1;
    [M8.Animator.TakeSelector]
    public int takeDeath = -1;
    [M8.Animator.TakeSelector]
    public int takeDespawn = -1;
    [M8.Animator.TakeSelector]
    public int takeVictory = -1;

    public UnitData data { get; private set; }

    public UnitState state {
        get { return mState; }

        set {
            if(mState != value) {
                ClearCurrentState();

                mState = value;

                RestartStateTime();

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

                HitpointsChanged(prevHitpoints);

                //signal                
            }
        }
    }

    public int hitpointsMax { get { return data.hitpoints; } }

    public bool isDamageable { get { return data.hitpoints > 0 && (state == UnitState.Idle || state == UnitState.Move || state == UnitState.Act || state == UnitState.Retreat || state == UnitState.RetreatToBase); } }

    public bool isMovable {
        get {
            return moveCtrl && !moveCtrl.isLocked;
        }
    }

    public bool isVictoryEnabled { get { return takeVictory != -1; } }

    public virtual bool canSwim { get { return false; } }

    public bool isSwimming { get; private set; }

    public Vector2 position {
        get { return transform.position; }
        set { transform.position = value; }
    }

    public Vector2 up {
        get { return transform.up; }
        set { transform.up = value; }
    }

    public MovableBase.Facing facing {
        get { return mFacing; }
        set {
            if(mFacing != value) {
                mFacing = value;

                if(root) {
                    var s = root.localScale;

                    switch(mFacing) {
                        case MovableBase.Facing.Left:
                            s.x = -Mathf.Abs(s.x);
                            break;

                        default:
                            s.x = Mathf.Abs(s.x);
                            break;
                    }

                    root.localScale = s;
                }
            }
        }
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

    /// <summary>
    /// Check if this unit has been marked (used for targeting by AI)
    /// </summary>
    public int markCount { get { return mMark; } }

    public bool isOffscreen {
        get {
            var colonyCtrl = ColonyController.instance;
            var screenExt = colonyCtrl.mainCamera2D.screenExtent;

            var bounds = boxCollider ? boxCollider.bounds : new Bounds(position, Vector2.one);

            return bounds.max.x < screenExt.min.x || bounds.min.x > screenExt.max.x || bounds.max.y < screenExt.min.y || bounds.min.y > screenExt.max.y;
        }
    }

    public event System.Action<Unit> stateChangedCallback;

    protected Coroutine mRout;

    private UnitState mState;

    private int mCurHitpoints;

    private int mTakeCurMoveInd;

    private float mUpdateAICurTime;

    private MovableBase.Facing mFacing = MovableBase.Facing.None;

    private int mMark;

    public void AddMark() {
        mMark++;
    }

    public void RemoveMark() {
        if(mMark > 0)
            mMark--;
    }

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

        state = UnitState.Move;

        return true;
    }

    public bool MoveTo(Waypoint toWaypoint, bool isRun) {
        if(!isMovable) return false;

        if(moveWaypoint != null) //remove previous waypoint
            moveWaypoint.RemoveMark();

        moveWaypoint = toWaypoint;
        moveWaypoint.AddMark();

        MoveApply(moveWaypoint.point, isRun);

        state = UnitState.Move;

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

        ApplyMoveToOwnerStructure(isRun);

        state = UnitState.Move;

        return true;
    }

    private void ApplyMoveToOwnerStructure(bool isRun) {
        if(moveWaypoint != null) //remove previous waypoint
            moveWaypoint.RemoveMark();

        moveWaypoint = ownerStructure.GetWaypointRandom(GameData.structureWaypointIdle, true);
        if(moveWaypoint != null) {
            moveWaypoint.AddMark();

            MoveApply(moveWaypoint.point, isRun);
        }
        else //just move to structure's position
            MoveApply(ownerStructure.position, isRun);
    }

    /// <summary>
    /// Move unit's y-position until it hits ground. Assumes pivot is at bottom. Returns true if grounded
    /// </summary>
    public bool FallDown() {
        var pos = position;

        float dist = GameData.instance.unitFallSpeed * Time.deltaTime;
        var hit = Physics2D.Raycast(pos, Vector2.down, dist, GameData.instance.groundLayerMask);
        if(hit.collider) {
            position = hit.point;

            return true;
        }
        else {
            pos.y -= dist;
            position = pos;

            return false;
        }
    }

    public void RetreatFrom(Vector2 sourcePoint) {
        if(!isMovable) return;

        Vector2 retreatPos;

        var dposX = position.x - sourcePoint.x;
        if(dposX < 0f)
            retreatPos = new Vector2(position.x - GameData.instance.unitRetreatDistanceRange.random, position.y);
        else
            retreatPos = new Vector2(position.x + GameData.instance.unitRetreatDistanceRange.random, position.y);

        MoveApply(retreatPos, true);

        state = UnitState.Retreat;
    }

    public void Despawn() {
        if(state == UnitState.Despawning || state == UnitState.Death || state == UnitState.None) //already despawing/death, or is released
            return;

        state = UnitState.Despawning;
    }

    protected void RestartStateTime() {
        stateTimeLastChanged = Time.time;
    }

    protected virtual void Init() { }

    protected virtual void Despawned() { }

    protected virtual void Spawned(M8.GenericParams parms) { }

    /// <summary>
    /// Called after spawn animation finished during Spawning state
    /// </summary>
    protected virtual void SpawnComplete() { 
        state = UnitState.Idle; 
    }

    protected virtual void ClearCurrentState() {
        StopCurrentRout();

        if(moveCtrl) moveCtrl.Cancel();

        //reset sprite render
        if(spriteRender) {
            spriteRender.gameObject.SetActive(true);
            spriteRender.transform.localPosition = Vector3.zero;
            spriteRender.transform.localScale = Vector3.one;
            spriteRender.transform.localRotation = Quaternion.identity;
            spriteRender.color = Color.white;
            spriteRender.flipX = false;
            spriteRender.flipY = false;
        }

        if(spriteOverlayRender) {
            spriteOverlayRender.gameObject.SetActive(false);
            spriteOverlayRender.color = Color.white;
        }

        switch(mState) {
            case UnitState.Move:
                if(moveWaypoint != null) {
                    moveWaypoint.RemoveMark();
                    moveWaypoint = null;
                }
                break;
        }
    }

    protected virtual void ApplyCurrentState() {
        if(!CanUpdateAI())
            ClearAIState();

        switch(mState) {
            case UnitState.Spawning:
                ApplyTelemetryState(false, false);

                mRout = StartCoroutine(_DoSpawn());
                break;

            case UnitState.Idle:
                if(isSwimming) {
                    if(takeSwim != -1)
                        animator.Play(takeSwim);
                }
                else {
                    if(takeIdle != -1)
                        animator.Play(takeIdle);
                }

                ApplyTelemetryState(true, true);
                break;

            case UnitState.Move:
            case UnitState.RetreatToBase:
                ApplyTelemetryState(true, true);

                mRout = StartCoroutine(DoMove());
                break;

            case UnitState.Act:
                ApplyTelemetryState(true, true);

                var actTakeInd = GetActTakeIndex();
                if(actTakeInd != -1)
                    animator.Play(actTakeInd);
                break;

            case UnitState.Hurt:
                ApplyTelemetryState(false, false);

                mRout = StartCoroutine(DoHurt());
                break;

            case UnitState.Dying:
                ApplyTelemetryState(false, false);

                mCurHitpoints = 0; //just in case

                mRout = StartCoroutine(DoDying());

                GameData.instance.signalUnitDying?.Invoke(this);
                break;

            case UnitState.Death:
                ApplyTelemetryState(false, false);

                mCurHitpoints = 0; //just in case

                AnimateToRelease(takeDeath);
                break;

            case UnitState.Despawning:
                ApplyTelemetryState(false, false);

                mCurHitpoints = 0;

                AnimateToRelease(takeDespawn);
                break;

            case UnitState.Retreat:
                ApplyTelemetryState(true, true);

                mRout = StartCoroutine(DoMove());
                break;

            case UnitState.BounceToBase:
                ApplyTelemetryState(true, false);

                mRout = StartCoroutine(DoBounceToBase());
                break;

            case UnitState.Victory:
                ApplyTelemetryState(false, false);

                mRout = StartCoroutine(DoVictory());
                break;

            case UnitState.None:
                if(animator)
                    animator.Stop();

                mCurHitpoints = 0;

                up = Vector2.up;
                facing = MovableBase.Facing.None;

                ApplyTelemetryState(false, false);

                mMark = 0;
                break;
        }
    }

    protected void ApplyTelemetryState(bool canMove, bool physicsActive) {
        if(moveCtrl)
            moveCtrl.isLocked = !canMove;

        if(boxCollider)
            boxCollider.enabled = physicsActive;
    }

    protected virtual int GetActTakeIndex() {
        return takeAct;
    }

    protected virtual bool CanUpdateAI() {
        return mState == UnitState.Idle || mState == UnitState.Move || mState == UnitState.Act || mState == UnitState.Retreat;
    }

    protected virtual void ClearAIState() { }

    protected virtual void UpdateAI() { }

    protected virtual void HitpointsChanged(int previousHitpoints) {
        if(mCurHitpoints > previousHitpoints) { //healed?
            //heal fx?

            if(state == UnitState.Dying) //revived?
                state = UnitState.Idle;
        }
        else if(mCurHitpoints == 0) { //dead?
            if(data.canRevive)
                state = UnitState.Dying;
            else
                state = UnitState.Death;
        }
        else if(mCurHitpoints < previousHitpoints) { //perform damage
            state = UnitState.Hurt;
        }
    }

    protected virtual void Update() {
        //AI update
        if(CanUpdateAI()) {
            mUpdateAICurTime += Time.deltaTime;
            if(mUpdateAICurTime >= GameData.instance.unitUpdateAIDelay) {
                mUpdateAICurTime = 0f;
                UpdateAI();
            }

            //check if we are off-screen and we have an owner structure
            if(ownerStructure) {
                //bounce back to base
                if(isOffscreen) {
                    state = UnitState.BounceToBase;
                }
                //check if there's hazzard, retreat if we have an owner base
                else if(ColonyController.instance.cycleController.isHazzard && ColonyController.instance.cycleController.isHazzardRetreat) {
                    ApplyMoveToOwnerStructure(true);
                    state = UnitState.RetreatToBase;
                }
            }
        }

        if(!(state == UnitState.Move 
            || state == UnitState.Retreat 
            || state == UnitState.BounceToBase 
            || state == UnitState.RetreatToBase 
            || state == UnitState.Spawning 
            || state == UnitState.Hurt 
            || state == UnitState.Despawning 
            || state == UnitState.Victory)) {
            //check if we are on water
            var levelBounds = ColonyController.instance.bounds;

            var checkPoint = new Vector2(position.x, levelBounds.max.y);
            var checkDir = Vector2.down;

            var hit = Physics2D.Raycast(checkPoint, checkDir, levelBounds.size.y, GameData.instance.groundLayerMask | GameData.instance.waterLayerMask);
            if(hit.collider && ((1 << hit.collider.gameObject.layer) & GameData.instance.waterLayerMask) != 0) {
                if(canSwim) {
                    position = hit.point;

                    if(!isSwimming) {
                        isSwimming = true;
                        state = UnitState.Idle;
                    }
                }
                else //just despawn if we can't swim
                    state = UnitState.Despawning;
            }
            else {
                if(isSwimming) {
                    position = hit.point;

                    isSwimming = false;

                    if(takeIdle != -1)
                        animator.Play(takeIdle);
                }
            }
        }
    }

    protected virtual void MoveToComplete() {
        if(state == UnitState.RetreatToBase) {
            if(ownerStructure is StructureColonyShip)
                ((StructureColonyShip)ownerStructure).AddUnitHazzardRetreat(data);

            Despawn();
        }
        else
            state = UnitState.Idle;
    }

    protected void AnimateToState(int takeInd, UnitState toState) {
        mRout = StartCoroutine(DoAnimationToState(takeInd, toState));
    }

    protected void AnimateToRelease(int takeInd) {
        mRout = StartCoroutine(DoAnimationToRelease(takeInd));
    }

    protected Vector2 GetScreenOutsidePosition(DirType dir) {
        var colonyCtrl = ColonyController.instance;
        var screenExt = colonyCtrl.mainCamera2D.screenExtent;

        Vector2 ofs, size;

        if(boxCollider) {
            ofs = boxCollider.offset;
            size = boxCollider.size;
        }
        else {
            ofs = Vector2.zero;
            size = Vector2.zero;
        }
        
        switch(dir) {
            case DirType.Up:
                return new Vector2(position.x, screenExt.max.y + ofs.y + size.y * 0.5f);

            case DirType.Down:
                return new Vector2(position.x, screenExt.min.y - ofs.y - size.y * 0.5f);

            case DirType.Left:
                return new Vector2(screenExt.min.x - ofs.x - size.x * 0.5f, position.y);

            case DirType.Right:
                return new Vector2(screenExt.max.x + ofs.x + size.x * 0.5f, position.y);

            default:
                return position;
        }
    }

    void M8.IPoolInit.OnInit() {
        poolCtrl = GetComponent<M8.PoolDataController>();

        moveCtrl = GetComponent<MovableBase>();

        boxCollider = GetComponent<BoxCollider2D>();
        if(boxCollider)
            boxCollider.enabled = false;

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
        mCurHitpoints = data.hitpointStartApply ? data.hitpointStart : data.hitpoints;
        stateTimeLastChanged = Time.time;

        //setup signals
        if(GameData.instance.signalVictory) GameData.instance.signalVictory.callback += OnVictory;

        Spawned(parms);
    }

    void M8.IPoolSpawnComplete.OnSpawnComplete() {
        state = UnitState.Spawning;
    }

    void M8.IPoolDespawn.OnDespawned() {
        if(GameData.instance.signalVictory) GameData.instance.signalVictory.callback -= OnVictory;

        state = UnitState.None;
        
        Despawned();

        data = null;
        ownerStructure = null;
    }

    void OnVictory() {
        if(isVictoryEnabled) {
            if(!(state == UnitState.Despawning || state == UnitState.Death || state == UnitState.None))
                state = UnitState.Victory;
        }
        else
            Despawn();
    }

    IEnumerator _DoSpawn() {
        if(takeSpawn != -1)
            yield return animator.PlayWait(takeSpawn);
        else
            yield return null;

        mRout = null;

        SpawnComplete();
    }

    IEnumerator DoMove() {
        yield return null;

        if(isSwimming) {
            if(takeSwim != -1)
                animator.Play(takeSwim);
        }
        else {
            if(mTakeCurMoveInd != -1)
                animator.Play(mTakeCurMoveInd);
        }

        moveCtrl.Move();

        while(moveCtrl.isMoving) {
            //update facing
            facing = moveCtrl.facing;

            yield return null;

            if(moveCtrl.isWater) {
                if(!isSwimming) {
                    isSwimming = true;

                    if(takeSwim != -1)
                        animator.Play(takeSwim);
                }
            }
            else {
                if(isSwimming) {
                    isSwimming = false;

                    if(mTakeCurMoveInd != -1)
                        animator.Play(mTakeCurMoveInd);
                }
            }
        }

        mRout = null;

        MoveToComplete();
    }

    IEnumerator DoBounceToBase() {
        var gameDat = GameData.instance;

        up = Vector2.up;

        if(takeMidAir != -1)
            animator.Play(takeMidAir);

        var dposX = ownerStructure.position.x - position.x;

        facing = dposX < 0f ? MovableBase.Facing.Left : MovableBase.Facing.Right;

        var startPos = position;

        Vector2 endPos;

        var wp = ownerStructure.GetWaypointRandom(GameData.structureWaypointSpawn, false);
        if(wp != null)
            endPos = wp.groundPoint.position;
        else
            endPos = ownerStructure.position;

        var height = gameDat.unitBounceToBaseHeightRange.random;

        while(stateTimeElapsed < gameDat.unitBounceToBaseDelay) {
            yield return null;

            var t = Mathf.Clamp01(stateTimeElapsed / gameDat.unitBounceToBaseDelay);

            position = new Vector2(Mathf.Lerp(startPos.x, endPos.x, t), Mathf.Lerp(startPos.y, endPos.y, t) + height * Mathf.Sin(t * Mathf.PI));
        }

        mRout = null;

        state = UnitState.Idle;
    }

    IEnumerator DoHurt() {
        if(takeHurt != -1)
            animator.Play(takeHurt);

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
        if(takeDying != -1)
            animator.Play(takeDying);

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

    IEnumerator DoVictory() {
        //fail-safe
        if(!FallDown()) {
            if(takeMidAir != -1)
                animator.Play(takeMidAir);

            while(!FallDown())
                yield return null;
        }

        if(takeIdle != -1)
            animator.Play(takeIdle);

        var gameDat = GameData.instance;

        while(true) {
            var curTime = 0f;
            var delay = gameDat.unitVictoryWaitDelayRange.random;

            while(curTime < delay) {
                yield return null;
                curTime += Time.deltaTime;
            }

            if(takeVictory != -1)
                yield return animator.PlayWait(takeVictory);
        }
    }

    private void MoveApply(Vector2 toPos, bool isRun) {
        if(isRun) {
            mTakeCurMoveInd = takeRun;

            moveCtrl.moveSpeed = data.runSpeed;
        }
        else {
            mTakeCurMoveInd = takeMove;

            moveCtrl.moveSpeed = data.moveSpeed;
        }

        moveCtrl.moveDestination = toPos;
    }

    private void StopCurrentRout() {
        if(mRout != null) {
            StopCoroutine(mRout);
            mRout = null;
        }
    }
}
