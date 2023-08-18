using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "structureHouse", menuName = "Game/Structure/House")]
public class StructureHouseData : StructureData {
    [System.Serializable]
    public struct PopulationLevelInfo {
        /// <summary>
        /// Amount of food needed, set to 0 disregard this requirement.
        /// </summary>
        public int foodMax;

        /// <summary>
        /// Amount of water needed, set to 0 disregard this requirement.
        /// </summary>
        public int waterMax;

        /// <summary>
        /// Amount of power to consume, set to 0 disregard this requirement.
        /// </summary>
        public float powerConsumption;
    }

    [Header("Citizen Info")]
    public UnitData citizenData;
    public int citizenStartCount = 1;
    public int citizenCapacity;

    [Header("Population Info")]
    [SerializeField]
    PopulationLevelInfo[] _populationLevels;

    public int populationLevelCount { get { return _populationLevels.Length; } }

    public PopulationLevelInfo GetPopulationLevelInfo(int index) {
        return index >= 0 && index < _populationLevels.Length ? _populationLevels[index] : new PopulationLevelInfo(); //fail-safe
    }

    public override void Setup(ColonyController colonyCtrl, int structureCount) {
        colonyCtrl.unitController.AddUnitData(citizenData, citizenCapacity * structureCount, false);
        citizenData.Setup(colonyCtrl);
    }
}
