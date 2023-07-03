using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used for solar panel
/// </summary>
[CreateAssetMenu(fileName = "sun", menuName = "Game/Atmosphere/Sun")]
public class SunAttribute : AtmosphereAttributeBase {
    public override string symbolString { get { return ""; } }
}
