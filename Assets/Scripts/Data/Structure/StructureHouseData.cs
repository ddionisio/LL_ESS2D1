using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "structureHouse", menuName = "Game/Structure/House")]
public class StructureHouseData : StructureData {
    [System.Serializable]
    public struct PopulationLevelInfo {
        public int foodMax; //food required
        public int waterMax; //water required
        public float powerConsumptionRate; //power consumption per second
    }

    [Header("Citizen Info")]
    public UnitData citizenData;
    public int citizenStartCount = 1;
    public int citizenCapacity;

    [Header("Population Info")]
    [SerializeField]
    PopulationLevelInfo[] _populationLevels;

    [Header("Structure Resource Look-ups")]
    public StructureData[] foodStructureSources;
    public StructureData[] waterStructureSources;

    public float populationPowerConsumeDelay; //how long to consume power before increasing population (if powerConsumptionRate > 0)

    public int populationLevelCount { get { return _populationLevels.Length; } }

    public PopulationLevelInfo GetPopulationLevelInfo(int index) {
        return index >= 0 && index < _populationLevels.Length ? _populationLevels[index] : new PopulationLevelInfo(); //fail-safe
    }

    public override void SetupUnitSpawns(UnitController unitCtrl, int structureCount) {
        unitCtrl.AddUnitData(citizenData, citizenCapacity * structureCount);
    }
}
