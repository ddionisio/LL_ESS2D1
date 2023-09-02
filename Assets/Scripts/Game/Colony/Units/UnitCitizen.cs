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

    [Header("Citizen SFX")]
    [M8.SoundPlaylist]
    public string sfxPickup;

    public override bool canSwim { get { return true; } }

    private ResourceGatherType mCarryType;

    private Structure mGatherTarget;
    private bool mGatherInProcess;
    private int mGatherGrabIndex = -1;
    private Waypoint mGatherWaypoint;

    private Transform mCarryRootParentDefault;
    private Vector3 mCarryRootPositionLocalDefault;

    public bool canGather { get { return ColonyController.instance.cycleAllowProgress && !ColonyController.instance.cycleController.isHazzard && !isSwimming; } }

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
                    if(ownerStructure.state != StructureState.Moving)
                        MoveTo(ownerStructure, GameData.structureWaypointSpawn, false, false);
                    else
                        MoveTo(ownerStructure.position, false);
                }
                else if(mGatherTarget) { //have a target gather? Try to move to it
                    Waypoint wp = null;

                    //plant, check if it's still valid
                    if(mGatherTarget is StructurePlant) {
                        if(CanGotoAndGatherPlant((StructurePlant)mGatherTarget))
                            wp = mGatherTarget.GetWaypointUnmarkedClosest(GameData.structureWaypointCollect, position.x);
                    }
                    else if(mGatherTarget is StructureResourceGenerateContainer) {
                        if(CanGotoAndGatherWater((StructureResourceGenerateContainer)mGatherTarget))
                            wp = mGatherTarget.GetWaypointUnmarkedClosest(GameData.structureWaypointCollect, position.x);
                    }

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
                    else if(mGatherTarget is StructureResourceGenerateContainer)
                        isValid = CanGatherWater((StructureResourceGenerateContainer)mGatherTarget);

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
        }
    }

    protected override void MoveToComplete() {
        //returning with resource, check if we are at base
        if(mCarryType != ResourceGatherType.None) {
            if(ownerStructure.state != StructureState.Moving && IsTouchingStructure(ownerStructure)) { //don't add yet until the house has finished moving
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
                else if(mGatherTarget is StructureResourceGenerateContainer) {
                    if(CanGatherWater((StructureResourceGenerateContainer)mGatherTarget)) {
                        mGatherWaypoint = moveWaypoint;
                        if(mGatherWaypoint != null) mGatherWaypoint.AddMark(); //re-add mark to persist until we finish gathering

                        state = UnitState.Act;
                        return;
                    }
                }
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

    IEnumerator DoAct() {
        if(mGatherTarget) {
            if(mGatherTarget is StructurePlant) {
                var plant = (StructurePlant)mGatherTarget;
                mGatherGrabIndex = plant.BloomGrab(carryRoot);
            }
            else if(mGatherTarget is StructureResourceGenerateContainer) {
                var resGen = (StructureResourceGenerateContainer)mGatherTarget;
                resGen.resource--;
            }

            mGatherInProcess = true;
        }

        var isActFinish = false;
        while(!isActFinish) {
            yield return null;

            if(mGatherTarget) {
                if(mGatherTarget.state == StructureState.Destroyed || mGatherTarget.state == StructureState.None) { //got destroyed or something, can't collect
                    mGatherGrabIndex = -1;
                    mGatherInProcess = false;
                    isActFinish = true;
                }
                else if(mGatherTarget is StructurePlant) {
                    if(mGatherGrabIndex != -1) {
                        var plant = (StructurePlant)mGatherTarget;
                        if(!plant.BloomIsBusy(mGatherGrabIndex)) {
                            //collect
                            SetCarryType(ResourceGatherType.Food);
                            mGatherGrabIndex = -1;
                            mGatherInProcess = false;
                            isActFinish = true;
                        }
                    }
                    else
                        isActFinish = true;
                }
                else if(mGatherTarget is StructureResourceGenerateContainer) {
                    if(stateTimeElapsed >= GameData.instance.unitGatherContainerDelay) {
                        //collect
                        SetCarryType(ResourceGatherType.Water);
                        mGatherInProcess = false;
                        isActFinish = true;
                    }
                }
            }
            else
                isActFinish = true;
        }

        mRout = null;

        MoveToOwnerStructure(false); //return to base
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
            else if(mGatherTarget is StructureResourceGenerateContainer) {
                if(house) house.RemoveWaterGather();

                if(mGatherInProcess)
                    ((StructureResourceGenerateContainer)mGatherTarget).resource++; //return resource
            }

            mGatherTarget = null;
        }

        mGatherInProcess = false;

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
                    if(!string.IsNullOrEmpty(sfxPickup)) M8.SoundPlaylist.instance.Play(sfxPickup, false);

                    if(carryFlowerGO) carryFlowerGO.SetActive(true);
                    if(carryWaterGO) carryWaterGO.SetActive(false);
                    break;
                case ResourceGatherType.Water:
                    if(!string.IsNullOrEmpty(sfxPickup)) M8.SoundPlaylist.instance.Play(sfxPickup, false);

                    if(carryFlowerGO) carryFlowerGO.SetActive(false);
                    if(carryWaterGO) carryWaterGO.SetActive(true);
                    break;
                case ResourceGatherType.None:
                    isCarryRootActive = false;                    
                    break;
            }

            if(carryRoot) {
                if(isCarryRootActive) {
                    carryRoot.SetParent(transform.parent, true);
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

        //can't gather if cycle is paused
        if(!canGather)
            return false;

        var house = ownerStructure as StructureHouse;
        if(!house)
            return false;

        var structureCtrl = ColonyController.instance.structurePaletteController;

        //check if we need food
        if(house.isFoodGatherAvailable) {
            //find nearest plant with available gather
            StructurePlant plant = null;
            for(int i = 0; i < GameData.instance.structureFoodSources.Length; i++) {
                plant = structureCtrl.GetStructureNearestActive<StructurePlant>(position.x, GameData.instance.structureFoodSources[i], CanGotoAndGatherPlant);
                if(plant)
                    break;
            }

            //set as target, and move to it
            if(plant) {
                mGatherTarget = plant;

                var wp = plant.GetWaypointUnmarkedClosest(GameData.structureWaypointCollect, position.x);
                MoveTo(wp, false);

                house.AddFoodGather();

                return true;
            }
        }

        //check if we need water
        if(house.isWaterGatherAvailable) {
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

                house.AddWaterGather();

                return true;
            }
        }

        return false;
    }

    private bool CanGatherPlant(StructurePlant plant) {
        return canGather && plant.growthState == StructurePlant.GrowthState.Bloom && plant.BloomGrabAvailableIndex() != -1;
    }

    private bool CanGotoAndGatherPlant(StructurePlant plant) {
        if(CanGatherPlant(plant)) {
            //check if all collect waypoint is marked (this means someone else is on the way)
            var unmarkedCollectWp = plant.GetWaypointUnmarkedClosest(GameData.structureWaypointCollect, position.x);

            return unmarkedCollectWp != null;
        }

        return false;
    }

    private bool CanGatherWater(StructureResourceGenerateContainer waterGen) {
        return canGather && waterGen.resourceWhole > 0;
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