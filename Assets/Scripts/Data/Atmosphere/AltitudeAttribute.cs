using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "altitude", menuName = "Game/Atmosphere/Altitude")]
public class AltitudeAttribute : AtmosphereAttributeBase {
    public override string symbolString { get { return "ft"; } }
}
