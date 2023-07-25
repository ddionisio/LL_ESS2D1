using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitCitizen : Unit {
    public enum ResourceGatherType {
        None,
        Water,
        Food,
    }

    [Header("Citizen Carry Display")]
    public Transform carryRoot;
    public GameObject carryFlowerGO;
    public GameObject carryWaterGO;

    private ResourceGatherType mCarryType;

    private Structure mGatherTarget;
    private int mGatherGrabIndex = -1;
    private Waypoint mGatherWaypoint;

    private Transform mCarryRootParentDefault;
    private Vector3 mCarryRootPositionLocalDefault;

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
                if(mGatherTarget) {
                    if(mGatherTarget is StructurePlant) {
                        var plant = (StructurePlant)mGatherTarget;
                        mGatherGrabIndex = plant.BloomGrab(carryRoot);
                    }
                    //TODO: water
                }
                break;

            case UnitState.Dying:
            case UnitState.Death:
            case UnitState.Despawning:
            case UnitState.None:
                SetCarryType(ResourceGatherType.None);
                break;
        }   
    }

    protected override void ClearAIState() {
        GatherCancel();
    }

    protected override void UpdateAI() {
        switch(state) {
            case UnitState.Idle:
                if(mCarryType != ResourceGatherType.None) { //carrying something? move back to base
                    MoveTo(ownerStructure, GameData.structureWaypointSpawn, false, false);
                }
                else if(mGatherTarget) { //have a target gather? Try to move to it
                    Waypoint wp = null;

                    //plant, check if it's still valid
                    if(mGatherTarget is StructurePlant) {
                        if(CanGotoAndGatherPlant((StructurePlant)mGatherTarget))
                            wp = mGatherTarget.GetWaypointUnmarked(GameData.structureWaypointCollect);
                    }
                    //TODO: water

                    if(wp != null)
                        MoveTo(wp, false);
                    else
                        GatherCancel();
                }
                else { //look for something to gather
                    if(!RefreshAndMoveToNewTarget()) {
                        if(stateTimeElapsed >= GameData.instance.unitIdleWanderDelay) //wander
                            MoveToOwnerStructure(false);
                    }
                }
                break;

            case UnitState.Move:
                //check if we can still gather from target
                if(mGatherTarget) {
                    var isValid = false;

                    //plant
                    if(mGatherTarget is StructurePlant) {
                        isValid = CanGatherPlant((StructurePlant)mGatherTarget);
                    }
                    //TODO: water

                    if(!isValid) {
                        GatherCancel();

                        //find a new one
                        if(!RefreshAndMoveToNewTarget()) //return to base
                            MoveToOwnerStructure(false);
                    }
                }
                else if(mCarryType == ResourceGatherType.None) {
                    //look for something to gather
                    RefreshAndMoveToNewTarget();
                }
                //other things
                break;

            case UnitState.Act:
                var isActFinish = false;

                if(mGatherTarget) {
                    if(mGatherTarget is StructurePlant) {
                        if(mGatherGrabIndex != -1) {
                            var plant = (StructurePlant)mGatherTarget;
                            if(!plant.BloomIsBusy(mGatherGrabIndex)) {
                                //collect
                                SetCarryType(ResourceGatherType.Food);
                                mGatherGrabIndex = -1;
                                isActFinish = true;
                            }
                        }
                        else
                            isActFinish = true;
                    }
                    //TODO: water
                }
                else
                    isActFinish = true;

                if(isActFinish) //return to base
                    MoveToOwnerStructure(false);
                break;
        }
    }

    protected override void MoveToComplete() {
        //returning with resource, check if we are at base
        if(mCarryType != ResourceGatherType.None) {
            if(IsTouchingStructure(ownerStructure)) {
                var house = ownerStructure as StructureHouse;
                if(house) {
                    //add resource to house
                    switch(mCarryType) {
                        case ResourceGatherType.Food:
                            house.foodCount++;
                            break;
                        case ResourceGatherType.Water:
                            house.waterCount++;
                            break;
                    }
                }

                SetCarryType(ResourceGatherType.None);
            }
        }
        //gathering, check if we can still gather
        else if(mGatherTarget) {
            //are we at the gather spot?
            if(IsTouchingStructure(mGatherTarget)) {
                if(mGatherTarget is StructurePlant) {
                    if(CanGatherPlant((StructurePlant)mGatherTarget)) {
                        mGatherWaypoint = moveWaypoint;
                        if(mGatherWaypoint != null) mGatherWaypoint.AddMark(); //re-add mark to persist until we finish gathering

                        state = UnitState.Act;
                        return;
                    }
                }
                //TODO: water
            } //move to gather spot again
        } //no target

        //back to idle to re-evaluate our decisions
        base.MoveToComplete();
    }

    protected override void Init() {
        if(carryRoot) {
            mCarryRootParentDefault = carryRoot.parent;
            mCarryRootPositionLocalDefault = carryRoot.localPosition;

            carryRoot.gameObject.SetActive(false);
        }
    }

    private void GatherCancel() {
        //cancel collection action
        if(mGatherTarget) {
            var house = ownerStructure as StructureHouse;

            if(mGatherTarget is StructurePlant) {
                if(mGatherGrabIndex != -1) {
                    ((StructurePlant)mGatherTarget).BloomGrabCancel(mGatherGrabIndex);
                    mGatherGrabIndex = -1;
                }

                if(house) house.RemoveFoodGather();
            }
            //TODO: water

            mGatherTarget = null;
        }

        //free up waypoint from gathering
        if(mGatherWaypoint != null) {
            mGatherWaypoint.RemoveMark();
            mGatherWaypoint = null;
        }
    }

    private void SetCarryType(ResourceGatherType carry) {
        if(mCarryType != carry) {
            var isCarryRootActive = true;

            switch(carry) {
                case ResourceGatherType.Food:
                    if(carryFlowerGO) carryFlowerGO.SetActive(true);
                    if(carryWaterGO) carryWaterGO.SetActive(false);
                    break;
                case ResourceGatherType.Water:
                    if(carryFlowerGO) carryFlowerGO.SetActive(false);
                    if(carryWaterGO) carryWaterGO.SetActive(true);
                    break;
                case ResourceGatherType.None:
                    isCarryRootActive = false;                    
                    break;
            }

            if(carryRoot) {
                if(isCarryRootActive) {
                    carryRoot.SetParent(transform.parent);
                    carryRoot.gameObject.SetActive(true);
                }
                else {
                    carryRoot.SetParent(mCarryRootParentDefault);
                    carryRoot.localPosition = mCarryRootPositionLocalDefault;
                    carryRoot.gameObject.SetActive(false);
                }
            }

            mCarryType = carry;
        }
    }

    private bool RefreshAndMoveToNewTarget() {
        if(mGatherTarget) //fail-safe, shouldn't exist when calling this
            GatherCancel();

        var house = ownerStructure as StructureHouse;
        if(!house)
            return false;

        var structureCtrl = ColonyController.instance.structurePaletteController;

        //check if we need food
        if(house.foodCount < house.foodMax && house.isFoodGatherAvailable) {
            //find nearest plant with available gather
            StructurePlant plant = null;
            for(int i = 0; i < house.houseData.foodStructureSources.Length; i++) {
                plant = structureCtrl.GetStructureNearestActive<StructurePlant>(position.x, house.houseData.foodStructureSources[i], CanGotoAndGatherPlant);
                if(plant)
                    break;
            }

            //set as target, and move to it
            if(plant) {
                mGatherTarget = plant;

                var wp = plant.GetWaypointUnmarked(GameData.structureWaypointCollect);
                MoveTo(wp, false);

                house.AddFoodGather();

                return true;
            }
        }

        //check if we need water
        //TODO

        return false;
    }

    private bool CanGatherPlant(StructurePlant plant) {
        return plant.growthState == StructurePlant.GrowthState.Bloom && plant.BloomGrabAvailableIndex() != -1;
    }

    private bool CanGotoAndGatherPlant(StructurePlant plant) {
        if(CanGatherPlant(plant)) {
            //check if all collect waypoint is marked (this means someone else is on the way)
            var unmarkedCollectWp = plant.GetWaypointUnmarked(GameData.structureWaypointCollect);

            return unmarkedCollectWp != null;
        }

        return false;
    }
}