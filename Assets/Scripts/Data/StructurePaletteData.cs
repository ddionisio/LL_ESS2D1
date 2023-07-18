using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "structurePalette", menuName = "Game/Structure Palette")]
public class StructurePaletteData : ScriptableObject {
    [System.Serializable]
    public struct StructureInfo {
        public StructureData data;
        public bool isHidden;
    }

    [System.Serializable]
    public class GroupInfo {
        [Header("Info")]
        [M8.Localize]
        public string nameRef;

        public Sprite icon;

        [Header("Catalog")]
        public StructureInfo[] structures;

        public int capacityStart; //starting capacity
        public int capacity;
    }

    public GroupInfo[] groups;

    public int GetGroupIndex(StructureData data) {
        for(int i = 0; i < groups.Length; i++) {
            var itm = groups[i];

            bool isFound = false;
            for(int j = 0; j < itm.structures.Length; j++) {
                if(itm.structures[j].data == data) {
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
