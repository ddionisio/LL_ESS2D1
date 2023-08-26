using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureResourceGenerateGlobal : Structure {
    public StructureResourceData resourceData { get; private set; }

    public StructureResourceData.ResourceType resourceType {
        get {
            return resourceData ? resourceData.resourceType : StructureResourceData.ResourceType.None;
        }
    }

    public float resourceCapacity {
        get {
            return resourceData ? resourceData.resourceCapacity : 0f;
        }
    }

    public float resourceFixedScale {
        get {
            return Mathf.Clamp01((float)hitpointsCurrent / hitpointsMax);
        }
    }

    private bool mIsResourceGlobalCapacityApplied;
    private ColonyController.ResourceFixedAmount mResourceFixed;

    protected override void HitpointsChanged(int previousHitpoints) {
        base.HitpointsChanged(previousHitpoints);

        //reduce output based on hitpoints
        if(mResourceFixed != null && hitpointsCurrent > 0) {
            mResourceFixed.UpdateBase(resourceData.resourceFixedValue * resourceFixedScale);
            ColonyController.instance.RefreshResources(resourceType);
        }
    }

    protected override void Despawned() {
        resourceData = null;
    }

    protected override void Spawned() {
        resourceData = data as StructureResourceData;

    }

    protected override void ClearCurrentState() {
        base.ClearCurrentState();

    }

    protected override void ApplyCurrentState() {
        base.ApplyCurrentState();

        switch(state) {
            case StructureState.Active:
                ApplyGlobalResource(true);

                if(resourceData && resourceData.resourceGenerateRate > 0f)
                    mRout = StartCoroutine(DoActive());
                break;

            case StructureState.Destroyed:
            case StructureState.None:
                ApplyGlobalResource(false);
                break;
        }
    }

    private void ApplyGlobalResource(bool apply) {
        if(mIsResourceGlobalCapacityApplied != apply) {
            mIsResourceGlobalCapacityApplied = apply;

            var colonyCtrl = ColonyController.instance;

            var resGlobalCapacity = colonyCtrl.GetResourceCapacity(resourceType);

            if(apply) {
                if(resourceData.resourceFixedValue > 0f)
                    mResourceFixed = colonyCtrl.AddResourceFixedAmount(resourceType, resourceData.resourceInputType, resourceData.resourceFixedValue * resourceFixedScale);

                colonyCtrl.SetResourceCapacity(resourceType, resGlobalCapacity + resourceCapacity);
            }
            else {
                if(mResourceFixed != null) {
                    colonyCtrl.RemoveResourceFixedAmount(resourceType, mResourceFixed);
                    mResourceFixed = null;
                }

                colonyCtrl.SetResourceCapacity(resourceType, resGlobalCapacity - resourceCapacity);
            }
        }
    }

    IEnumerator DoActive() {
        var colonyCtrl = ColonyController.instance;
        var cycleCtrl = colonyCtrl.cycleController;

        while(true) {
            yield return null;

            var resGlobal = colonyCtrl.GetResourceAmount(resourceType);
            var resCapacityGlobal = colonyCtrl.GetResourceCapacity(resourceType);

            if(cycleCtrl.cycleTimeScale > 0f && resGlobal < resCapacityGlobal) {
                var rate = resourceData.resourceGenerateRate * cycleCtrl.GetResourceScale(resourceData.resourceInputType);

                colonyCtrl.AddResourceAmount(resourceType, rate * Time.deltaTime);
            }
        }
    }
}
