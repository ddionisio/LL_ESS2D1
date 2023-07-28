using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "unitStructureEater", menuName = "Game/Unit/Structure Eater")]
public class UnitStructureEaterData : UnitTargetStructureData {
    [Header("Growth Data")]
    public float growthDelay;
    public float attackDelay;
}
