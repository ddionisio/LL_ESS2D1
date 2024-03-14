using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "unitStructureDOT", menuName = "Game/Unit/Structure DOT")]
public class UnitStructureDOTData : UnitTargetStructureData {
	[Header("DOT Data")]
	public float growthDelay;
	public float attackDelay;
}
