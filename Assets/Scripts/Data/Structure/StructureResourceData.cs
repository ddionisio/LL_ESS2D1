using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "structureResource", menuName = "Game/Structure/Resource")]
public class StructureResourceData : StructureData {
    public enum ResourceType {
        None,
        Power,
        Water,
    }

    [Header("Resource Info")]
    public CycleResourceType resourceInputType;
    public ResourceType resourceType;
    public float resourceGenerateRate;
    public float resourceCapacity;
}
