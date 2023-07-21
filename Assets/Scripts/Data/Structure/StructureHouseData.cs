using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "structureHouse", menuName = "Game/Structure/House")]
public class StructureHouseData : StructureData {
    [System.Serializable]
    public struct PopulationLevelInfo {
        public int foodCount; //food required
        public int waterCount; //water required
        public float powerConsumptionRate; //power consumption per second
    }

    [Header("Citizen Info")]
    public UnitData citizenData;
    public int citizenCapacity;

    [Header("Population Info")]
    public PopulationLevelInfo[] populationLevels;
    public float populationPowerConsumeDelay; //how long to consume power before increasing population (if powerConsumptionRate > 0)

    public override void SetupUnitSpawns(UnitController unitCtrl, int structureCount) {
        unitCtrl.AddUnitData(citizenData, citizenCapacity * structureCount);
    }
}
