using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitTargetStructureData : UnitData {
    [System.Flags]
    public enum StructureTargetFlags {
        None = 0x0,
        Plants = 0x1,
        Buildables = 0x2,
        Invulnerables = 0x4,
    }

    [Header("Target Structure Info")]
    [M8.EnumMask]
    public StructureTargetFlags structureTargetFlags;

    public bool IsTargetable(Structure structure) {
        var dat = structure.data;
        
        if(structure.state == StructureState.None || structure.state == StructureState.Demolish || structure.state == StructureState.Moving)
            return false;

        bool invulValid = dat.hitpoints == 0 && (structureTargetFlags | StructureTargetFlags.Invulnerables) == StructureTargetFlags.None,
             plantValid = dat is StructurePlantData && (structureTargetFlags | StructureTargetFlags.Plants) != StructureTargetFlags.None,
             buildableValid = dat.buildTime > 0f && (structureTargetFlags | StructureTargetFlags.Buildables) != StructureTargetFlags.None;

        return invulValid || plantValid || buildableValid;
    }
}
