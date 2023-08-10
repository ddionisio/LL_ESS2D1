using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "temperature", menuName = "Game/Atmosphere/Temperature")]
public class TemperatureAttribute : AtmosphereAttributeBase {

    public override string symbolString { get { return "°F"; } }

    public override string legendRangeString { get { return "-5 15 30 50 70 85 100"; } }
}
