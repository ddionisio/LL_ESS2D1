using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Filler used for things like overlay toggle
/// </summary>
[CreateAssetMenu(fileName = "none", menuName = "Game/Atmosphere/None")]
public class AtmosphereNoneAttribute : AtmosphereAttributeBase {
    public override string symbolString { get { return ""; } }
}
