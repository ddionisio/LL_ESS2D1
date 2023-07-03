using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StructureState {
    None,
    Spawn,
    Placement,
    Construction,
    Attacked,
    Damaged
}

[System.Flags]
public enum StructureFlags {
    None = 0x0,

    Damageable = 0x1,
    Buildable = 0x2,
}