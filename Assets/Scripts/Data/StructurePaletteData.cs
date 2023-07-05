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
}
