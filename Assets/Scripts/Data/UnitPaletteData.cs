using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "unitPalette", menuName = "Game/Unit Palette")]
public class UnitPaletteData : ScriptableObject {
    [System.Serializable]
    public struct UnitInfo {
        public UnitData data;
        public bool isHidden;
    }

    public UnitInfo[] units;

    public int capacityStart;
    public int capacity;

    public int GetIndex(UnitData unitData) {
        for(int i = 0; i < units.Length; i++) {
            if(units[i].data == unitData)
                return i;
        }

        return -1;
    }
}
