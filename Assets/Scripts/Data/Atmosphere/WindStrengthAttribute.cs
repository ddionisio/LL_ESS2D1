using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "windStrength", menuName = "Game/Atmosphere/Wind Strength")]
public class WindStrengthAttribute : AtmosphereAttributeBase {

    public override string symbolString { get { return "mph"; } }
}
