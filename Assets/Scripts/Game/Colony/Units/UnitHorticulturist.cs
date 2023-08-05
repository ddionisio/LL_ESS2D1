using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitHorticulturist : Unit {
    [Header("Resource Info")]
    public int resourceCapacity = 3;

    [Header("Arable Field Develop Info")]
    public float arableFieldCultivateDelay = 1f; //how long it takes to add health to arable land

    [Header("Carry Display")]
    public Transform carryRoot;

    [Header("UnitHorticulturist Animation")]
    [M8.Animator.TakeSelector]
    public string takeGather;
    [M8.Animator.TakeSelector]
    public string takeCultivate;

    public int resourceCount {
        get { return mResourceCount; }
        set {
            var _val = Mathf.Clamp(value, 0, resourceCapacity);
            if(mResourceCount != _val) {
                mResourceCount = _val;

                if(carryRoot) {
                    var isCarry = mResourceCount > 0;

                    var go = carryRoot.gameObject;
                    if(go.activeSelf != isCarry) {
                        if(isCarry) {
                            carryRoot.SetParent(transform.parent, true);
                            carryRoot.gameObject.SetActive(true);
                        }
                        else {
                            carryRoot.SetParent(mCarryRootParentDefault);
                            carryRoot.localPosition = mCarryRootPositionLocalDefault;
                            carryRoot.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }
    }

    private StructureResourceGenerateContainer mGatherTarget;
    private bool mGatherInProcess;
    private Waypoint mGatherWaypoint;

    private int mResourceCount;

    private ArableField mArableFieldTarget;

    private Transform mCarryRootParentDefault;
    private Vector3 mCarryRootPositionLocalDefault;

    private int mTakeGatherInd = -1;
    private int mTakeCultivateInd = -1;

    protected override int GetActTakeIndex() {
        return -1; //let our own act set the animation
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
                mRout = StartCoroutine(DoAct());
                break;

            case UnitState.Dying:
            case UnitState.Death:
            case UnitState.Despawning:
            case UnitState.None:
                resourceCount = 0;
                break;
        }
    }

    protected override void ClearAIState() {
        GatherCancel();
        ArableFieldCancel();
    }

    protected override void UpdateAI() {
        switch(state) {
            case UnitState.Idle:
                if(resourceCount > 0) {
                    //move to field target
                    if(mArableFieldTarget) {
                        MoveToArableField(mArableFieldTarget);
                        return;
                    }

                    //find arable field
                    if(RefreshAndMoveToArableField())
                        return;

                    //refill water?
                    if(resourceCount <= 0) {
                        if(RefreshAndMoveToNewResource())
                            return;
                    }
                }
                else if(mGatherTarget) {
                    Waypoint wp = null;

                    //check if it's still valid
                    if(CanGotoAndGatherWater(mGatherTarget))
                        wp = mGatherTarget.GetWaypointUnmarkedClosest(GameData.structureWaypointCollect, position.x);

                    if(wp != null)
                        MoveTo(wp, false);
                    else
                        GatherCancel();
                }
                else {
                    //check if there are fields that need development
                    if(ArableField.arableFieldAvailable.Count > 0) {
                        if(RefreshAndMoveToNewResource()) //find resource
                            return;
                    }
                }

                if(stateTimeElapsed >= GameData.instance.unitIdleWanderDelay) //wander
                    Wander();
                break;

            case UnitState.Move:
                if(mArableFieldTarget) {
                    if(mArableFieldTarget.isHealthFull) { //still arable?
                        ArableFieldCancel();

                        //stop and rethink
                        state = UnitState.Idle;
                    }
                }
                else if(mGatherTarget) {
                    if(!CanGatherWater(mGatherTarget)) { //can still gather?
                        GatherCancel();

                        //stop and rethink
                        state = UnitState.Idle;
                    }
                }
                //other things
                break;
        }
    }

    protected override void MoveToComplete() {
        if(mArableFieldTarget) {
            if(mArableFieldTarget.health < mArableFieldTarget.healthMax) {
                state = UnitState.Act;
                return;
            }
            else
                ArableFieldCancel();
        }
        else if(mGatherTarget) {
            //are we at the gather spot?
            if(IsTouchingStructure(mGatherTarget)) {
                if(CanGatherWater(mGatherTarget)) {
                    mGatherWaypoint = moveWaypoint;
                    if(mGatherWaypoint != null) mGatherWaypoint.AddMark(); //re-add mark to persist until we finish gathering

                    state = UnitState.Act;
                    return;
                }
            } //move to gather spot again
        }

        //back to idle to re-evaluate our decisions
        base.MoveToComplete();
    }

    protected override void Init() {
        if(carryRoot) {
            mCarryRootParentDefault = carryRoot.parent;
            mCarryRootPositionLocalDefault = carryRoot.localPosition;

            carryRoot.gameObject.SetActive(false);
        }

        if(animator) {
            mTakeGatherInd = animator.GetTakeIndex(takeGather);
            mTakeCultivateInd = animator.GetTakeIndex(takeCultivate);
        }
    }

    IEnumerator DoAct() {
        yield return null;

        if(mGatherTarget) {
            if(mTakeGatherInd != -1)
                animator.Play(mTakeGatherInd);
                        
            if(!(mGatherTarget.state == StructureState.Destroyed || mGatherTarget.state == StructureState.None)) { //got destroyed or something, can't collect
                mGatherTarget.resource--;
                mGatherInProcess = true;

                do {
                    yield return null;
                } while(stateTimeElapsed < GameData.instance.unitGatherContainerDelay);

                //collect
                resourceCount = resourceCapacity;
                mGatherInProcess = false;
            }
        }
        else if(mArableFieldTarget) {
            if(mTakeCultivateInd != -1)
                animator.Play(mTakeCultivateInd);

            while(!mArableFieldTarget.isHealthFull && resourceCount > 0) {
                //add hitpoint
                mArableFieldTarget.health++;
                resourceCount--;

                //wait a bit
                do {
                    yield return null;
                } while(stateTimeElapsed < arableFieldCultivateDelay);

                RestartStateTime();
            }
        }

        mRout = null;

        state = UnitState.Idle;
    }

    private void GatherCancel() {
        //cancel collection action
        if(mGatherTarget) {
            if(mGatherInProcess)
                mGatherTarget.resource++; //return resource

            mGatherTarget = null;
        }

        mGatherInProcess = false;

        //free up waypoint from gathering
        if(mGatherWaypoint != null) {
            mGatherWaypoint.RemoveMark();
            mGatherWaypoint = null;
        }
    }

    private void ArableFieldCancel() {
        if(mArableFieldTarget) {
            mArableFieldTarget.RemoveMark();
            mArableFieldTarget = null;
        }
    }

    private void MoveToArableField(ArableField field) {
        var fieldPos = field.position;

        GroundPoint groundPt;
        if(GroundPoint.GetGroundPoint(fieldPos.x, out groundPt))
            MoveTo(groundPt.position, false);
        else
            MoveTo(fieldPos, false);
    }

    private bool RefreshAndMoveToArableField() {
        if(mArableFieldTarget) //just in case
            ArableFieldCancel();

        if(resourceCount > 0) {
            var arableFields = ArableField.arableFieldAvailable;

            ArableField nearField = null;
            float nearFieldDist = 0f;

            for(int i = 0; i < arableFields.Count; i++) {
                var field = arableFields[i];
                if(field.isMarked)
                    continue;

                var fieldDist = Mathf.Abs(field.position.x - position.x);
                if(!nearField || (field.health <= nearField.health && fieldDist < nearFieldDist)) {
                    nearField = field;
                    nearFieldDist = fieldDist;
                }
            }

            if(nearField) {
                mArableFieldTarget = nearField;
                mArableFieldTarget.AddMark();

                MoveToArableField(mArableFieldTarget);

                return true;
            }
        }
        else //need to find resource
            return RefreshAndMoveToNewResource();

        return false;
    }

    private bool RefreshAndMoveToNewResource() {
        if(mGatherTarget) //fail-safe, shouldn't exist when calling this
            GatherCancel();

        var structureCtrl = ColonyController.instance.structurePaletteController;
                
        //check if we need water
        if(resourceCount <= 0) {
            //find nearest water generator with available gather
            StructureResourceGenerateContainer waterGen = null;
            for(int i = 0; i < GameData.instance.structureWaterSources.Length; i++) {
                waterGen = structureCtrl.GetStructureNearestActive<StructureResourceGenerateContainer>(position.x, GameData.instance.structureWaterSources[i], CanGotoAndGatherWater);
                if(waterGen)
                    break;
            }

            //set as target, and move to it
            if(waterGen) {
                mGatherTarget = waterGen;

                var wp = waterGen.GetWaypointUnmarkedClosest(GameData.structureWaypointCollect, position.x);
                MoveTo(wp, false);

                return true;
            }
        }

        return false;
    }

    private void Wander() {
        var structureCtrl = ColonyController.instance.structurePaletteController;

        //check if there are water sources nearby
        //find nearest water generator with available gather
        StructureResourceGenerateContainer waterGen = null;
        for(int i = 0; i < GameData.instance.structureWaterSources.Length; i++) {
            waterGen = structureCtrl.GetStructureNearestActive<StructureResourceGenerateContainer>(position.x, GameData.instance.structureWaterSources[i], null);
            if(waterGen)
                break;
        }

        if(waterGen)
            MoveTo(waterGen, GameData.structureWaypointIdle, false, false);
        else //just move back to base
            MoveToOwnerStructure(false);
    }

    private bool CanGatherWater(StructureResourceGenerateContainer waterGen) {
        return waterGen.resourceWhole > 0;
    }

    private bool CanGotoAndGatherWater(StructureResourceGenerateContainer waterGen) {
        if(CanGatherWater(waterGen)) {
            //check if all collect waypoint is marked (this means someone else is on the way)
            var unmarkedCollectWp = waterGen.GetWaypointUnmarkedClosest(GameData.structureWaypointCollect, position.x);

            return unmarkedCollectWp != null;
        }

        return false;
    }
}
