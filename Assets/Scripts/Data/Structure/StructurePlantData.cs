using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "structurePlant", menuName = "Game/Structure/Plant")]
public class StructurePlantData : StructureData {
    [Header("Plant Stats")]
    public float readyDelay = 1f;
    public float growthDelay = 3f;
}
