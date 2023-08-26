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

        public CycleResourceScale resourceScaleMod;
    }

    public WeatherInfo[] cycles;
    public float cycleTotalDuration = 150f;

    public CycleResourceScale resourceScale;

    public float cycleDuration { get { return cycles.Length > 0 ? cycleTotalDuration / cycles.Length : 0f; } }
}
