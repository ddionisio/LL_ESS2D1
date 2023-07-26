using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureResourceGenerateContainer : Structure {
    [Header("Container Display")]
    public SpriteRenderer containerFillRender;

    public StructureResourceData resourceData { get; private set; }

    public float resource {
        get { return mResource; }
        set {
            if(mResource != value) {
                mResource = Mathf.Clamp(value, 0f, resourceCapacity);

                //update
                RefreshDisplay();
            }
        }
    }

    public int resourceWhole {
        get { return Mathf.FloorToInt(mResource); }
        set {
            if(resourceWhole != value) {
                var fraction = mResource - Mathf.Floor(mResource);
                resource = value + fraction;
            }
        }
    }

    public float resourceCapacity {
        get {
            return resourceData ? resourceData.resourceCapacity : 0f;
        }
    }

    public int resourceCapacityWhole {
        get {
            return resourceData ? Mathf.FloorToInt(resourceData.resourceCapacity) : 0;
        }
    }

    private Vector2 mContainerFillSize;
    private float mResource;

    protected override void Despawned() {
        resourceData = null;
    }

    protected override void Spawned() {
        resourceData = data as StructureResourceData;

        RefreshDisplay();
    }

    protected override void Init() {
        if(containerFillRender)
            mContainerFillSize = containerFillRender.size;
    }

    protected override void ApplyCurrentState() {
        base.ApplyCurrentState();

        switch(state) {
            case StructureState.Active:
                mRout = StartCoroutine(DoActive());
                break;

            case StructureState.Destroyed:
                mResource = 0f;
                RefreshDisplay();
                break;

            case StructureState.None:
                mResource = 0f;
                break;
        }
    }

    IEnumerator DoActive() {
        if(!resourceData) { //fail-safe
            mRout = null;
            yield break;
        }

        var cycleCtrl = ColonyController.instance.cycleController;

        while(true) {
            yield return null;

            if(resource < resourceCapacity) {
                var rate = resourceData.resourceGenerateRate + cycleCtrl.GetResourceRate(resourceData.resourceInputType);

                resource += rate * Time.deltaTime;
            }
        }
    }

    private void RefreshDisplay() {
        if(containerFillRender) {
            var fillScale = resourceCapacity > 0f ? Mathf.Clamp01(resource / resourceCapacity) : 0f;
            containerFillRender.size = new Vector2(mContainerFillSize.x, mContainerFillSize.y * fillScale);
        }
    }
}
