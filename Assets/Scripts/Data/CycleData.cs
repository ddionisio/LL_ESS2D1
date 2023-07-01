using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "cycle", menuName = "Game/Cycle")]
public class CycleData : ScriptableObject {
    [System.Serializable]
    public struct WeatherInfo {
        [Header("Atmosphere Info")]
        public WeatherTypeData weather;
        public AtmosphereModifier[] atmosphereMods;
    }

    [Header("Cycles")]
    public WeatherInfo[] cycles;
}
