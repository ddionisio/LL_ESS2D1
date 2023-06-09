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

        public CycleResource resourceRateMod;
    }

    public WeatherInfo[] cycles;

    public CycleResource resourceRate;
}
