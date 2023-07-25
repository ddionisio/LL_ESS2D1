using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "unitGardener", menuName = "Game/Unit/Gardener")]
public class UnitGardenerData : UnitData {
    [Header("Gardener Info")]
    public StructureData[] targetPlantStructures;
}
