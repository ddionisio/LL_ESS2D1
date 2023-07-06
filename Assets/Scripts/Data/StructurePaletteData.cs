using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "structurePalette", menuName = "Game/Structure Palette")]
public class StructurePaletteData : ScriptableObject {
    [System.Serializable]
    public class ItemInfo {
        [Header("Info")]
        [M8.Localize]
        public string nameRef;

        public Sprite icon;

        [Header("Catalog")]
        public StructureData[] structures;

        public int capacity;
    }

    public ItemInfo[] items;

    public int GetItemIndex(StructureData data) {
        for(int i = 0; i < items.Length; i++) {
            var itm = items[i];

            bool isFound = false;
            for(int j = 0; j < itm.structures.Length; j++) {
                if(itm.structures[j] == data) {
                    isFound = true;
                    break;
                }
            }

            if(isFound)
                return i;
        }

        return -1;
    }
}
