using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "unitPalette", menuName = "Game/Unit Palette")]
public class UnitPaletteData : ScriptableObject {
    [System.Serializable]
    public struct UnitInfo {
        public UnitData data;
        public int populationQuotaUnlock; //set to 0 to unlock right away

        public bool IsHidden(int population) {
            return population < populationQuotaUnlock;
        }
    }

    [System.Serializable]
    public struct CapacityUpgradeInfo {
        public int populationQuota;
        public int capacityIncrease;
    }

    public UnitInfo[] units;
    
    public int capacityStart;
    public int capacity;

    public CapacityUpgradeInfo[] capacityUpgrades;

    public int GetIndex(UnitData unitData) {
        for(int i = 0; i < units.Length; i++) {
            if(units[i].data == unitData)
                return i;
        }

        return -1;
    }

    public int GetCurrentCapacity(int population) {
        int curCapacity = capacityStart;

        if(ColonyController.isInstantiated) {
            for(int i = 0; i < capacityUpgrades.Length; i++) {
                var upgradeInfo = capacityUpgrades[i];
                if(upgradeInfo.populationQuota <= population)
                    curCapacity += upgradeInfo.capacityIncrease;
            }
        }

        return Mathf.Clamp(curCapacity, 0, capacity);
    }
}
