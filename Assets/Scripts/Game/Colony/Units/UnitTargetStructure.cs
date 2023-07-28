using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UnitTargetStructure : Unit {
    public Structure targetStructure { 
        get { return mTargetStructure; }
        set {
            if(mTargetStructure != value) {
                mTargetStructure = value;
                TargetChanged();
            }
        }
    }

    private Structure mTargetStructure;

    protected virtual void TargetChanged() { }

    protected override void Spawned(M8.GenericParams parms) {
        if(parms != null) {
            if(parms.ContainsKey(UnitSpawnParams.structureTarget))
                mTargetStructure = parms.GetValue<Structure>(UnitSpawnParams.structureTarget);
        }
    }

    protected override void ApplyCurrentState() {
        base.ApplyCurrentState();

        switch(state) {
            case UnitState.None:
                mTargetStructure = null;
                break;
        }
    }
}
