using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitTargetStructureData : UnitData {
    [System.Flags]
    public enum StructureTargetFlags {
        None = 0x0,
        Plants = 0x1,
        ColonyShip = 0x2,
        House = 0x4,
        Resource = 0x8,
    }

    [Header("Target Structure Info")]
    [M8.EnumMask]
    public StructureTargetFlags structureTargetFlags;
    [Tooltip("Set to 'None' for any resource structure, specific otherwise")]
    public StructureResourceData.ResourceType structureResourceType = StructureResourceData.ResourceType.None;

	public bool IsTargetable(Structure structure) {
        var dat = structure.data;
        
        if(structure.state == StructureState.None || structure.state == StructureState.Spawning || structure.state == StructureState.Construction || structure.state == StructureState.Demolish || structure.state == StructureState.Moving)
            return false;

        if((structureTargetFlags & StructureTargetFlags.Plants) != StructureTargetFlags.None && dat is StructurePlantData)
            return true;

        if((structureTargetFlags & StructureTargetFlags.ColonyShip) != StructureTargetFlags.None && dat is StructureColonyShipData)
            return true;

        if((structureTargetFlags & StructureTargetFlags.House) != StructureTargetFlags.None && dat is StructureHouseData)
            return true;

        if((structureTargetFlags & StructureTargetFlags.Resource) != StructureTargetFlags.None && dat is StructureResourceData) {
            if(structureResourceType != StructureResourceData.ResourceType.None) {
                var resDat = (StructureResourceData)dat;
                return structureResourceType == resDat.resourceType;
            }

            return true;
        }

        return false;
    }
}
