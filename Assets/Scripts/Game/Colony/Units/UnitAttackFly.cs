using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitAttackFly : Unit {
    [Header("Fly Info")]
    public float idleDelay = 0.3f;

    public float despawnMoveDelay = 1f;
    public DG.Tweening.Ease despawnMoveEase = DG.Tweening.Ease.InSine;

    private bool mIsMovingLeft;

    private StructurePlant mPlantTarget;

    private RaycastHit2D[] mCheckHits = new RaycastHit2D[4];

    private DG.Tweening.EaseFunction mFlyWaitMoveEaseFunc;
    private DG.Tweening.EaseFunction mFlyGrabEaseFunc;
    private DG.Tweening.EaseFunction mFlyDespawnMoveEaseFunc;

    private float mSpawnPositionY;

    protected override void ClearCurrentState() {
        base.ClearCurrentState();

        switch(state) {
            case UnitState.Act:
                ClearAIState();
                break;
        }
    }

    protected override void ApplyCurrentState() {
        switch(state) {
            case UnitState.Idle:
                base.ApplyCurrentState();
                mRout = StartCoroutine(DoIdle());
                break;

            case UnitState.Act:
                base.ApplyCurrentState();
                mRout = StartCoroutine(DoAct());
                break;

            case UnitState.Despawning: //override despawn state
                ApplyTelemetryState(false, false);

                mRout = StartCoroutine(DoDespawn());
                break;

            case UnitState.None:
                base.ApplyCurrentState();
                mFlyWaitMoveEaseFunc = null;
                mFlyGrabEaseFunc = null;
                break;

            default:
                base.ApplyCurrentState();
                break;
        }
    }

    protected override void ClearAIState() {
        if(mPlantTarget) {
            mPlantTarget.RemoveMark();
            mPlantTarget = null;            
        }
    }

    protected override void UpdateAI() {
        switch(state) {
            case UnitState.Move:
                //look for plant below
                var colonyCtrl = ColonyController.instance;

                var boxSize = boxCollider ? boxCollider.size : Vector2.one;
                var checkCount = Physics2D.BoxCastNonAlloc(position, boxSize, 0f, Vector2.down, mCheckHits, colonyCtrl.bounds.extents.y, GameData.instance.structureLayerMask);
                for(int i = 0; i < checkCount; i++) {
                    var hit = mCheckHits[i];
                    if(hit.collider) {
                        var structure = hit.collider.GetComponent<Structure>();
                        if(structure is StructurePlant) {
                            var plant = (StructurePlant)structure;

                            //make sure there's a spot for us to grab blooms
                            //and it's not in a ruined state
                            if(plant.markCount < plant.hitpointsMax && !(plant.state == StructureState.None || plant.state == StructureState.Destroyed || plant.state == StructureState.Demolish)) {
                                if(mPlantTarget) //fail-safe
                                    mPlantTarget.RemoveMark();

                                //target acquired, start action
                                mPlantTarget = plant;
                                mPlantTarget.AddMark();
                                state = UnitState.Act;
                                return;
                            }
                        }
                    }
                }
                break;
        }
    }

    protected override void MoveToComplete() {
        mIsMovingLeft = !mIsMovingLeft;

        base.MoveToComplete();
    }

    protected override void Spawned(M8.GenericParams parms) {

        DirType dirType = DirType.Left;

        if(parms != null) {
            if(parms.ContainsKey(UnitSpawnParams.moveDirType))
                dirType = parms.GetValue<DirType>(UnitSpawnParams.moveDirType);
        }

        switch(dirType) {
            case DirType.Left:
                mIsMovingLeft = true;
                break;
            default:
                mIsMovingLeft = false;
                break;
        }

        mSpawnPositionY = position.y;
    }

    protected override void Init() {
        
    }

    IEnumerator DoIdle() {
        while(stateTimeElapsed < idleDelay)
            yield return null;

        mRout = null;

        var levelBounds = ColonyController.instance.bounds;

        if(mIsMovingLeft)
            MoveTo(new Vector2(levelBounds.min.x, mSpawnPositionY), false);
        else
            MoveTo(new Vector2(levelBounds.max.x, mSpawnPositionY), false);
    }

    IEnumerator DoAct() {
        var flyDat = data as UnitAttackFlyData;
        if(!flyDat) {
            mRout = null;
            yield break;
        }

        if(mFlyWaitMoveEaseFunc == null) mFlyWaitMoveEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(flyDat.flyWaitMoveEase);
        if(mFlyGrabEaseFunc == null) mFlyGrabEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(flyDat.flyGrabEase);

        yield return null;

        var plantBounds = mPlantTarget.boxCollider.bounds;

        var roamWP = mPlantTarget.GetWaypointRandom(GameData.structureWaypointRoam, false);

        Vector2 roamCenter;
        if(roamWP != null)
            roamCenter = roamWP.point;
        else
            roamCenter = new Vector2(plantBounds.center.x, plantBounds.max.y + flyDat.flyWaitRadius);

        Vector2 moveStart = position, moveEnd = position;
        bool isMove = false;
        bool isAttack = false;
        //int bloomInd = -1;

        while(mPlantTarget && !(mPlantTarget.state == StructureState.None || mPlantTarget.state == StructureState.Destroyed || mPlantTarget.state == StructureState.Demolish)) {
            yield return null;

            //grabbing?
            //if(bloomInd != -1) {
            if(isMove) {
                if(stateTimeElapsed < flyDat.flyWaitMoveDelay) {
                    var t = mFlyWaitMoveEaseFunc(stateTimeElapsed, flyDat.flyWaitMoveDelay, 0f, 0f);
                    position = Vector2.Lerp(moveStart, moveEnd, t);
                }
                else {
                    RestartStateTime();
                    isMove = false;
                }
            }
            else if(isAttack) {
                if(stateTimeElapsed < flyDat.flyGrabDelay) {
                    //moving towards target
                    var t = mFlyGrabEaseFunc(stateTimeElapsed, flyDat.flyGrabDelay, 0f, 0f);
                    position = Vector2.Lerp(moveStart, moveEnd, t);

                    //make sure it's still available
                    //if(!mPlantTarget.BloomIsAvailable(bloomInd)) {
                    if(!mPlantTarget.isDamageable) {
                        isAttack = false;

                        RestartStateTime();

                        moveEnd = roamCenter + Random.insideUnitCircle * flyDat.flyWaitRadius;
                        isMove = true;
                    }
                }
                else { //attack plant
                    //mPlantTarget.BloomClear(bloomInd);
                    if(mPlantTarget.isDamageable)
                        mPlantTarget.hitpointsCurrent--;

                    isAttack = false;

                    RestartStateTime();

                    moveStart = position;
                    moveEnd = roamCenter + Random.insideUnitCircle * flyDat.flyWaitRadius;

                    isMove = true;
                }
            }
            //check if bloom is available
            //else if(mPlantTarget.growthState == StructurePlant.GrowthState.Bloom && (bloomInd = mPlantTarget.BloomGrabAvailableIndex()) != -1) {
            else if(mPlantTarget.growthState == StructurePlant.GrowthState.Bloom) {
                RestartStateTime();

                moveStart = position;

                var wp = mPlantTarget.GetWaypointRandom(GameData.structureWaypointAttack, false);

                moveEnd = wp != null ? wp.point : mPlantTarget.position;
                //moveEnd = mPlantTarget.BloomPosition(bloomInd);

                isAttack = true;
            }
            else {
                if(stateTimeElapsed >= flyDat.flyWaitDelay) {
                    RestartStateTime();

                    moveStart = position;
                    moveEnd = roamCenter + Random.insideUnitCircle * flyDat.flyWaitRadius;

                    isMove = true;
                }
            }
        }

        //move to spawn position y
        moveStart = position;
        moveEnd = new Vector2(position.x, mSpawnPositionY);
        RestartStateTime();
        while(stateTimeElapsed < flyDat.flyGrabDelay) {
            yield return null;

            var t = mFlyGrabEaseFunc(stateTimeElapsed, flyDat.flyGrabDelay, 0f, 0f);
            position = Vector2.Lerp(moveStart, moveEnd, t);
        }

        mRout = null;

        state = UnitState.Idle;
    }

    IEnumerator DoDespawn() {
        yield return null;

        if(takeMove != -1)
            animator.Play(takeMove);

        if(mFlyDespawnMoveEaseFunc == null) mFlyDespawnMoveEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(despawnMoveEase);

        var screenRect = ColonyController.instance.mainCamera2D.screenExtent;

        var startPos = position;
        var offScreenPos = new Vector2(position.x, screenRect.max.y + boxCollider.size.y * 0.5f);

        while(stateTimeElapsed < despawnMoveDelay) {
            yield return null;

            var t = mFlyDespawnMoveEaseFunc(stateTimeElapsed, despawnMoveDelay, 0f, 0f);
            position = Vector2.Lerp(startPos, offScreenPos, t);
        }

        mRout = null;

        poolCtrl.Release();
    }
}
