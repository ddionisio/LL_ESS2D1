using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "humidity", menuName = "Game/Atmosphere/Humidity")]
public class HumidityAttribute : AtmosphereAttributeBase {

    public override string symbolString { get { return "%"; } }

    public override string legendRangeString { get { return "0 30 50 80 100"; } }
}
