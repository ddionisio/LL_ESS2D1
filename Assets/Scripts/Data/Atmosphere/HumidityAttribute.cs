using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "humidity", menuName = "Game/Atmosphere/Humidity")]
public class HumidityAttribute : AtmosphereAttributeBase {

    public override string symbolString { get { return "%"; } }
}
