using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "temperature", menuName = "Game/Atmosphere/Temperature")]
public class TemperatureAttribute : AtmosphereAttributeBase {

    public override string symbolString { get { return "°F"; } }
}
