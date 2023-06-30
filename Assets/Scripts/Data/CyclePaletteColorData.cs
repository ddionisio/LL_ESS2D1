using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "cyclePalette", menuName = "Game/Cycle Palette Color")]
public class CyclePaletteColorData : ScriptableObject {
    public struct WeatherModInfo {
        public WeatherTypeData weather;
        public Color color;
    }

    public Color[] cycleColors; //from sunrise to sunset

    public WeatherModInfo[] weatherMods;
}
