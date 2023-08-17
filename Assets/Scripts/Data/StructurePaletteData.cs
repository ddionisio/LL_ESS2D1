using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "structurePalette", menuName = "Game/Structure Palette")]
public class StructurePaletteData : ScriptableObject {
    [System.Serializable]
    public struct StructureInfo {
        public StructureData data;
        public int populationQuotaUnlock; //set to 0 to unlock right away

        public bool IsHidden(int population) {
            return population < populationQuotaUnlock;
        }
    }

    [System.Serializable]
    public class GroupInfo {
        [System.Serializable]
        public struct CapacityUpgradeInfo {
            public int populationQuota;
            public int capacityIncrease;
        }

        [Header("Info")]
        [M8.Localize]
        public string nameRef;

        public Sprite icon;

        [Header("Catalog")]
        public StructureInfo[] structures;
        
        [Header("Capacity Info")]
        public int capacityStart; //starting capacity
        public int capacity;

        public CapacityUpgradeInfo[] capacityUpgrades;

        public int GetCurrentCapacity(int population) {
            int curCapacity = capacityStart;

            for(int i = 0; i < capacityUpgrades.Length; i++) {
                var upgradeInfo = capacityUpgrades[i];
                if(upgradeInfo.populationQuota <= population)
                    curCapacity += upgradeInfo.capacityIncrease;
            }

            return Mathf.Clamp(curCapacity, 0, capacity);
        }
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
