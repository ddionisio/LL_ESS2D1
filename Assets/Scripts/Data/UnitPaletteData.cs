using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "unitPalette", menuName = "Game/Unit Palette")]
public class UnitPaletteData : ScriptableObject {
    public UnitData[] items;
}
